using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.Text.Json;

namespace Application.Services.Shippings.Queries
{
    // ==================== Interface ====================
    public interface IShippingService
    {
        ResultDto<List<ResultShippingListDto>> GetShippingList(RequestListDto request);
        ResultDto<ResultShippingItemDto> GetShippingItem(long id);

        /// <summary>
        /// totalPrice is the basket's current total (items after discount + previously
        /// resolved shipping excluded) — used to enforce each shipping option's MinPrice,
        /// i.e. the minimum purchase amount required to use that shipping method.
        /// </summary>
        ResultDto<List<ResultValidShippingOptionDto>> GetValidShippingOptions(int weight, long? cityId, long currentShippingId, long totalPrice, bool? isWholePrice = false);
    }

    // ==================== Implementation ====================
    public class ShippingService : IShippingService
    {
        private readonly IDataBaseContext _context;

        public ShippingService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<ResultShippingListDto>> GetShippingList(RequestListDto request)
        {
            try
            {
                var query = _context.Shippings
                    .AsNoTracking()
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderByDescending(s => s.CreatedAt);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var entities = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .ToList();

                var allCityIds = entities
                    .SelectMany(x => ShippingCityIds.Parse(x.CityIds))
                    .Distinct()
                    .ToList();

                var cityNamesById = ResolveCityNames(allCityIds);

                var list = entities
                    .Select(x =>
                    {
                        var cityIds = ShippingCityIds.Parse(x.CityIds);
                        var isAllCities = cityIds.Count == 0 || cityIds.Contains(0);

                        return new ResultShippingListDto
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            CityIds = cityIds,
                            CityNames = isAllCities
                                ? new List<string>()
                                : cityIds
                                    .Select(id => cityNamesById.TryGetValue(id, out var name) ? name : $"شهر #{id}")
                                    .ToList(),
                            IsAllCities = isAllCities,
                            MinPrice = x.MinPrice,
                            Conditions = ShippingConditions.Parse(x.Conditions),
                            CreatedAt = x.CreatedAt,
                            UpdatedAt = x.UpdatedAt,
                        };
                    })
                    .ToList();

                return new ResultDto<List<ResultShippingListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست روش‌های ارسال"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultShippingListDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ResultShippingItemDto> GetShippingItem(long id)
        {
            try
            {
                var entity = _context.Shippings
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == id);

                if (entity == null)
                {
                    return new ResultDto<ResultShippingItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "روش ارسال یافت نشد"
                    };
                }

                var cityIds = ShippingCityIds.Parse(entity.CityIds);
                var isAllCities = cityIds.Count == 0 || cityIds.Contains(0);
                var cityNamesById = ResolveCityNames(cityIds);

                var item = new ResultShippingItemDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    CityIds = cityIds,
                    CityNames = isAllCities
                        ? new List<string>()
                        : cityIds
                            .Select(cid => cityNamesById.TryGetValue(cid, out var name) ? name : $"شهر #{cid}")
                            .ToList(),
                    IsAllCities = isAllCities,
                    MinPrice = entity.MinPrice,
                    Conditions = ShippingConditions.Parse(entity.Conditions),
                    CreatedAt = entity.CreatedAt,
                    UpdatedAt = entity.UpdatedAt,
                };

                return new ResultDto<ResultShippingItemDto>
                {
                    Data = item,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات روش ارسال ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultShippingItemDto>(ex);
            }
        }

        // -------------------- ReturnValidShippingOptions --------------------
        public ResultDto<List<ResultValidShippingOptionDto>> GetValidShippingOptions(int weight, long? cityId, long currentShippingId, long totalPrice, bool? isWholePrice = false)
        {
            try
            {
                if (cityId == 0 || cityId == null)
                {
                    cityId = 0;
                }

                var shippings = _context.Shippings
                    .AsNoTracking()
                    .Select(x => new { x.Id, x.Name, x.Description, x.MinPrice, x.CityIds, x.Conditions })
                    .ToList();

                var options = shippings
                    .Where(x => ShippingCityIds.AppliesToCity(x.CityIds, cityId))
                    .Where(x => !x.MinPrice.HasValue || totalPrice >= x.MinPrice.Value)
                    .Select(x => new
                    {
                        Shipping = x,
                        Price = ShippingConditions.FindPrice(ShippingConditions.Parse(x.Conditions), weight)
                    })
                    .Where(x => x.Price.HasValue)
                    .OrderBy(x => x.Price)
                    .Select(x => new ResultValidShippingOptionDto
                    {
                        ShippingId = x.Shipping.Id,
                        Name = x.Shipping.Name,
                        Description = x.Shipping.Description,
                        MinPrice = x.Shipping.MinPrice,
                        Weight = weight,
                        Price = x.Price!.Value
                    })
                    .ToList();

                return new ResultDto<List<ResultValidShippingOptionDto>>
                {
                    Data = options,
                    IsSuccess = true,
                    Status = 200,
                    Message = options.Any() ? "روش‌های ارسال معتبر" : "روش ارسالی برای این شهر یافت نشد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultValidShippingOptionDto>>(ex);
            }
        }

        // ==================== Helpers ====================
        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }

