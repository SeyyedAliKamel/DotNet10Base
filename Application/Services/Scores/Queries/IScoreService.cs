using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Scores;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Scores.Queries
{
    // ==================== Interface ====================
    public interface IScoreService
    {
        ResultDto<List<ResultScoreListDto>> GetScoreList(RequestListDto request, long userId);
        ResultDto<ResultScoreItemDto> GetScoreItem(int id);
        ResultDto<ResultScoreTotalDto> GetScoreTotal(long userId);
        ResultDto CheckLoginScores(long userId);
    }

    // ==================== Implementation ====================
    public class ScoreService : IScoreService
    {
        private readonly IDataBaseContext _context;

        private const string DailyLoginKey = "attendance_daily_login";
        private const string AnnualSignUpKey = "loyalty_annual_sign_up";
        private const string BirthdayKey = "loyalty_birthday";
        private const string PageVisitKey = "interact_visit_product";

        public ScoreService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        // isLottery = true  -> returns ScoreTypeEnum.Lottery entries
        // isLottery = false -> returns Deposit/Withdraw entries (normal scores)
        public ResultDto<List<ResultScoreListDto>> GetScoreList(RequestListDto request, long userId)
        {
            try
            {
                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                int rowsCount = 0;

                var list = _context.Scores
                    .AsNoTracking()
                    .OrderByDescending(s => s.CreatedAt)
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(s => MapToListDto(s))
                    .ToList();

                return new ResultDto<List<ResultScoreListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultScoreListDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ResultScoreItemDto> GetScoreItem(int id)
        {
            try
            {
                var data = _context.Scores
                    .AsNoTracking()
                    .Where(s => s.Id == id)
                    .Select(s => new ResultScoreItemDto
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        UserFullName = _context.Users
                            .Where(u => u.Id == s.UserId)
                            .Select(u => u.FirstName + " " + u.LastName)
                            .FirstOrDefault(),
                        Amount = s.Amount,
                        Description = s.Description,
                        Key = s.Key,
                        ReferenceId = s.ReferenceId,
                        Code = s.Code,
                        Type = s.Type
                    })
                    .FirstOrDefault();

                if (data == null)
                {
                    return new ResultDto<ResultScoreItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "امتیاز یافت نشد"
                    };
                }

                return new ResultDto<ResultScoreItemDto>
                {
                    Data = data,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات امتیاز"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultScoreItemDto>(ex);
            }
        }

        public ResultDto CheckLoginScores(long userId)
        {
            try
            {
                var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 404,
                        Message = "کاربر یافت نشد"
                    };
                }
                var now = DateTime.Now;
                var today = now.Date;
                var awardedThisYear = _context.Scores
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && s.CreatedAt.Year == now.Year)
                    .Select(s => new { s.Key, s.CreatedAt })
                    .ToList();
                var awardedKeys = new List<string>();
                // -------- attendance_daily_login --------
                var alreadyToday = awardedThisYear.Any(s => s.Key == DailyLoginKey && s.CreatedAt.Date == today);
                if (!alreadyToday)
                {
                    awardedKeys.Add(DailyLoginKey);
                }
                // -------- loyalty_annual_sign_up --------
                var signUpDate = user.CreatedAt;
                if (now.Year != signUpDate.Year)
                {
                    var day = Math.Min(signUpDate.Day, DateTime.DaysInMonth(now.Year, signUpDate.Month)); // handles Feb 29
                    var anniversaryThisYear = new DateTime(now.Year, signUpDate.Month, day);
                    var alreadyAnniversary = awardedThisYear.Any(s => s.Key == AnnualSignUpKey);
                    if (today >= anniversaryThisYear && !alreadyAnniversary)
                    {
                        awardedKeys.Add(AnnualSignUpKey);
                    }
                }
                // -------- loyalty_birthday --------
                if (user.Birthday != null
                    && user.Birthday.Value.Month == today.Month
                    && user.Birthday.Value.Day == today.Day)
                {
                    var alreadyBirthday = awardedThisYear.Any(s => s.Key == BirthdayKey);
                    if (!alreadyBirthday)
                    {
                        awardedKeys.Add(BirthdayKey);
                    }
                }
                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = awardedKeys.Any()
                        ? "امتیاز(های) جدید ثبت شد: " + string.Join(", ", awardedKeys)
                        : "امتیاز جدیدی برای ثبت وجود نداشت"
                };
            }
            catch (Exception ex)
            {
                return new ResultDto
                {
                    IsSuccess = false,
                    Status = 500,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد"
                };
            }
        }

        // -------------------- GetTotal --------------------
        public ResultDto<ResultScoreTotalDto> GetScoreTotal(long userId)
        {
            try
            {
                var deposit = _context.Scores.AsNoTracking()
                    .Where(s => s.UserId == userId && s.Type == ScoreTypeEnum.Deposit)
                    .Sum(s => (int?)s.Amount) ?? 0;

                var withdraw = _context.Scores.AsNoTracking()
                    .Where(s => s.UserId == userId && s.Type == ScoreTypeEnum.Withdraw)
                    .Sum(s => (int?)s.Amount) ?? 0;

                return new ResultDto<ResultScoreTotalDto>
                {
                    Data = new ResultScoreTotalDto{
                     Score=   deposit - withdraw ,
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "مجموع امتیاز کاربر"
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // ==================== Helpers ====================
        private static ResultScoreListDto MapToListDto(Score s) => new()
        {
            Id = s.Id,
            UserId = s.UserId,
            UserFullName = null,
            Amount = s.Amount,
            Description = s.Description,
            Key = s.Key,
            ReferenceId = s.ReferenceId,
            Code = s.Code,
            Type = s.Type
        };

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
                // prevent secondary crash during logging
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
    public class ResultScoreItemDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? UserFullName { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
        public string? Key { get; set; }
        public long? ReferenceId { get; set; }
        public string Code { get; set; }
        public ScoreTypeEnum Type { get; set; }
    }

    public class ResultScoreListDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? UserFullName { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
        public string? Key { get; set; }
        public long? ReferenceId { get; set; }
        public string Code { get; set; }
        public ScoreTypeEnum Type { get; set; }
    }

    public class ResultScoreTotalDto
    {
        public int Score { get; set; }
    }
}