using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;

namespace Common;

public static class Pagination
{
    public static IQueryable<T> ToPaged<T>(this IQueryable<T> source, int page, int pageSize, out int totalCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        totalCount = source.Count();
        return source.Skip((page - 1) * pageSize).Take(pageSize);
    }
}

// =============================================================================
// Generic Sort + Filter engine
//
// Both `sort` and `filter` accept a property "path": a plain property name,
// or a dotted chain that walks navigation properties of ANY depth, mixing
// single references and collections freely, e.g.:
//
//   "Name"                                   -> direct column
//   "Brand.Title"                            -> single-reference hop
//   "Hashtags.HashtagId"                     -> collection hop
//   "ProductCategories.Category.Name"        -> collection -> reference -> leaf
//   "ProductCategories.Category.Parent.Name" -> any number of hops
//
// Filter JSON: { "path": value }, where value is a single scalar
// (string/number/bool) for an equality/Contains match, or a JSON array of
// scalars for an IN-style "match any of these" filter. This is backward
// compatible with the old { "path": "singleValue" } contract.
//
// RANGE FILTERS: if a key does not resolve as a literal property path, but
// stripping a trailing "From" or "To" DOES resolve (e.g. "CreatedAtFrom" ->
// "CreatedAt", "PaymentDateTo" -> "PaymentDate"), the entry is treated as a
// range bound instead of an equality/IN match:
//   { "CreatedAtFrom": "2026-08-23 00:00:00" }  -> CreatedAt >= value
//   { "CreatedAtTo":   "2026-09-07 23:59:59" }  -> CreatedAt <= value
// Range bounds support DateTime, int, long, decimal and double leaf
// properties (including through collection hops, via Any()). Only the
// first supplied value is used as the bound. A literal property that is
// itself named e.g. "CreatedAtFrom" always takes precedence and is matched
// with ordinary equality/Contains/IN semantics.
//
// Sort JSON: { "Field": "path", "Direction": "asc" | "desc" } - unchanged
// contract. If the path passes through one or more collection hops, the
// values are aggregated with Min() (e.g. sorting products by
// "ProductCategories.Category.Name" orders by the alphabetically-first
// linked category name).
// =============================================================================
public static class Filter
{
    public static IQueryable<T> ApplySortingAndFiltering<T>(this IQueryable<T> query, string? sort, string? filter)
        where T : class
    {
        if (!string.IsNullOrWhiteSpace(sort))
            query = ApplySorting(query, sort);

        if (!string.IsNullOrWhiteSpace(filter))
            query = ApplyFiltering(query, filter);

        return query;
    }

    public static IQueryable<T> ApplySorting<T>(IQueryable<T> query, string sort) where T : class
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query;

        var sortOptions = JsonSerializer.Deserialize<SortOptions>(sort, JsonOptions.Default)
            ?? throw new ArgumentException("Invalid sort JSON.");

        if (string.IsNullOrWhiteSpace(sortOptions.Field) || sortOptions.Direction is not ("asc" or "desc"))
            return query;

        var hops = ResolveHops(typeof(T), sortOptions.Field.Split('.'));
        if (hops is null || hops.Count == 0)
            return query;

        var built = BuildSortExpression(hops, 0);
        if (built is null)
            return query;

        var (expression, isEnumerable) = built.Value;
        var key = isEnumerable ? $"{expression}.Min()" : expression;
        var direction = sortOptions.Direction == "asc" ? "ascending" : "descending";