        private Dictionary<long, string> ResolveCityNames(List<long> cityIds)
        {
            if (cityIds == null || cityIds.Count == 0)
                return new Dictionary<long, string>();

            return _context.Cities
                .AsNoTracking()
                .Where(c => cityIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToList()
                .ToDictionary(c => c.Id, c => c.Name);
        }

        private ResultDto<T> HandleException<T>(Exception ex)
        {
            try
            {
                DetachAll();

                _context.HandleExceptions.Add(new HandleException
                {
                    Message = ex.InnerException?.Message ?? ex.Message,
                    StackTrace = ex.StackTrace
                });

                _context.SaveChanges();
            }
            catch
            {
                // avoid secondary crash during logging
            }

            return new ResultDto<T>
            {
                IsSuccess = false,
                Status = 500,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد"
            };
        }

        public static class ShippingConditions
        {
            private static readonly JsonSerializerOptions JsonOptions = new()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            public static string Serialize(List<WeightPriceTierDto> tiers)
            {
                var ordered = tiers.OrderBy(t => t.From).ToList();
                return JsonSerializer.Serialize(ordered, JsonOptions);
            }

            public static List<WeightPriceTierDto> Parse(string? json)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return new List<WeightPriceTierDto>();

                try
                {
                    return JsonSerializer.Deserialize<List<WeightPriceTierDto>>(json, JsonOptions)
                           ?.OrderBy(t => t.From)
                           .ToList()
                           ?? new List<WeightPriceTierDto>();
                }
                catch (JsonException)
                {
                    return new List<WeightPriceTierDto>();
                }
            }

            public static bool Validate(List<WeightPriceTierDto>? tiers, out string message)
            {
                message = string.Empty;

                if (tiers is null || tiers.Count == 0)
                {
                    message = "حداقل یک بازه وزنی الزامی است";
                    return false;
                }

                var ordered = tiers.OrderBy(t => t.From).ToList();

                for (int i = 0; i < ordered.Count; i++)
                {
                    var tier = ordered[i];

                    if (tier.From < 0)
                    {
                        message = "وزن شروع نمی‌تواند منفی باشد";
                        return false;
                    }

                    if (tier.Price < 0)
                    {
                        message = "قیمت نمی‌تواند منفی باشد";
                        return false;
                    }

                    if (tier.To.HasValue && tier.To.Value <= tier.From)
                    {
                        message = "وزن پایان باید بزرگتر از وزن شروع باشد";
                        return false;
                    }

                    if (!tier.To.HasValue && i != ordered.Count - 1)
                    {
                        message = "فقط آخرین بازه می‌تواند بدون سقف باشد";
                        return false;
                    }

                    if (i > 0 && ordered[i - 1].To != tier.From)
                    {
                        message = "بازه‌های وزنی نباید همپوشانی یا فاصله داشته باشند";
                        return false;
                    }
                }

                return true;
            }

            public static int? FindPrice(List<WeightPriceTierDto> tiers, int weight)
            {
                if (weight < 0)
                    return null;

                var tier = tiers.FirstOrDefault(t =>
                    (weight > t.From || (weight == 0 && t.From == 0)) &&
                    (t.To is null || weight <= t.To));

                return tier?.Price;
            }
        }

        // ==================== City ids ====================
        public static class ShippingCityIds
        {
            public static string? Serialize(IEnumerable<long>? ids)
            {
                var list = ids?.Distinct().OrderBy(x => x).ToList();
                return list == null || list.Count == 0 ? null : string.Join(",", list);
            }

            public static List<long> Parse(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return new List<long>();

                return value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => long.TryParse(s, out var id) ? id : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();
            }

            public static bool Validate(List<long>? ids, out string message)
            {
                message = string.Empty;

                if (ids is null || ids.Count == 0)
                    return true;

                if (ids.Any(id => id < 0))
                {
                    message = "شناسه شهر نامعتبر است";
                    return false;
                }

                return true;
            }

            public static bool AppliesToCity(string? cityIds, long? cityId)
            {
                var ids = Parse(cityIds);

                if (ids.Count == 0 || ids.Contains(0))
                    return true;

                if (cityId is null or <= 0)
                    return false;

                return ids.Contains(cityId.Value);
            }
        }
    }

    // ==================== DTOs ====================
    public class ResultShippingListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<long>? CityIds { get; set; }
        public List<string> CityNames { get; set; } = new();
        public bool IsAllCities { get; set; }
        public int? MinPrice { get; set; }
        public List<WeightPriceTierDto> Conditions { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultShippingItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<long>? CityIds { get; set; }
        public List<string> CityNames { get; set; } = new();
        public bool IsAllCities { get; set; }
        public int? MinPrice { get; set; }
        public List<WeightPriceTierDto> Conditions { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultShippingPriceDto
    {
        public long ShippingId { get; set; }
        public int Weight { get; set; }
        public int Price { get; set; }
    }

    public class WeightPriceTierDto
    {
        public int From { get; set; }
        public int? To { get; set; }
        public int Price { get; set; }
    }

    public class ResultValidShippingOptionDto
    {
        public long ShippingId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int? MinPrice { get; set; }
        public int Weight { get; set; }
        public int Price { get; set; }
    }

    public class ResultValidShippingOptionsDto
    {
        public long? CurrentShippingId { get; set; }
        public bool IsCurrentValid { get; set; }
        public List<ResultValidShippingOptionDto> Options { get; set; } = new();
    }
}