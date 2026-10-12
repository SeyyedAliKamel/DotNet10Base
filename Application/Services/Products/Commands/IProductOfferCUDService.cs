using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.Baskets;
using Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;

namespace Application.Services.Products.Commands
{
    // ==================== Interface ====================
    public interface IProductOfferCUDService
    {
        ResultDto<ProductOfferResultDto> AddProductOffer(RequestAddProductOfferDto request, long currentUserId);
        ResultDto<ProductOfferResultDto> EditProductOffer(RequestEditProductOfferDto request, long currentUserId);
        ResultDto RemoveProductOffer(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ProductOfferCUDService : IProductOfferCUDService
    {
        private readonly IDataBaseContext _context;
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        public ProductOfferCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Add --------------------
        public ResultDto<ProductOfferResultDto> AddProductOffer(RequestAddProductOfferDto request, long currentUserId)
        {
            if (!TryParseDate(request.OfferStart, out var offerStart))
                return FailData<ProductOfferResultDto>($"فرمت تاریخ شروع تخفیف صحیح نیست. فرمت مورد نیاز: {DateFormat}");

            if (!TryParseDate(request.OfferEnd, out var offerEnd))
                return FailData<ProductOfferResultDto>($"فرمت تاریخ پایان تخفیف صحیح نیست. فرمت مورد نیاز: {DateFormat}");

            if (request.OfferPercentage <= 0 || request.OfferPercentage > 100)
                return FailData<ProductOfferResultDto>("درصد تخفیف باید بین ۱ تا ۹۹ باشد");

            var productOffer = new ProductOffer
            {
                ProductId = request.ProductId,
                OfferDescription = request.OfferDescription,
                OfferPercentage = request.OfferPercentage,
                OfferStatus = request.OfferStatus,
                OfferStart = offerStart,
                OfferEnd = offerEnd,
                OfferLimit = request.OfferLimit
            };

            _context.SetUserId(currentUserId);
            _context.ProductOffers.Add(productOffer);
            _context.SaveChanges();

            return new ResultDto<ProductOfferResultDto>
            {
                Data = MapToResult(productOffer),
                IsSuccess = true,
                Message = "تخفیف محصول با موفقیت ایجاد گردید",
                Status = 200
            };
        }

        // -------------------- Edit --------------------
        public ResultDto<ProductOfferResultDto> EditProductOffer(RequestEditProductOfferDto request, long currentUserId)
        {
            var productOffer = _context.ProductOffers.FirstOrDefault(p => p.Id == request.Id);
            if (productOffer == null)
                return new ResultDto<ProductOfferResultDto> { IsSuccess = false, Message = "تخفیف محصول یافت نشد", Status = 400 };
            if (!TryParseDate(request.OfferStart, out var offerStart))
                return FailData<ProductOfferResultDto>($"فرمت تاریخ شروع تخفیف صحیح نیست. فرمت مورد نیاز: {DateFormat}");
            if (!TryParseDate(request.OfferEnd, out var offerEnd))
                return FailData<ProductOfferResultDto>($"فرمت تاریخ پایان تخفیف صحیح نیست. فرمت مورد نیاز: {DateFormat}");
            if (request.OfferPercentage <= 0 || request.OfferPercentage > 100)
                return FailData<ProductOfferResultDto>("درصد تخفیف باید بین ۱ تا ۹۹ باشد");

            var oldProductId = productOffer.ProductId;

            productOffer.ProductId = request.ProductId;
            productOffer.OfferDescription = request.OfferDescription;
            productOffer.OfferPercentage = request.OfferPercentage;
            productOffer.OfferStatus = request.OfferStatus;
            productOffer.OfferStart = offerStart;
            productOffer.OfferEnd = offerEnd;
            productOffer.OfferLimit = request.OfferLimit;

            _context.SetUserId(currentUserId);
            _context.SaveChanges();

            SyncBasketItemDiscountsForProduct(oldProductId);
            if (request.ProductId != oldProductId)
                SyncBasketItemDiscountsForProduct(request.ProductId);

            return new ResultDto<ProductOfferResultDto>
            {
                Data = MapToResult(productOffer),
                IsSuccess = true,
                Message = "تخفیف محصول با موفقیت به روز گردید",
                Status = 200
            };
        }

        // -------------------- Remove --------------------
        public ResultDto RemoveProductOffer(long id, long currentUserId)
        {
            var productOffer = _context.ProductOffers.Find(id);
            if (productOffer == null)
                return new ResultDto { IsSuccess = false, Message = "تخفیف محصول یافت نشد", Status = 400 };

            var productId = productOffer.ProductId;

            _context.SetUserId(currentUserId);
            _context.ProductOffers.Remove(productOffer);
            _context.SaveChanges();

            SyncBasketItemDiscountsForProduct(productId);

            return new ResultDto { IsSuccess = true, Message = "تخفیف محصول با موفقیت حذف گردید", Status = 200 };
        }

        // ==================== Helpers ====================

        // Parses "yyyy-MM-dd HH:mm:ss" strings into DateTime?. Empty/null input is treated as "no date" (valid).
        private static bool TryParseDate(string? value, out DateTime? result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = null;
                return true;
            }

            if (DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                result = parsed;
                return true;
            }

            result = null;
            return false;
        }

        private void SyncBasketItemDiscountsForProduct(long productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
                return;

            var affectedItems = _context.BasketItems
                .Include(bi => bi.Basket)
                .Where(bi => bi.ProductId == productId
                          && bi.Basket.BasketStatus == BasketStatus.Created)
                .ToList();

            if (!affectedItems.Any())
                return;

            var (offerDiscount, offerDiscountType) = ResolveOfferDiscount(product);

            var touchedBaskets = new List<Basket>();

            foreach (var item in affectedItems)
            {
                var isOfferManaged = item.DiscountType == null || item.DiscountType == DiscountTypeProductOffer;
                if (!isOfferManaged)
                    continue;

                if (item.Discount == offerDiscount && item.DiscountType == offerDiscountType)
                    continue;

                item.Discount = offerDiscount;
                item.DiscountType = offerDiscountType;
                _context.BasketItems.Update(item);
                touchedBaskets.Add(item.Basket);
            }

            if (!touchedBaskets.Any())
                return;

            _context.SaveChanges();
        }

        private (long Discount, int? DiscountType) ResolveOfferDiscount(Product product)
        {
            var now = DateTime.UtcNow;

            var activeOffer = _context.ProductOffers
                .Where(o => o.ProductId == product.Id)
                .Where(o => o.OfferStatus == OfferStatus.Active)
                .Where(o => o.OfferStart == null || o.OfferStart <= now)
                .Where(o => o.OfferEnd == null || o.OfferEnd >= now)
                .OrderByDescending(o => o.Id)
                .FirstOrDefault();

            if (activeOffer == null)
                return (0, null);

            return ((long)activeOffer.OfferPercentage, DiscountTypeProductOffer);
        }

        private const int DiscountTypeProductOffer = 1;

        private static ProductOfferResultDto MapToResult(ProductOffer productOffer) => new()
        {
            Id = productOffer.Id,
            ProductId = productOffer.ProductId,
            OfferDescription = productOffer.OfferDescription,
            OfferPercentage = productOffer.OfferPercentage,
            OfferStatus = productOffer.OfferStatus,
            OfferStart = productOffer.OfferStart,
            OfferEnd = productOffer.OfferEnd,
            OfferLimit = productOffer.OfferLimit
        };

        private static ResultDto<T> FailData<T>(string message, int status = 400) =>
            new ResultDto<T> { Data = default, IsSuccess = false, Message = message, Status = status };
    }

    // ==================== DTOs ====================

    public class RequestAddProductOfferDto
    {
        [Required(ErrorMessage = "محصول الزامی است.")]
        public long ProductId { get; set; }
        public string? OfferDescription { get; set; }
        [Required(ErrorMessage = "درصد تخفیف الزامی است.")]
        public float OfferPercentage { get; set; }
        public OfferStatus OfferStatus { get; set; } = OfferStatus.Inactive;
        public string? OfferStart { get; set; }
        public string? OfferEnd { get; set; }
        public int? OfferLimit { get; set; }
    }

    public class RequestEditProductOfferDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string? OfferDescription { get; set; }
        public float OfferPercentage { get; set; }
        public OfferStatus OfferStatus { get; set; }
        public string? OfferStart { get; set; }
        public string? OfferEnd { get; set; }
        public int? OfferLimit { get; set; }
    }

    public class ProductOfferResultDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string? OfferDescription { get; set; }
        public float OfferPercentage { get; set; }
        public OfferStatus OfferStatus { get; set; }
        public DateTime? OfferStart { get; set; }
        public DateTime? OfferEnd { get; set; }
        public int? OfferLimit { get; set; }
    }
}