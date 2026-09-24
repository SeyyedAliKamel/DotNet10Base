using Application.Interfaces.Contexts;
using Application.Services.Scores.Queries;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Baskets;
using Domain.Entities.HandleException;
using Domain.Entities.Scores;
using Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using System.CodeDom;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using static Common.Helper;

namespace Application.Services.Scores.Commands
{
    // ==================== Interface ====================
    public interface IScoreCUDService
    {
        ResultDto AddScore(RequestAddScoreDto request, long currentUserId);
        ResultDto EditScore(RequestEditScoreDto request, long currentUserId);
        ResultDto RemoveScore(int id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ScoreCUDService : IScoreCUDService
    {
        private readonly IDataBaseContext _context;
        private static readonly int OtpExpirationMinutes = 5;

        public ScoreCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto AddScore(RequestAddScoreDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var randomNumber = RandomNumberGenerator.GetInt32(0, 10_000_000);
                var code = $"{request.CampaignId ?? 0}{request.Amount:D2}{randomNumber:D7}";

                var score = new Score
                {
                    UserId = request.UserId,
                    Amount = request.Amount,
                    Description = request.Description,
                    Code = code,
                    Type = ScoreTypeEnum.Deposit
                };

                _context.SetUserId(currentUserId);
                _context.Scores.Add(score);
                _context.SaveChanges();
                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "امتیاز با موفقیت ثبت شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Update --------------------
        public ResultDto EditScore(RequestEditScoreDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var data = _context.Scores.FirstOrDefault(s => s.Id == request.Id);
                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = true,
                        Status = 200,
                        Message = "اطلاعات اشتباه است "
                    };

                data.Amount = request.Amount;
                data.Description = request.Description;

                _context.SetUserId(currentUserId);
                _context.Scores.Update(data);
                _context.SaveChanges();
                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "امتیاز با موفقیت ثبت شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Delete --------------------
        public ResultDto RemoveScore(int id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var data = _context.Scores.FirstOrDefault(s => s.Id == id);
                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "امتیاز یافت نشد",
                        Status = 200,
                    };
                _context.SetUserId(currentUserId);
                _context.Scores.Remove(data);
                _context.SaveChanges();
                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "امتیاز با موفقیت حذف شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException(ex, id.ToJson(), currentUserId);
            }
        }

        // ==================== Private Helpers ====================
        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
                entry.State = EntityState.Detached;
        }

        private ResultDto HandleException(Exception ex, string requestParam, long? userId)
        {
            DetachAll();

            var item = new HandleException
            {
                Message = ex.InnerException?.Message.ToString(),
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = userId,
            };
            _context.HandleExceptions.Add(item);
            _context.SaveChanges();

            return new ResultDto
            {
                IsSuccess = false,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                Status = 400,
            };
        }
    }

    // ==================== DTOs ====================
    public class RequestAddScoreDto
    {
        [Required(ErrorMessage = "کاربر الزامی است.")]
        public long UserId { get; set; }
        [Required(ErrorMessage = "امتیاز الزامی است.")]
        public int Amount { get; set; }
        public string? Description { get; set; }
        public int? CampaignId { get; set; }
    }

    public class RequestEditScoreDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
        public int? CampaignId { get; set; }
    }

    public class RequestGiftScoreDto
    {
        [Required(ErrorMessage = "امتیاز الزامی است.")]
        public long ScoreId { get; set; }

        [Required(ErrorMessage = "گیرنده الزامی است.")]
        public long ReceiverUserId { get; set; }
    }

    public class ResultBasketScoreDiscountDto
    {
        public int ScoreUsed { get; set; }
        public long DiscountAmount { get; set; }
        public long TotalPrice { get; set; }
        public long FinalPrice { get; set; }
    }
}