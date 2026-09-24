using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using PaypalServerSdk.Standard.Models;
using System.Linq;
using System.Reflection;
using static Common.Helper;

namespace Application.Services.Products.Commands
{
    // ==================== Interface ====================
    public interface IReviewCUDService
    {
        // -------- Admin (unrestricted) --------
        ResultDto<ReviewResultDto> AddReview(RequestAddReviewDto request, long currentUserId);
        ResultDto<ReviewResultDto> EditReview(RequestEditReviewDto request, long currentUserId);
        ResultDto RemoveReview(long id, long currentUserId);

        // -------- Frontend (owner-only) --------
        ResultDto<ReviewResultDto> AddOrUpdateReviewFrontEnd(RequestAddOrUpdateReviewFrontEndDto request, long currentUserId);
        ResultDto<ReviewResultDto> EditReviewFrontEnd(RequestEditReviewFrontEndDto request, long currentUserId);
        ResultDto RemoveReviewFrontEnd(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ReviewCUDService : IReviewCUDService
    {
        private readonly IDataBaseContext _context;

        public ReviewCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Add (Admin + Frontend share this) --------------------
        public ResultDto<ReviewResultDto> AddReview(RequestAddReviewDto request, long currentUserId)
        {
            try
            {
                string fullName;
                string? mobile;
                long? userId;

                if (currentUserId != 0)
                {
                    var user = _context.Users.FirstOrDefault(u => u.Id == currentUserId);
                    fullName = user?.FirstName + " " + user?.LastName;
                    mobile = user?.Mobile;
                    userId = currentUserId;
                }
                else
                {
                    fullName = request.FullName;
                    mobile = request.Mobile;
                    userId = null;
                }

                var (isValid, normalized) = MobileValidator.Normalize(mobile);
                if (!isValid)
                    return new ResultDto<ReviewResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Message = "شماره موبایل معتبر نیست",
                        Status = 200
                    };

                var review = new Review
                {
                    UserId = userId,
                    FullName = fullName,
                    Mobile = mobile,
                    Description = request.Description,
                    Rate = ClampRate(request.Rate),
                    ReferenceId = request.ReferenceId,
                    Type = request.Type,
                    IsActive = request.IsActive,
                };

                _context.SetUserId(currentUserId);
                _context.Reviews.Add(review);
                _context.SaveChanges();

                return new ResultDto<ReviewResultDto>
                {
                    Data = MapToResult(review),
                    IsSuccess = true,
                    Message = "نظر شما با موفقیت ثبت گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ReviewResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Edit (Admin, unrestricted) --------------------
        public ResultDto<ReviewResultDto> EditReview(RequestEditReviewDto request, long currentUserId)
        {
            try
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == request.Id);

                if (review == null)
                    return new ResultDto<ReviewResultDto> { IsSuccess = false, Message = "نظر یافت نشد", Status = 400 };

                review.Description = request.Description;
                review.Rate = ClampRate(request.Rate);
                review.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.SaveChanges();

                return new ResultDto<ReviewResultDto>
                {
                    Data = MapToResult(review),
                    IsSuccess = true,
                    Message = "نظر شما با موفقیت به روز گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ReviewResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove (Admin, unrestricted) --------------------
        public ResultDto RemoveReview(long id, long currentUserId)
        {
            try
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == id);

                if (review == null)
                    return new ResultDto { IsSuccess = false, Message = "نظر یافت نشد", Status = 400 };

                _context.SetUserId(currentUserId);
                _context.Reviews.Remove(review);
                _context.SaveChanges();

                return new ResultDto { IsSuccess = true, Message = "نظر با موفقیت حذف گردید", Status = 200 };
            }
            catch (Exception ex)
            {
                return HandleException(ex, id.ToJson(), currentUserId);
            }
        }

        // -------------------- Add or Update (Frontend, basket-verified) --------------------
        public ResultDto<ReviewResultDto> AddOrUpdateReviewFrontEnd(RequestAddOrUpdateReviewFrontEndDto request, long currentUserId)
        {
            try
            {
                var type = ReviewTypeEnum.Product;
                var user = _context.Users.FirstOrDefault(u => u.Id == currentUserId);
                var fullName = user?.FirstName + " " + user?.LastName;
                var mobile = user?.Mobile;

                if (currentUserId == 0)
                    return new ResultDto<ReviewResultDto> { IsSuccess = false, Message = "برای ثبت نظر باید وارد شوید", Status = 401 };

                var basket = _context.Baskets
                    .Include(b => b.BasketItems)
                    .FirstOrDefault(b => b.Id == request.BasketId && b.UserId == currentUserId);

                if (basket == null)
                    return new ResultDto<ReviewResultDto> { IsSuccess = false, Message = "سبد خرید یافت نشد", Status = 404 };

                if (basket.BasketStatus != BasketStatus.Sent && basket.BasketStatus != BasketStatus.Finalized)
                    return new ResultDto<ReviewResultDto> { IsSuccess = false, Message = "امکان ثبت نظر برای این سبد خرید وجود ندارد", Status = 400 };

                var hasProduct = basket.BasketItems.Any(i => i.ProductId == request.ReferenceId);
                if (!hasProduct)
                    return new ResultDto<ReviewResultDto> { IsSuccess = false, Message = "این محصول در سبد خرید شما یافت نشد", Status = 400 };

                var review = _context.Reviews.FirstOrDefault(r =>
                    r.UserId == currentUserId &&
                    r.ReferenceId == request.ReferenceId &&
                    r.Type == type);

                var isNew = review == null;

                if (isNew)
                {
                    review = new Review
                    {
                        UserId = currentUserId,
                        FullName = fullName,
                        Mobile = mobile,
                        ReferenceId = request.ReferenceId,
                        Type = type,
                        BasketId = request.BasketId,
                    };
                    _context.Reviews.Add(review);
                }
                else
                {
                    review.BasketId = request.BasketId;
                }

                review.Description = request.Description;
                review.Rate = ClampRate(request.Rate);
                review.IsActive = false;

                _context.SetUserId(currentUserId);
                _context.SaveChanges();

                return new ResultDto<ReviewResultDto>
                {
                    Data = MapToResult(review),
                    IsSuccess = true,
                    Message = isNew ? "نظر شما با موفقیت ثبت گردید" : "نظر شما با موفقیت به روز گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ReviewResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Edit (Frontend, owner-only) --------------------
        public ResultDto<ReviewResultDto> EditReviewFrontEnd(RequestEditReviewFrontEndDto request, long currentUserId)
        {
            try
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == request.Id);

                if (review == null)
                    return new ResultDto<ReviewResultDto> { IsSuccess = false, Message = "نظر یافت نشد", Status = 404 };

                if (review.UserId != currentUserId)
                    return new ResultDto<ReviewResultDto> { IsSuccess = false, Message = "شما اجازه ویرایش این نظر را ندارید", Status = 403 };

                review.Description = request.Description;
                review.IsActive = false;

                _context.SetUserId(currentUserId);
                _context.SaveChanges();

                return new ResultDto<ReviewResultDto>
                {
                    Data = MapToResult(review),
                    IsSuccess = true,
                    Message = "نظر شما با موفقیت به روز گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ReviewResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove (Frontend, owner-only) --------------------
        public ResultDto RemoveReviewFrontEnd(long id, long currentUserId)
        {
            try
            {
                var review = _context.Reviews.FirstOrDefault(r => r.Id == id);

                if (review == null)
                    return new ResultDto { IsSuccess = false, Message = "نظر یافت نشد", Status = 404 };

                if (review.UserId != currentUserId)
                    return new ResultDto { IsSuccess = false, Message = "شما اجازه حذف این نظر را ندارید", Status = 403 };

                _context.SetUserId(currentUserId);
                _context.Reviews.Remove(review);
                _context.SaveChanges();

                return new ResultDto { IsSuccess = true, Message = "نظر با موفقیت حذف گردید", Status = 200 };
            }
            catch (Exception ex)
            {
                return HandleException(ex, id.ToJson(), currentUserId);
            }
        }

        // ==================== Helpers ====================
        private static int ClampRate(int rate) => rate < 0 ? 0 : rate > 5 ? 5 : rate;

        private static ReviewResultDto MapToResult(Review review) => new()
        {
            Id = review.Id,
            UserId = review.UserId,
            Description = review.Description,
            FullName = review.FullName,
            Rate = review.Rate,
            ReferenceId = review.ReferenceId
        };

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }

        private ResultDto<T> HandleException<T>(Exception ex, string requestParam, long currentUserId)
        {
            DetachAll();

            var item = new HandleException
            {
                Message = ex.InnerException?.Message ?? ex.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = currentUserId,
            };
            _context.HandleExceptions.Add(item);
            _context.SaveChanges();

            return new ResultDto<T>
            {
                IsSuccess = false,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                Status = 400,
            };
        }

        private ResultDto HandleException(Exception ex, string requestParam, long currentUserId)
        {
            DetachAll();

            var item = new HandleException
            {
                Message = ex.InnerException?.Message ?? ex.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = currentUserId,
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
    public class RequestAddReviewDto
    {
        public string Description { get; set; }
        public int Rate { get; set; }
        public long ReferenceId { get; set; }
        public ReviewTypeEnum Type { get; set; }
        public bool IsActive { get; set; }
        public string FullName { get; set; }
        public string? Mobile { get; set; }
    }

    public class RequestEditReviewDto
    {
        public long Id { get; set; }
        public string Description { get; set; }
        public int Rate { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestEditReviewFrontEndDto
    {
        public long Id { get; set; }
        public string Description { get; set; }
    }

    public class ReviewResultDto
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string Description { get; set; }
        public string FullName { get; set; }
        public string? Mobile { get; set; }
        public int Rate { get; set; }
        public long ReferenceId { get; set; }
    }

    public class RequestAddReviewFrontEndDto
    {
        public long ReferenceId { get; set; }
        public string Description { get; set; }
        public ReviewTypeEnum Type { get; set; }
        public string FullName { get; set; }
        public string? Mobile { get; set; }
    }

    public class RequestAddOrUpdateReviewFrontEndDto
    {
        public long BasketId { get; set; }
        public long ReferenceId { get; set; }
        public string Description { get; set; }
        public int Rate { get; set; }
    }
}