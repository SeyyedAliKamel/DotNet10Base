using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Products.Queries
{
    // ==================== Interface ====================
    public interface IProductOfferService
    {
        ResultDto<List<ProductOfferDto>> GetProductOfferList(RequestProductOfferListDto request);
        ResultDto<ProductOfferItemDto> GetProductOfferItem(long id);
    }

    // ==================== Implementation ====================
    public class ProductOfferService : IProductOfferService
    {
        private readonly IDataBaseContext _context;

        public ProductOfferService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<ProductOfferDto>> GetProductOfferList(RequestProductOfferListDto request)
        {
            try
            {
                var query = _context.ProductOffers
                    .AsNoTracking()
                    .AsQueryable();

                if (request.ProductId.HasValue)
                {
                    query = query.Where(p => p.ProductId == request.ProductId);
                }

                if (request.OfferStatus.HasValue)
                {
                    query = query.Where(p => p.OfferStatus == request.OfferStatus);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new ProductOfferDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        ProductName = _context.Products
                            .Where(prod => prod.Id == p.ProductId)
                            .Select(prod => prod.Name)
                            .FirstOrDefault(),
                        OfferPercentage = p.OfferPercentage,
                        OfferStatus = p.OfferStatus,
                        OfferEnd = p.OfferEnd,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ProductOfferDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست تخفیف محصولات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductOfferDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ProductOfferItemDto> GetProductOfferItem(long id)
        {
            try
            {
                var productOffer = _context.ProductOffers
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new ProductOfferItemDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        ProductName = _context.Products
                            .IgnoreQueryFilters()
                            .Where(prod => prod.Id == p.ProductId)
                            .Select(prod => prod.Name)
                            .FirstOrDefault(),
                        OfferDescription = p.OfferDescription,
                        OfferPercentage = p.OfferPercentage,
                        OfferStatus = p.OfferStatus,
                        OfferStart = p.OfferStart,
                        OfferEnd = p.OfferEnd,
                        OfferLimit = p.OfferLimit,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (productOffer == null)
                {
                    return new ResultDto<ProductOfferItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "تخفیف محصول یافت نشد"
                    };
                }

                return new ResultDto<ProductOfferItemDto>
                {
                    Data = productOffer,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات تخفیف محصول ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ProductOfferItemDto>(ex);
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
    public class ProductOfferDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public float OfferPercentage { get; set; }
        public OfferStatus OfferStatus { get; set; }
        public DateTime? OfferEnd { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductOfferItemDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string? OfferDescription { get; set; }
        public float OfferPercentage { get; set; }
        public OfferStatus OfferStatus { get; set; }
        public DateTime? OfferStart { get; set; }
        public DateTime? OfferEnd { get; set; }
        public int? OfferLimit { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RequestProductOfferListDto
    {
        public long? ProductId { get; set; }
        public OfferStatus? OfferStatus { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}