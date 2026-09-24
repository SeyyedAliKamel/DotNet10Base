using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace Application.ConnectedServices
{
    // ==================== Interface ====================
    // Thin wrapper around Bale's Bot API (https://tapi.bale.ai/bot<TOKEN>/...).
    // Mirrors ISadadGatewayClient / ISepGatewayClient: no business logic here,
    // just "send this HTTP request, hand back the parsed response".
    public interface IBaleGatewayClient
    {
        BaleSendMessageResponseDto SendMessage(string chatId, string text, string? parseMode = "HTML");
    }

    // ==================== Implementation ====================
    public class BaleGatewayClient : IBaleGatewayClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken;
        private readonly string _apiBaseUrl;

        // Expected appsettings.json section:
        // "BaleBot": { "Token": "xxxxx", "ApiBaseUrl": "https://tapi.bale.ai" }
        public BaleGatewayClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(BaleGatewayClient));

            var baleSettings = configuration.GetSection("BaleBot");
            _botToken = baleSettings["Token"] ?? string.Empty;
            _apiBaseUrl = (baleSettings["ApiBaseUrl"] ?? "https://tapi.bale.ai").TrimEnd('/');
        }

        public BaleSendMessageResponseDto SendMessage(string chatId, string text, string? parseMode = "HTML")
        {
            var url = $"{_apiBaseUrl}/bot{_botToken}/sendMessage";

            var payload = new BaleSendMessageRequestDto
            {
                ChatId = chatId,
                Text = text,
                ParseMode = parseMode,
                DisableWebPagePreview = false
            };

            try
            {
                // .Result is acceptable here only because the rest of this codebase's
                // service layer is synchronous end-to-end (see PaymentService/ContentService).
                // Swap SendMessage/this class to async all the way up if that ever changes.
                using var response = _httpClient.PostAsJsonAsync(url, payload).Result;
                var parsed = response.Content.ReadFromJsonAsync<BaleSendMessageResponseDto>().Result;

                if (parsed != null)
                {
                    parsed.HttpStatusCode = (int)response.StatusCode;
                    return parsed;
                }

                return new BaleSendMessageResponseDto
                {
                    Ok = false,
                    Description = "Empty/unparseable response from Bale API",
                    HttpStatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                // Network failure, timeout, DNS, etc. Caller (ContentBroadcastService) decides
                // whether this should stop the whole broadcast or just skip this one chat.
                return new BaleSendMessageResponseDto
                {
                    Ok = false,
                    Description = ex.Message,
                    HttpStatusCode = 0
                };
            }
        }
    }

    // ==================== DTOs ====================
    public class BaleSendMessageRequestDto
    {
        [JsonPropertyName("chat_id")]
        public string ChatId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("parse_mode")]
        public string? ParseMode { get; set; }

        [JsonPropertyName("disable_web_page_preview")]
        public bool DisableWebPagePreview { get; set; }
    }

    public class BaleSendMessageResponseDto
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public BaleMessageDto? Result { get; set; }

        [JsonPropertyName("error_code")]
        public int? ErrorCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        // Not part of Bale's payload; set locally from the HTTP response for logging.
        [JsonIgnore]
        public int HttpStatusCode { get; set; }
    }

    public class BaleMessageDto
    {
        [JsonPropertyName("message_id")]
        public long MessageId { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }
    }
}