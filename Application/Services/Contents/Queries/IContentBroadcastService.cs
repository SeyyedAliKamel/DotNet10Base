using Application.ConnectedServices;
using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Contents.Commands
{
    // ==================== Interface ====================
    public interface IContentBroadcastService
    {
        ResultDto<ContentBroadcastResultDto> PostContentToAllGroups(long contentId);
    }

    // ==================== Implementation ====================
    public class ContentBroadcastService : IContentBroadcastService
    {
        // Bale/Telegram-style bots are rate-limited to roughly 30 msgs/sec spread across
        // *different* chats. A small per-message delay keeps a large broadcast well under
        // that without needing a queue/background job for a first version.
        private const int DelayBetweenMessagesMs = 40;

        // ==================== FILL THIS IN ====================
        // The Bale chat IDs to broadcast to. Group IDs are negative numbers, e.g. "-1001234567890".
        // Get one by adding the bot to the group, sending any message, then checking the
        // incoming update (or the getUpdates response) for "chat":{"id": ...}.
        private static readonly string[] TargetGroupChatIds =
        {
            "6381110139",
        };
        // =========================================================

        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;
        private readonly IBaleGatewayClient _baleClient;

        public ContentBroadcastService(
            IDataBaseContext context,
            IConfiguration configuration,
            IBaleGatewayClient baleClient)
        {
            _context = context;
            _configuration = configuration;
            _baleClient = baleClient;
        }

        // -------------------- PostContentToAllGroups --------------------
        // Sends one content item as a message to every active group the bot is a member
        // of. Best-effort per chat: one group failing (bot kicked, rate-limited, chat
        // deleted, ...) never stops the rest of the broadcast.
        public ResultDto<ContentBroadcastResultDto> PostContentToAllGroups(long contentId)
        {
            try
            {
                var content = _context.Contents
                    .Where(c => c.Id == contentId && c.IsActive && !c.IsDeleted)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.ShortDescription
                    })
                    .FirstOrDefault();

                if (content == null)
                {
                    return new ResultDto<ContentBroadcastResultDto>
                    {
                        IsSuccess = false,
                        Status = 404,
                        Message = "محتوا یافت نشد یا فعال نیست"
                    };
                }

                var slug = _context.Slugs
                    .Where(s => s.ReferenceId == content.Id && s.Type == SlugTypeEnum.Content)
                    .Select(s => s.Title)
                    .FirstOrDefault();

                var messageText = BuildBroadcastMessage(content.Name, content.ShortDescription, slug);

                var groups = TargetGroupChatIds.Where(id => !string.IsNullOrWhiteSpace(id) && id != "PUT_GROUP_ID_HERE").ToList();

                if (groups.Count == 0)
                {
                    return new ResultDto<ContentBroadcastResultDto>
                    {
                        Data = new ContentBroadcastResultDto
                        {
                            ContentId = contentId,
                            TotalGroups = 0,
                            SuccessCount = 0,
                            FailedCount = 0
                        },
                        IsSuccess = false,
                        Status = 500,
                        Message = "هیچ گروهی برای ارسال تنظیم نشده است (TargetGroupChatIds خالی است)"
                    };
                }

                var failedChats = new List<BroadcastFailureDto>();
                var successCount = 0;

                foreach (var chatId in groups)
                {
                    var sendResult = _baleClient.SendMessage(chatId, messageText);

                    if (sendResult.Ok)
                    {
                        successCount++;
                    }
                    else
                    {
                        failedChats.Add(new BroadcastFailureDto
                        {
                            ChatId = chatId,
                            Error = sendResult.Description ?? $"HTTP {sendResult.HttpStatusCode}"
                        });

                        // Chat IDs are hardcoded, so there's nothing to auto-deactivate here —
                        // if one starts failing (bot kicked, chat deleted), remove it from
                        // TargetGroupChatIds by hand.
                    }

                    if (DelayBetweenMessagesMs > 0)
                        Thread.Sleep(DelayBetweenMessagesMs);
                }

                if (failedChats.Count > 0)
                {
                    LogBroadcastFailures(contentId, failedChats);
                }

                _context.SaveChanges();

                return new ResultDto<ContentBroadcastResultDto>
                {
                    Data = new ContentBroadcastResultDto
                    {
                        ContentId = contentId,
                        TotalGroups = groups.Count,
                        SuccessCount = successCount,
                        FailedCount = failedChats.Count,
                        FailedChats = failedChats
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = failedChats.Count == 0
                        ? "محتوا در تمام گروه‌ها ارسال شد"
                        : $"محتوا در {successCount} گروه ارسال شد، {failedChats.Count} مورد ناموفق بود"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ContentBroadcastResultDto>(ex);
            }
        }

        // ==================== Helpers ====================

        private string BuildBroadcastMessage(string name, string? shortDescription, string? slug)
        {
            var baseUrl = (_configuration.GetSection("AppSettings")["FrontendBaseUrl"] ?? string.Empty).TrimEnd('/');
            var link = !string.IsNullOrEmpty(slug) ? "danilooscoffee.com" + $"{baseUrl}/content/{slug}" : baseUrl;

            var text = $"{name}";

            if (!string.IsNullOrWhiteSpace(shortDescription))
                text += $"\n\n{shortDescription}";

            if (!string.IsNullOrEmpty(link))
                text += $"\n\n{link}";

            return text;
        }

        private void LogBroadcastFailures(long contentId, List<BroadcastFailureDto> failures)
        {
            foreach (var failure in failures)
            {
                _context.HandleExceptions.Add(new HandleException
                {
                    Message = $"ContentBroadcast contentId={contentId} chatId={failure.ChatId} ({failure.Title}): {failure.Error}",
                    StackTrace = null
                });
            }
        }

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
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
    }

    // ==================== DTOs ====================
    public class ContentBroadcastResultDto
    {
        public long ContentId { get; set; }
        public int TotalGroups { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<BroadcastFailureDto> FailedChats { get; set; } = new();
    }

    public class BroadcastFailureDto
    {
        public string ChatId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}