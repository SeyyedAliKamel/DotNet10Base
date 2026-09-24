using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.ConnectedServices
{
    // ==================== Interface ====================
    public interface ISadadGatewayClient
    {
        SadadPaymentGatewayResponseDto RequestPayment(SadadPaymentGatewayRequestDto request);
        SadadVerifyGatewayResponseDto VerifyPayment(SadadVerifyGatewayRequestDto request);
        string BuildPurchaseRedirectUrl(string token);

        /// <summary>Sadad's TripleDES/ECB "SignData" scheme. merchantKeyBase64 is the merchant's own key, not app config.</summary>
        string Sign(string plainText, string merchantKeyBase64);
    }

    // ==================== Implementation ====================
    /// <summary>
    /// Talks to the real Sadad IPG API, OR fakes it entirely for local testing — controlled by
    /// "SadadGateway:UseFake" in appsettings. No DI/registration change needed to switch between
    /// them, just flip the config value.
    ///
    /// Fake mode trick: the "gateway token" is set to the OrderId itself, so the fake checkout
    /// page doesn't need to look anything up — the OrderId just rides along in the URL.
    ///
    /// _pendingAmounts is static (not instance-level) so the fake's in-memory state survives
    /// across separate HTTP requests (Initiate then Callback) regardless of whether this class
    /// is registered Scoped or Singleton.
    /// </summary>
    public class SadadGatewayClient : ISadadGatewayClient
    {
        private static readonly ConcurrentDictionary<string, long> _pendingAmounts = new();

        private readonly HttpClient _httpClient;
        private readonly bool _useFake;
        private readonly string _purchasePageBaseUrl;
        private readonly string _fakeCheckoutBaseUrl;

        public SadadGatewayClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var sadadSettings = configuration.GetSection("SadadGateway");
            _useFake = sadadSettings.GetValue<bool>("UseFake");
            _purchasePageBaseUrl = sadadSettings["PurchasePageBaseUrl"];
            _fakeCheckoutBaseUrl = sadadSettings["FakeCheckoutBaseUrl"] ?? "https://localhost:5001";
        }

        public SadadPaymentGatewayResponseDto RequestPayment(SadadPaymentGatewayRequestDto request)
            => _useFake ? FakeRequestPayment(request) : RealRequestPayment(request);

        public SadadVerifyGatewayResponseDto VerifyPayment(SadadVerifyGatewayRequestDto request)
            => _useFake ? FakeVerifyPayment(request) : RealVerifyPayment(request);

        public string BuildPurchaseRedirectUrl(string token) => _useFake
            ? $"{_fakeCheckoutBaseUrl.TrimEnd('/')}/api/FakeSadadCheckout/Show?token={token}"
            : $"{_purchasePageBaseUrl.TrimEnd('/')}/Purchase/Index?token={Uri.EscapeDataString(token)}";

        public string Sign(string plainText, string merchantKeyBase64)
            => _useFake ? "FAKE-SIGNATURE" : RealSign(plainText, merchantKeyBase64);

        // -------------------- Real Sadad IPG --------------------

        private SadadPaymentGatewayResponseDto RealRequestPayment(SadadPaymentGatewayRequestDto request)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_purchasePageBaseUrl}/api/v0/Request/PaymentRequest")
            {
                Content = JsonContent.Create(request)
            };

            using var response = _httpClient.Send(httpRequest);
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream();
            var result = JsonSerializer.Deserialize<SadadPaymentGatewayResponseDto>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? throw new InvalidOperationException("درگاه سداد پاسخ نامعتبری برای PaymentRequest بازگرداند");
        }

        private SadadVerifyGatewayResponseDto RealVerifyPayment(SadadVerifyGatewayRequestDto request)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_purchasePageBaseUrl}/api/v0/Advice/Verify")
            {
                Content = JsonContent.Create(request)
            };

            using var response = _httpClient.Send(httpRequest);
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream();
            var result = JsonSerializer.Deserialize<SadadVerifyGatewayResponseDto>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? throw new InvalidOperationException("درگاه سداد پاسخ نامعتبری برای Verify بازگرداند");
        }

        private string RealSign(string plainText, string merchantKeyBase64)
        {
            // TripleDES/ECB/PKCS7 with a zero IV — this is what the Sadad IPG API requires,
            // not a general-purpose crypto choice.
            using var tripleDes = TripleDES.Create();
            tripleDes.Mode = CipherMode.ECB;
            tripleDes.Padding = PaddingMode.PKCS7;
            tripleDes.Key = Convert.FromBase64String(merchantKeyBase64);

            using var encryptor = tripleDes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(cipherBytes);
        }

        // -------------------- Fake (local testing) --------------------

        private SadadPaymentGatewayResponseDto FakeRequestPayment(SadadPaymentGatewayRequestDto request)
        {
            _pendingAmounts[request.OrderId] = request.Amount;

            return new SadadPaymentGatewayResponseDto
            {
                ResCode = "0",
                Token = request.OrderId, // token == orderId, purely for the fake's convenience
                Description = "FAKE: accepted"
            };
        }

        private SadadVerifyGatewayResponseDto FakeVerifyPayment(SadadVerifyGatewayRequestDto request)
        {
            var orderId = request.Token; // we set token = orderId above
            var amount = _pendingAmounts.TryGetValue(orderId, out var amt) ? amt : 0;

            return new SadadVerifyGatewayResponseDto
            {
                Succeed = true,
                ResCode = "0",
                Description = "FAKE: verified",
                Amount = amount.ToString(),
                RetrivalRefNo = "FAKE-REF-0001",
                SystemTraceNo = "FAKE-STN-0001",
                OrderId = orderId
            };
        }
    }

    // ==================== DTOs (raw Sadad IPG contracts) ====================
    public class SadadPaymentGatewayRequestDto
    {
        public string TerminalId { get; set; }
        public string MerchantId { get; set; }
        public long Amount { get; set; }
        public string SignData { get; set; }
        public string ReturnUrl { get; set; }
        public DateTime LocalDateTime { get; set; }
        public string OrderId { get; set; }
    }

    public class SadadPaymentGatewayResponseDto
    {
        public string ResCode { get; set; }
        public string? Token { get; set; }
        public string? Description { get; set; }
    }

    public class SadadVerifyGatewayRequestDto
    {
        public string Token { get; set; }
        public string SignData { get; set; }
    }

    public class SadadVerifyGatewayResponseDto
    {
        public bool Succeed { get; set; }
        public string ResCode { get; set; }
        public string? Description { get; set; }
        public string? Amount { get; set; }
        public string? RetrivalRefNo { get; set; }
        public string? SystemTraceNo { get; set; }
        public string? OrderId { get; set; }
    }

    /// <summary>What the bank posts back to your Sadad ReturnUrl.</summary>
    public class PaymentGatewayCallbackDto
    {
        public string OrderId { get; set; }
        public string Token { get; set; }
        public string ResCode { get; set; }
    }
}