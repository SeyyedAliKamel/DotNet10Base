using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdditionalInformations.Queries
{
    // ==================== Interface ====================
    public interface IReviewService
    {
        ResultDto<List<ReviewDto>> GetReviewList(RequestListDto request);
        ResultDto<ReviewItemDto> GetReviewItem(long id);

        // -------- Frontend-facing (active only) --------
        // Reviews written by a specific user (e.g. "my reviews")
        ResultDto<List<ReviewDto>> GetReviewListFrontEnd(RequestListDto request, long userId);
        ResultDto<List<ReviewDto>> GetReviewListByTypeFrontEnd(RequestReviewListByTypeDto request);
        ResultDto<ReviewItemDto> GetReviewItemFrontEnd(long id);
        ResultDto<ReviewUserProductDto> GetReviewUserProduct(long basketItemId, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ReviewService : IReviewService
    {
        private readonly IDataBaseContext _context;

        public ReviewService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList (admin) --------------------
        public ResultDto<List<ReviewDto>> GetReviewList(RequestListDto request)
        {
            try
            {
                var query = _context.Reviews
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

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new ReviewDto
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        Description = p.Description,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Rate = p.Rate,
                        ReferenceId = p.ReferenceId,
                        ReferenceName = p.Type == ReviewTypeEnum.Product
                            ? _context.Products
                                .Where(pr => pr.Id == p.ReferenceId)
                                .Select(pr => pr.Name)
                                .FirstOrDefault()
                            : _context.Contents
                                .Where(c => c.Id == p.ReferenceId)
                                .Select(c => c.Name)
                                .FirstOrDefault(),
                        Type = p.Type,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ReviewDto>>
                {
                    Data = list,
                    Result = new ResultListDto { Current = page, Count = rowsCount },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست نظرات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ReviewDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd (current user only) --------------------
        public ResultDto<List<ReviewDto>> GetReviewListFrontEnd(RequestListDto request, long userId)
        {
            try
            {
                var query = _context.Reviews
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted && r.UserId == userId)
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderByDescending(s => s.CreatedAt);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new ReviewDto
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        Description = p.Description,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Rate = p.Rate,
                        ReferenceId = p.ReferenceId,
                        ReferenceName = p.Type == ReviewTypeEnum.Product
                            ? _context.Products
                                .Where(pr => pr.Id == p.ReferenceId)
                                .Select(pr => pr.Name)
                                .FirstOrDefault()
                            : _context.Contents
                                .Where(c => c.Id == p.ReferenceId)
                                .Select(c => c.Name)
                                .FirstOrDefault(),
                        Type = p.Type,
                        IsActive = p.IsActive,
                        BasketId = p.BasketId,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                foreach (var item in list)
                {
                    item.Mobile = MaskMobile(item.Mobile);
                }

                return new ResultDto<List<ReviewDto>>
                {
                    Data = list,
                    Result = new ResultListDto { Current = page, Count = rowsCount },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست نظرات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ReviewDto>>(ex);
            }
        }

        // -------------------- GetListByTypeFrontEnd (public reviews for a product/content) --------------------
        public ResultDto<List<ReviewDto>> GetReviewListByTypeFrontEnd(RequestReviewListByTypeDto request)
        {
            try
            {
                var query = _context.Reviews
                    .AsNoTracking()
                    .Where(r => r.IsActive && !r.IsDeleted && r.Type == request.Type && r.ReferenceId == request.ReferenceId)
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderByDescending(s => s.CreatedAt);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new ReviewDto
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        Description = p.Description,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Rate = p.Rate,
                        ReferenceId = p.ReferenceId,
                        ReferenceName = p.Type == ReviewTypeEnum.Product
                            ? _context.Products
                                .Where(pr => pr.Id == p.ReferenceId)
                                .Select(pr => pr.Name)
                                .FirstOrDefault()
                            : _context.Contents
                                .Where(c => c.Id == p.ReferenceId)
                                .Select(c => c.Name)
                                .FirstOrDefault(),
                        Type = p.Type,
                        HasPurchased = p.BasketId != null,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                foreach (var item in list)
                {
                    item.Mobile = MaskMobile(item.Mobile);
                }

                return new ResultDto<List<ReviewDto>>
                {
                    Data = list,
                    Result = new ResultListDto { Current = page, Count = rowsCount },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست نظرات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ReviewDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ReviewItemDto> GetReviewItem(long id)
        {
            try
            {
                var review = _context.Reviews
                    .AsNoTracking()
                    .Where(r => r.Id == id)
                    .Select(p => new ReviewItemDto
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        Description = p.Description,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Rate = p.Rate,
                        ReferenceId = p.ReferenceId,
                        ReferenceName = p.Type == ReviewTypeEnum.Product
                            ? _context.Products
                                .Where(pr => pr.Id == p.ReferenceId)
                                .Select(pr => pr.Name)
                                .FirstOrDefault()
                            : _context.Contents
                                .Where(c => c.Id == p.ReferenceId)
                                .Select(c => c.Name)
                                .FirstOrDefault(),
                        Type = p.Type,
                        BasketId = p.BasketId,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (review == null)
                {
                    return new ResultDto<ReviewItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "نظر یافت نشد"
                    };
                }

                return new ResultDto<ReviewItemDto>
                {
                    Data = review,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات نظر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ReviewItemDto>(ex);
            }
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ReviewItemDto> GetReviewItemFrontEnd(long id)
        {
            try
            {
                var review = _context.Reviews
                    .AsNoTracking()
                    .Where(r => r.Id == id && !r.IsDeleted)
                    .Select(p => new ReviewItemDto
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        Description = p.Description,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Rate = p.Rate,
                        ReferenceId = p.ReferenceId,
                        ReferenceName = p.Type == ReviewTypeEnum.Product
                            ? _context.Products
                                .Where(pr => pr.Id == p.ReferenceId)
                                .Select(pr => pr.Name)
                                .FirstOrDefault()
                            : _context.Contents
                                .Where(c => c.Id == p.ReferenceId)
                                .Select(c => c.Name)
                                .FirstOrDefault(),
                        Type = p.Type,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (review != null)
                {
                    review.Mobile = MaskMobile(review.Mobile);
                }

                if (review == null)
                {
                    return new ResultDto<ReviewItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "نظر یافت نشد"
                    };
                }

                return new ResultDto<ReviewItemDto>
                {
                    Data = review,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات نظر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ReviewItemDto>(ex);
            }
        }

        // -------------------- GetReviewUserProduct (owner-only, per basket item) --------------------
        public ResultDto<ReviewUserProductDto> GetReviewUserProduct(long basketItemId, long currentUserId)
        {
            try
            {
                if (currentUserId == 0)
                    return new ResultDto<ReviewUserProductDto>
                    {
                        IsSuccess = false,
                        Status = 401,
                        Message = "برای مشاهده نظر باید وارد شوید"
                    };

                var basketItem = _context.BasketItems
                    .AsNoTracking()
                    .Where(bi => bi.Id == basketItemId)
                    .Select(bi => new
                    {
                        bi.Id,
                        bi.BasketId,
                        bi.ProductId,
                        BasketUserId = bi.Basket.UserId,
                        bi.Basket.BasketStatus,
                        ProductName = _context.Products
                            .Where(pr => pr.Id == bi.ProductId)
                            .Select(pr => pr.Name)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();

                if (basketItem == null)
                    return new ResultDto<ReviewUserProductDto>
                    {
                        IsSuccess = false,
                        Status = 404,
                        Message = "آیتم سبد خرید یافت نشد"
                    };

                if (basketItem.BasketUserId != currentUserId)
                    return new ResultDto<ReviewUserProductDto>
                    {
                        IsSuccess = false,
                        Status = 403,
                        Message = "دسترسی به این سبد خرید امکان‌پذیر نیست"
                    };

                var review = _context.Reviews
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted
                             && r.UserId == currentUserId
                             && r.Type == ReviewTypeEnum.Product
                             && r.ReferenceId == basketItem.ProductId)
                    .Select(p => new ReviewItemDto
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        Description = p.Description,
                        FullName = p.FullName,
                        Mobile = p.Mobile,
                        Rate = p.Rate,
                        ReferenceId = p.ReferenceId,
                        ReferenceName = basketItem.ProductName,
                        Type = p.Type,
                        IsActive = p.IsActive,
                        BasketId = p.BasketId,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (review != null)
                    review.Mobile = MaskMobile(review.Mobile);

                return new ResultDto<ReviewUserProductDto>
                {
                    Data = new ReviewUserProductDto
                    {
                        BasketItemId = basketItem.Id,
                        BasketId = basketItem.BasketId,
                        ProductId = basketItem.ProductId,
                        ProductName = basketItem.ProductName,
                        Review = review,
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = review == null ? "نظری برای این محصول ثبت نشده است" : "اطلاعات نظر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ReviewUserProductDto>(ex);
            }
        }

        // ==================== Helpers ====================
        private static string? MaskMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length <= 7)
                return mobile;

            var start = mobile.Substring(0, 4);
            var end = mobile.Substring(mobile.Length - 3, 3);

            return $"{start}****{end}";
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
    public class ReviewDto
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string Description { get; set; }
        public string FullName { get; set; }
        public string? Mobile { get; set; }
        public int Rate { get; set; }
        public long ReferenceId { get; set; }
        public string? ReferenceName { get; set; }
        public ReviewTypeEnum Type { get; set; }
        public bool IsActive { get; set; }
        public long? BasketId { get; set; }
        public bool HasPurchased { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ReviewItemDto
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string Description { get; set; }
        public string FullName { get; set; }
        public string? Mobile { get; set; }
        public int Rate { get; set; }
        public long ReferenceId { get; set; }
        public string? ReferenceName { get; set; }
        public ReviewTypeEnum Type { get; set; }
        public bool IsActive { get; set; }
        public long? BasketId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RequestReviewListByTypeDto
    {
        public ReviewTypeEnum Type { get; set; }
        public long ReferenceId { get; set; }
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ReviewUserProductDto
    {
        public long BasketItemId { get; set; }
        public long BasketId { get; set; }
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public ReviewItemDto? Review { get; set; }
        public List<ReviewImageDto> Images { get; set; } = new();
    }

    public class ReviewImageDto
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; }
        public string? ImageDescription { get; set; }
        public bool IsActive { get; set; } 
    }

    public class ReviewScoreDto
    {
        public int Total { get; set; }
        public int ReviewAmount { get; set; }
        public int ImageAmount { get; set; }
    }
}