        return query.OrderBy($"{key} {direction}");
    }

    private static IQueryable<T> ApplyFiltering<T>(IQueryable<T> query, string filter) where T : class
    {
        if (string.IsNullOrWhiteSpace(filter))
            return query;

        var filterMap = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(filter, JsonOptions.Default)
            ?? throw new ArgumentException("Invalid filter JSON.");

        ApplyDateRangeDefaults<T>(filterMap);

        foreach (var (rawPath, element) in filterMap)
        {
            var values = ExtractValues(element);
            if (values.Count == 0)
                continue;

            if (rawPath.Contains('|'))
            {
                var compositeParameters = new List<object>();
                var compositeExpression = BuildCompositeSearchExpressionFromPath<T>(rawPath, values, compositeParameters);
                if (compositeExpression is not null)
                    query = query.Where(compositeExpression, compositeParameters.ToArray());

                continue;
            }

            if (string.Equals(rawPath, "FullName", StringComparison.OrdinalIgnoreCase)
                && ResolveHops(typeof(T), rawPath.Split('.')) is null)
            {
                var fullNameParameters = new List<object>();
                var fullNameExpression = BuildFullNameExpression<T>(values, fullNameParameters);
                if (fullNameExpression is not null)
                {
                    query = query.Where(fullNameExpression, fullNameParameters.ToArray());
                    continue;
                }
                // No FirstName/LastName on T - fall through so ResolveFilterKey
                // handles it normally (which will just skip the unknown key).
            }

            var (path, op) = ResolveFilterKey<T>(rawPath);

            var hops = ResolveHops(typeof(T), path.Split('.'));
            if (hops is null || hops.Count == 0)
                continue;

            var parameters = new List<object>();
            var expression = BuildFilterExpression(hops, 0, values, parameters, op);
            if (expression is null)
                continue;

            query = query.Where(expression, parameters.ToArray());
        }

        return query;
    }

    // -------------------------------------------------------------------------
    // Path resolution (shared by sort and filter)
    // -------------------------------------------------------------------------

    private readonly record struct Hop(PropertyInfo Property, bool IsCollection, Type NextType);

    private enum FilterOp
    {
        Equality,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    /// Fallback lower bound used when a "*To" range key is supplied without a
    /// matching "*From" (e.g. only "CreatedAtTo" is given). Adjust as needed.
    /// </summary>
    private static readonly DateTime DefaultRangeStart = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Scans the raw filter map for DateTime range keys ("*From"/"*To") and,
    /// where only one side of a pair is present, injects the other side with
    /// a default so the query doesn't come back unbounded:
    ///   - "CreatedAtFrom" with no "CreatedAtTo"  -> To defaults to DateTime.UtcNow
    ///   - "CreatedAtTo" with no "CreatedAtFrom"   -> From defaults to <see cref="DefaultRangeStart"/>
    /// Only applies to properties whose (unwrapped) type is DateTime, and only
    /// to keys that resolve as range keys (i.e. don't already exist as a
    /// literal property on T) so it never touches ordinary equality filters.
    /// </summary>
    private static void ApplyDateRangeDefaults<T>(Dictionary<string, JsonElement> filterMap) where T : class
    {
        var seen = new Dictionary<string, (bool HasFrom, bool HasTo)>();

        foreach (var rawKey in filterMap.Keys)
        {
            // Literal property matches are ordinary equality filters, not ranges - skip.
            if (ResolveHops(typeof(T), rawKey.Split('.')) is not null)
                continue;

            string? path = null;
            bool isFrom = false;

            if (rawKey.EndsWith("From", StringComparison.OrdinalIgnoreCase) && rawKey.Length > 4)
            {
                var stripped = rawKey[..^4];
                if (ResolveHops(typeof(T), stripped.Split('.')) is not null)
                {
                    path = stripped;
                    isFrom = true;
                }
            }
            else if (rawKey.EndsWith("To", StringComparison.OrdinalIgnoreCase) && rawKey.Length > 2)
            {
                var stripped = rawKey[..^2];
                if (ResolveHops(typeof(T), stripped.Split('.')) is not null)
                {
                    path = stripped;
                    isFrom = false;
                }
            }

            if (path is null)
                continue;

            var hops = ResolveHops(typeof(T), path.Split('.'));
            var leafType = hops?[^1].Property.PropertyType;
            leafType = leafType is null ? null : Nullable.GetUnderlyingType(leafType) ?? leafType;
            if (leafType != typeof(DateTime))
                continue;

            seen.TryGetValue(path, out var entry);
            seen[path] = isFrom ? (true, entry.HasTo) : (entry.HasFrom, true);
        }

        foreach (var (path, (hasFrom, hasTo)) in seen)
        {
            if (hasFrom && !hasTo)
            {
                filterMap[$"{path}To"] = JsonSerializer.SerializeToElement(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else if (hasTo && !hasFrom)
            {
                filterMap[$"{path}From"] = JsonSerializer.SerializeToElement(DefaultRangeStart.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }
    }

    /// <summary>
    /// Builds a Dynamic LINQ predicate for a "|"-delimited composite filter
    /// key, e.g. "FirstName|LastName". Each sub-path is resolved exactly like
    /// a normal filter path (so dotted navigation/collection paths work too,
    /// e.g. "FirstName|LastName|Company.Name"). Delegates the actual clause
    /// building to <see cref="BuildCompositeSearchExpression"/>.
    /// </summary>
    private static string? BuildCompositeSearchExpressionFromPath<T>(string rawPath, List<string> values, List<object> parameters) where T : class
    {
        var subHops = rawPath
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => ResolveHops(typeof(T), p.Split('.')))
            .Where(h => h is not null)
            .Select(h => h!)
            .ToList();

        return BuildCompositeSearchExpression(subHops, values, parameters);
    }

    /// <summary>
    /// If T has both "FirstName" and "LastName" properties, builds the same
    /// kind of composite search clause as a "FirstName|LastName" path, so a
    /// plain filter key of "FullName" works automatically - no registration
    /// or client-side pipe syntax needed. Returns null if either property is
    /// missing on T.
    /// </summary>
    private static string? BuildFullNameExpression<T>(List<string> values, List<object> parameters) where T : class
    {
        var firstNameHops = ResolveHops(typeof(T), new[] { "FirstName" });
        var lastNameHops = ResolveHops(typeof(T), new[] { "LastName" });

        if (firstNameHops is null || lastNameHops is null)
            return null;

        return BuildCompositeSearchExpression(new List<List<Hop>> { firstNameHops, lastNameHops }, values, parameters);
    }

    /// <summary>
    /// Core composite-search builder shared by the "|" path syntax and the
    /// "FullName" convention. For each supplied value, the value is split on
    /// whitespace into words; each word must match at least one of the
    /// resolved sub-paths (Contains), and all words must match (AND).
    /// Multiple supplied values are OR'd together.
    /// </summary>
    private static string? BuildCompositeSearchExpression(List<List<Hop>> subHops, List<string> values, List<object> parameters)
    {
        if (subHops.Count == 0)
            return null;

        var valueClauses = new List<string>();
        foreach (var value in values)
        {
            var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            var tokenClauses = new List<string>();
            foreach (var token in tokens)
            {
                var tokenAsValues = new List<string> { token };
                var propClauses = subHops
                    .Select(hops => BuildFilterExpression(hops, 0, tokenAsValues, parameters, FilterOp.Equality))
                    .Where(clause => clause is not null)
                    .ToList();

                if (propClauses.Count == 0)
                    continue;

                tokenClauses.Add(propClauses.Count == 1 ? propClauses[0]! : "(" + string.Join(" || ", propClauses) + ")");
            }

            if (tokenClauses.Count == 0)
                continue;

            valueClauses.Add(tokenClauses.Count == 1 ? tokenClauses[0] : "(" + string.Join(" && ", tokenClauses) + ")");
        }

        if (valueClauses.Count == 0)
            return null;

        return valueClauses.Count == 1 ? valueClauses[0] : "(" + string.Join(" || ", valueClauses) + ")";
    }

    /// <summary>
    /// Determines the actual property path and comparison operator for a raw
    /// filter key. A literal match always wins (backward compatible). If the
    /// literal path doesn't resolve, a trailing "From"/"To" suffix is tried
    /// as a range bound on the remaining path.
    /// </summary>
    private static (string Path, FilterOp Op) ResolveFilterKey<T>(string rawKey) where T : class
    {
        if (ResolveHops(typeof(T), rawKey.Split('.')) is not null)
            return (rawKey, FilterOp.Equality);

        if (rawKey.EndsWith("From", StringComparison.OrdinalIgnoreCase) && rawKey.Length > 4)
        {
            var stripped = rawKey[..^4];
            if (ResolveHops(typeof(T), stripped.Split('.')) is not null)
                return (stripped, FilterOp.GreaterOrEqual);
        }
        else if (rawKey.EndsWith("To", StringComparison.OrdinalIgnoreCase) && rawKey.Length > 2)
        {
            var stripped = rawKey[..^2];
            if (ResolveHops(typeof(T), stripped.Split('.')) is not null)
                return (stripped, FilterOp.LessOrEqual);
        }

        // Neither resolves; return as-is so downstream ResolveHops fails and
        // the entry is silently skipped, matching prior "unknown path" behavior.
        return (rawKey, FilterOp.Equality);
    }

    /// <summary>
    /// Walks a dotted path against <paramref name="rootType"/>, resolving each
    /// segment to a property (case-insensitive) and recording whether it's a
    /// collection hop. Returns null if any segment can't be resolved.
    /// </summary>
    private static List<Hop>? ResolveHops(Type rootType, string[] segments)
    {
        var hops = new List<Hop>(segments.Length);
        var currentType = rootType;

        foreach (var segment in segments)
        {
            var prop = currentType.GetProperty(segment, PublicInstance | BindingFlags.IgnoreCase);
            if (prop is null)
                return null;

            var isCollection = TryGetCollectionElementType(prop.PropertyType, out var elementType);
            var nextType = isCollection ? elementType : prop.PropertyType;

            hops.Add(new Hop(prop, isCollection, nextType));
            currentType = nextType;
        }

        return hops;
    }

    /// <summary>
    /// Builds the Dynamic LINQ predicate string for a resolved path, e.g.
    /// "ProductCategories.Any(Category.Name.Contains(@0))" for equality, or
    /// "CreatedAt >= @0" / collection-wrapped equivalents for range bounds.
    /// Returns null if the leaf type isn't supported for the given operator,
    /// so the caller can silently skip an invalid filter entry.
    /// </summary>
    private static string? BuildFilterExpression(List<Hop> hops, int index, List<string> values, List<object> parameters, FilterOp op)
    {
        var hop = hops[index];
        var isLast = index == hops.Count - 1;

        if (isLast)
            return hop.IsCollection ? null : BuildLeafComparison(hop.Property, values, parameters, op);

        var inner = BuildFilterExpression(hops, index + 1, values, parameters, op);
        if (inner is null)
            return null;

        return hop.IsCollection
            ? $"{hop.Property.Name}.Any({inner})"
            : $"{hop.Property.Name}.{inner}";
    }

    /// <summary>
    /// Builds the Dynamic LINQ projection string for a resolved sort path,
    /// e.g. "ProductCategories.Select(Category.Name)". Returns whether the
    /// result is a per-entity scalar or an IEnumerable that still needs
    /// aggregating (because the path crossed a collection).
    /// </summary>
    private static (string Expression, bool IsEnumerable)? BuildSortExpression(List<Hop> hops, int index)
    {
        var hop = hops[index];
        var isLast = index == hops.Count - 1;

        if (isLast)
            return hop.IsCollection ? null : (hop.Property.Name, false);

        var inner = BuildSortExpression(hops, index + 1);
        if (inner is null)
            return null;

        var (innerExpr, innerIsEnumerable) = inner.Value;

        if (hop.IsCollection)
        {
            var method = innerIsEnumerable ? "SelectMany" : "Select";
            return ($"{hop.Property.Name}.{method}({innerExpr})", true);
        }

        return ($"{hop.Property.Name}.{innerExpr}", innerIsEnumerable);
    }

    private static string? BuildLeafComparison(PropertyInfo prop, List<string> values, List<object> parameters, FilterOp op)
    {
        var propType = prop.PropertyType;
        var underlying = Nullable.GetUnderlyingType(propType) ?? propType;

        if (op != FilterOp.Equality)
            return BuildRangeComparison(prop, underlying, values, parameters, op);

        if (underlying == typeof(string))
        {
            var clauses = new List<string>();
            foreach (var value in values)
            {
                parameters.Add(value);
                clauses.Add($"{prop.Name}.Contains(@{parameters.Count - 1})");
            }

            return clauses.Count switch
            {
                0 => null,
                1 => clauses[0],
                _ => "(" + string.Join(" || ", clauses) + ")"
            };
        }

        Type? scalarType = underlying switch
        {
            _ when underlying == typeof(int) => typeof(int),
            _ when underlying == typeof(long) => typeof(long),
            _ when underlying == typeof(bool) => typeof(bool),
            _ when underlying == typeof(DateTime) => typeof(DateTime),
            _ when underlying.IsEnum => underlying,
            _ => null
        };

        if (scalarType is null)
            return null;

        var parsed = ParseValues(scalarType, values);
        if (parsed.Count == 0)
            return null;

        if (parsed.Count == 1)
        {
            parameters.Add(parsed[0]);
            return $"{prop.Name} == @{parameters.Count - 1}";
        }

        parameters.Add(BuildTypedList(scalarType, parsed));
        return $"@{parameters.Count - 1}.Contains({prop.Name})";
    }

    /// <summary>
    /// Builds a "&gt;=" or "&lt;=" comparison against a single parsed bound
    /// value. Only ordered scalar types are supported; anything else (string,
    /// bool, enum, ...) returns null and the filter entry is skipped.
    /// </summary>
    private static string? BuildRangeComparison(PropertyInfo prop, Type underlying, List<string> values, List<object> parameters, FilterOp op)
    {
        Type? scalarType = underlying switch
        {
            _ when underlying == typeof(DateTime) => typeof(DateTime),
            _ when underlying == typeof(int) => typeof(int),
            _ when underlying == typeof(long) => typeof(long),
            _ when underlying == typeof(decimal) => typeof(decimal),
            _ when underlying == typeof(double) => typeof(double),
            _ => null
        };

        if (scalarType is null)
            return null;

        var parsed = ParseValues(scalarType, values);
        if (parsed.Count == 0)
            return null;

        // Range bounds are single-valued; ignore any extras beyond the first.
        parameters.Add(parsed[0]);
        var opStr = op == FilterOp.GreaterOrEqual ? ">=" : "<=";
        return $"{prop.Name} {opStr} @{parameters.Count - 1}";
    }

    // -------------------------------------------------------------------------
    // Value parsing helpers
    // -------------------------------------------------------------------------

    private static List<string> ExtractValues(JsonElement element)
    {
        var result = new List<string>();

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var s = element.GetString();
                if (!string.IsNullOrWhiteSpace(s)) result.Add(s);
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                result.Add(element.ToString());
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var itemStr = item.GetString();
                        if (!string.IsNullOrWhiteSpace(itemStr)) result.Add(itemStr);
                    }
                    else if (item.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                    {
                        result.Add(item.ToString());
                    }
                }
                break;
        }

        return result;
    }

    private static List<object> ParseValues(Type scalarType, List<string> raw)
    {
        var result = new List<object>();

        foreach (var value in raw)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (scalarType == typeof(int))
            {
                if (int.TryParse(value, out var v)) result.Add(v);
            }
            else if (scalarType == typeof(long))
            {
                if (long.TryParse(value, out var v)) result.Add(v);
            }
            else if (scalarType == typeof(decimal))
            {
                if (decimal.TryParse(value, out var v)) result.Add(v);
            }
            else if (scalarType == typeof(double))
            {
                if (double.TryParse(value, out var v)) result.Add(v);
            }
            else if (scalarType == typeof(bool))
            {
                if (bool.TryParse(value, out var v)) result.Add(v);
            }
            else if (scalarType == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out var v)) result.Add(v);
            }
            else if (scalarType.IsEnum)
            {
                if (TryParseEnum(scalarType, value, out var v)) result.Add(v);
            }
        }

        return result;
    }

    private static object BuildTypedList(Type elementType, List<object> values)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
        foreach (var value in values)
            list.Add(value);

        return list;
    }

    /// <summary>
    /// Resolves the element type if <paramref name="type"/> is a collection
    /// (anything implementing IEnumerable&lt;T&gt; other than string).
    /// </summary>
    private static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        if (type != typeof(string))
        {
            var interfaces = type.IsInterface ? type.GetInterfaces().Prepend(type) : type.GetInterfaces();
            foreach (var candidate in interfaces)
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elementType = candidate.GetGenericArguments()[0];
                    return true;
                }
            }
        }

        elementType = null!;
        return false;
    }

    private static bool TryParseEnum(Type enumType, string value, out object result)
    {
        // Accept either the numeric value ("2") or the member name ("Sent")
        if (int.TryParse(value, out var numeric) && Enum.IsDefined(enumType, numeric))
        {
            result = Enum.ToObject(enumType, numeric);
            return true;
        }

        if (Enum.TryParse(enumType, value, ignoreCase: true, out var parsed))
        {
            result = parsed!;
            return true;
        }

        result = null!;
        return false;
    }

    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
}

// -------------------------------------------------------------------------
// Supporting types
// -------------------------------------------------------------------------

file record SortOptions(string Field, string Direction);

file static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };
}