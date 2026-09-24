using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.ConnectedServices
{
    // ==================== Interface ====================
    public interface ISepGatewayClient
    {
        SepTokenResponseDto RequestToken(SepTokenRequestDto request);
        SepVerifyResponseDto VerifyTransaction(SepVerifyRequestDto request);

        /// <summary>
        /// SEP does NOT accept a simple GET redirect like Sadad does — the browser must POST the
        /// token to sep.shaparak.ir/OnlinePG/OnlinePG. This returns just the action URL and token
        /// so the caller (frontend) can build/submit that POST form itself.
        /// </summary>
        SepRedirectInfo GetPurchaseRedirectInfo(string token);
    }

    // ==================== Implementation ====================
    /// <summary>
    /// Talks to the real SEP (Saman) IPG, or fakes it for local testing — controlled by
    /// "SepGateway:UseFake" in appsettings.
    ///
    /// Config keys (section "SepGateway"):
    ///   UseFake              true/false. Use true on your dev machine.
    ///   TerminalId           string (kept as text so leading zeros survive)
    ///   ReturnUrl            full URL on the domain registered for the terminal
    ///   VerifyUrl            optional override of the verify endpoint
    ///   FakeCheckoutBaseUrl  optional, only used when UseFake=true
    ///
    /// _pendingAmounts is static so the fake's in-memory state survives across separate HTTP
    /// requests (Initiate then Callback) regardless of DI lifetime.
    /// </summary>
    public class SepGatewayClient : ISepGatewayClient
    {
        private static readonly ConcurrentDictionary<string, long> _pendingAmounts = new();

        // Token request/response: JSON POST, action=token.
        private const string TokenRequestUrl = "https://sep.shaparak.ir/onlinepg/onlinepg";

        // Where the browser is form-POSTed to redeem the token and reach the bank page.
        private const string PurchaseRedirectUrl = "https://sep.shaparak.ir/OnlinePG/OnlinePG";

        // Default verify endpoint (taken from your working sample). Override with SepGateway:VerifyUrl
        // if SEP's docs give you a different one.
        private const string DefaultVerifyUrl = "https://sep.shaparak.ir/verifyTxnRandomSessionkey/ipg/VerifyTranscation";

        // Keep property names EXACTLY as declared (no camelCase), skip nulls, and don't escape
        // characters like '&' in URLs.
        private static readonly JsonSerializerOptions RequestJsonOptions = new()
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<SepGatewayClient> _logger;
        private readonly bool _useFake;
        private readonly string _fakeCheckoutBaseUrl;
        private readonly string _verifyUrl;

        public SepGatewayClient(HttpClient httpClient, IConfiguration configuration, ILogger<SepGatewayClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var sepSettings = configuration.GetSection("SepGateway");
            _useFake = sepSettings.GetValue<bool>("UseFake");
            _fakeCheckoutBaseUrl = sepSettings["FakeCheckoutBaseUrl"] ?? "https://localhost:5001";
            _verifyUrl = sepSettings["VerifyUrl"] is { Length: > 0 } configured ? configured : DefaultVerifyUrl;
        }

        public SepTokenResponseDto RequestToken(SepTokenRequestDto request)
            => _useFake ? FakeRequestToken(request) : RealRequestToken(request);

        public SepVerifyResponseDto VerifyTransaction(SepVerifyRequestDto request)
            => _useFake ? FakeVerifyTransaction(request) : RealVerifyTransaction(request);

        public SepRedirectInfo GetPurchaseRedirectInfo(string token)
        {
            var actionUrl = _useFake
                ? $"{_fakeCheckoutBaseUrl.TrimEnd('/')}/api/FakeSepCheckout/Show"
                : PurchaseRedirectUrl;

            return new SepRedirectInfo
            {
                ActionUrl = actionUrl,
                Token = token
            };
        }

        // -------------------- Real SEP IPG --------------------

        private SepTokenResponseDto RealRequestToken(SepTokenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.TerminalId))
                throw new InvalidOperationException("SepGateway:TerminalId is not configured.");
            if (string.IsNullOrWhiteSpace(request.RedirectUrl))
                throw new InvalidOperationException("SepGateway:ReturnUrl is not configured.");

            var terminalId = request.TerminalId.Trim();

            // Matches SEP's sample body: "action" lowercase, the rest PascalCase,
            // TerminalId and CellNumber as strings, Amount as a bare number.
            var payload = new
            {
                action = "token",
                TerminalId = terminalId,
                Amount = request.Amount,
                ResNum = request.ResNum,
                RedirectUrl = request.RedirectUrl.Trim(),
                CellNumber = NormalizeCellNumber(request.CellNumber) // null => omitted
            };

            var json = JsonSerializer.Serialize(payload, RequestJsonOptions);

            _logger.LogInformation("SEP token request body: {Body}",
                json.Replace(terminalId, MaskId(terminalId)));

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, TokenRequestUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = _httpClient.Send(httpRequest);
            var raw = ReadBody(response);

            var result = ParseTokenResponse(raw, (int)response.StatusCode);

            if (result.Status != 1 || string.IsNullOrEmpty(result.Token))
            {
                _logger.LogWarning("SEP token request rejected. HTTP {HttpStatus}, response: {Raw}",
                    (int)response.StatusCode, Truncate(raw, 1000));
            }

            return result;
        }

        private static SepTokenResponseDto ParseTokenResponse(string raw, int httpStatus)
        {
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(raw);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(
                    $"SEP returned a non-JSON token response (HTTP {httpStatus}): {Truncate(raw, 500)}");
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        $"SEP returned an unexpected token response (HTTP {httpStatus}): {Truncate(raw, 500)}");
                }

                return new SepTokenResponseDto
                {
                    Status = TryGetInt(root, "status") ?? 0, // anything but 1 is treated as failure
                    Token = TryGetString(root, "token"),
                    ErrorCode = TryGetString(root, "errorCode"),
                    ErrorDesc = TryGetString(root, "errorDesc"),
                    RawResponse = Truncate(raw, 1000)
                };
            }
        }

        private SepVerifyResponseDto RealVerifyTransaction(SepVerifyRequestDto request)
        {
            var json = JsonSerializer.Serialize(request, RequestJsonOptions);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _verifyUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = _httpClient.Send(httpRequest);
            var raw = ReadBody(response);

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(raw);
            }
            catch (JsonException)
            {
                _logger.LogWarning("SEP verify returned non-JSON (HTTP {HttpStatus}): {Raw}",
                    (int)response.StatusCode, Truncate(raw, 1000));

                return new SepVerifyResponseDto
                {
                    Succeed = false,
                    Description = $"Non-JSON verify response (HTTP {(int)response.StatusCode})",
                    RefNum = request.RefNum,
                    RawResponse = Truncate(raw, 1000)
                };
            }

            using (doc)
            {
                var root = doc.RootElement;

                // SEP nests the transaction data under "TransactionDetail"; fall back to the
                // root in case this endpoint returns a flat object.
                var detail = TryGetObject(root, "TransactionDetail") ?? root;

                string? resultCode = TryGetString(root, "ResultCode", "State");
                string? description = TryGetString(root, "ResultDescription", "Message");
                bool? successFlag = TryGetBool(root, "Success", "Succeed");

                // Prefer the explicit success flag; otherwise ResultCode 0 / "Success".
                bool isSuccess = successFlag
                    ?? (resultCode == "0" || string.Equals(resultCode, "Success", StringComparison.OrdinalIgnoreCase));

                // OrginalAmount (sic) is what the customer paid; this is what we compare against.
                string? amount = TryGetString(detail, "OrginalAmount", "OriginalAmount", "Amount", "AffectiveAmount");
                string? refNum = TryGetString(detail, "RefNum");
                string? traceNo = TryGetString(detail, "StraceNo", "TraceNo");
                string? rrn = TryGetString(detail, "RRN", "ReferenceNumber");

                if (isSuccess)
                    _logger.LogDebug("SEP verify response: {Raw}", raw);
                else
                    _logger.LogWarning("SEP verify not successful. HTTP {HttpStatus}, response: {Raw}",
                        (int)response.StatusCode, Truncate(raw, 1000));

                return new SepVerifyResponseDto
                {
                    Succeed = isSuccess,
                    ResultCode = resultCode ?? string.Empty,
                    Description = description,
                    Amount = amount,
                    RefNum = refNum ?? request.RefNum,
                    TraceNo = traceNo,
                    Rrn = rrn,
                    RawResponse = Truncate(raw, 1000)
                };
            }
        }

        // -------------------- JSON / string helpers --------------------

        private static string ReadBody(HttpResponseMessage response)
        {
            using var stream = response.Content.ReadAsStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>Case-insensitive lookup; tries each name in order, returns the first non-null value.</summary>
        private static string? TryGetString(JsonElement element, params string[] propertyNames)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            foreach (var name in propertyNames)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    if (!string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)) continue;

                    var value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetRawText(),
                        JsonValueKind.True or JsonValueKind.False => prop.Value.GetRawText(),
                        _ => null
                    };

                    if (value != null) return value;
                }
            }

            return null;
        }

        private static int? TryGetInt(JsonElement element, params string[] propertyNames)
        {
            var s = TryGetString(element, propertyNames);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : (int?)null;
        }

        private static bool? TryGetBool(JsonElement element, params string[] propertyNames)
        {
            var s = TryGetString(element, propertyNames);
            return bool.TryParse(s, out var v) ? v : (bool?)null;
        }

        private static JsonElement? TryGetObject(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                    && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    return prop.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// SEP's sample sends the mobile without the leading zero (9120000000).
        /// Returns null (=> field omitted) if the number can't be normalised to 10 digits.
        /// </summary>
        private static string? NormalizeCellNumber(string? cell)
        {
            if (string.IsNullOrWhiteSpace(cell)) return null;

            // Map any Unicode digits (e.g. Persian ۰-۹) to ASCII, drop everything else.
            var sb = new StringBuilder();
            foreach (var c in cell)
            {
                var d = CharUnicodeInfo.GetDecimalDigitValue(c);
                if (d >= 0) sb.Append(d);
            }

            var digits = sb.ToString();

            if (digits.StartsWith("0098")) digits = digits[4..];
            else if (digits.StartsWith("98") && digits.Length == 12) digits = digits[2..];
            else if (digits.StartsWith('0')) digits = digits[1..];

            return digits.Length == 10 ? digits : null;
        }

        private static string MaskId(string id)
            => id.Length <= 2 ? "**" : new string('*', id.Length - 2) + id[^2..];

        private static string Truncate(string? value, int max)
            => string.IsNullOrEmpty(value) || value.Length <= max ? value ?? string.Empty : value[..max] + "...";

        // -------------------- Fake (local testing) --------------------

        private SepTokenResponseDto FakeRequestToken(SepTokenRequestDto request)
        {
            _pendingAmounts[request.ResNum] = request.Amount;

            return new SepTokenResponseDto
            {
                Status = 1,
                Token = request.ResNum // token == ResNum, purely for the fake's convenience
            };
        }

        private SepVerifyResponseDto FakeVerifyTransaction(SepVerifyRequestDto request)
        {
            // In fake mode the "RefNum" the fake checkout hands back is just the ResNum/token.
            var amount = _pendingAmounts.TryGetValue(request.RefNum, out var amt) ? amt : 0;

            return new SepVerifyResponseDto
            {
                Succeed = true,
                ResultCode = "0",
                Description = "FAKE: verified",
                Amount = amount.ToString(),
                RefNum = request.RefNum,
                TraceNo = "FAKE-TRACE-0001",
                Rrn = "FAKE-RRN-" + request.RefNum
            };
        }
    }

    // ==================== DTOs (raw SEP contracts) ====================

    public class SepTokenRequestDto
    {
        public string TerminalId { get; set; } = string.Empty; // string: keeps leading zeros
        public string ResNum { get; set; } = string.Empty; // your own unique order id
        public long Amount { get; set; } // Rials
        public string? CellNumber { get; set; } // payer mobile; omitted from the request when empty/invalid
        public string RedirectUrl { get; set; } = string.Empty;
    }

    public class SepTokenResponseDto
    {
        public int Status { get; set; } // 1 == success
        public string? Token { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorDesc { get; set; }
        public string? RawResponse { get; set; } // for debugging rejected requests
    }

    public class SepVerifyRequestDto
    {
        public string TerminalNumber { get; set; } = string.Empty;
        public string RefNum { get; set; } = string.Empty;
        public string CellNumber { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public bool IgnoreNationalcode { get; set; } = true;
    }

    public class SepVerifyResponseDto
    {
        public bool Succeed { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Amount { get; set; } // Rials
        public string? RefNum { get; set; }
        public string? TraceNo { get; set; }
        public string? Rrn { get; set; }
        public string? RawResponse { get; set; } // for debugging while you confirm the real schema
    }

    /// <summary>
    /// What the browser needs to POST the token to SEP — replaces the old pre-built HTML string.
    /// Frontend builds and submits a form from these two values.
    /// </summary>
    public class SepRedirectInfo
    {
        public string ActionUrl { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// What SEP POSTs back to your RedirectUrl — field names taken directly from the working
    /// sample's Result.aspx.cs (Request.Form["RefNum"], ["ResNum"], ["state"], ["TraceNo"], ["TerminalId"]).
    /// </summary>
    public class SepGatewayCallbackDto
    {
        public string ResNum { get; set; } = string.Empty; // == your OrderId
        public string RefNum { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty; // "OK" == success
        public string TraceNo { get; set; } = string.Empty;
        public string TerminalId { get; set; } = string.Empty;
    }
}