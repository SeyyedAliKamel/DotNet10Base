using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.Baskets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;

namespace Application.Services.Baskets.Commands
{
    public interface IBasketCUDService
    {
        ResultDto ChangeInvoiceAddress(long Id, long userId, bool isWholePrice = false);
        ResultDto ChangeBasketStatus(ChangeBasketStatusDto request, long currentUserId);
        Task<ResultDto> BasketPayment(RequestBasketPeymentDto request, long currentUserId);

        Task<ResultDto<ResultBasketPaymentVerificationDto>> BasketPaymentVerification(MellatCallbackModel model);
    }

    public class BasketCUDService : IBasketCUDService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public BasketCUDService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ResultDto ChangeInvoiceAddress(long Id, long userId, bool isWholePrice = false)
        {
            try
            {
                var basket = _context.Baskets
                    .FirstOrDefault(p => p.BasketStatus == BasketStatus.Created
                                       && p.UserId == userId);

                if (basket == null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 201,
                        Message = "سبد خرید یافت نشد."
                    };
                }

                if (basket.UserAddressId == Id)
                {
                    return new ResultDto
                    {
                        IsSuccess = true,
                        Status = 200,
                        Message = "آدرس سبد خرید با موفقیت تغییر یافت."
                    };
                }

                var userAddress = _context.UserAddresses.FirstOrDefault(p => p.Id == Id);

                if (userAddress == null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 201,
                        Message = "آدرس یافت نشد."
                    };
                }

                if (userAddress.UserId != userId)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 201,
                        Message = "این آدرس برای کاربر شما یافت نشد."
                    };
                }

                basket.UserAddressId = userAddress.Id;

                if (basket.ShippingId != null)
                {
                    var shipping = _context.Shippings.FirstOrDefault(s => s.Id == basket.ShippingId);

                    var cityStillAllowed = shipping != null
                        && IsCityInShippingList(shipping.CityIds, userAddress.CityId);

                    if (!cityStillAllowed)
                    {
                        basket.ShippingId = null;
                        basket.ShippingDescription = null;
                        basket.ShippingCost = 0;
                    }
                }

                _context.Baskets.Update(basket);
                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "آدرس سبد خرید با موفقیت تغییر یافت."
                };
            }
            catch (Exception ex)
            {
                return new ResultDto
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "خطایی رخ داد."
                };
            }
        }

        public async Task<ResultDto> BasketPayment(RequestBasketPeymentDto request, long currentUserId)
        {
            var basket = _context.Baskets.Include(p => p.UserAddress).Include(b => b.Shipping)
                .FirstOrDefault(p => p.Id == request.BasketId);

            if (basket != null && basket.UserAddress == null)
            {
                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "باید ابتدا آدرس خود را ثبت کنید",
                    Status = 200
                };
            }

            var productLimit = _context.BasketItems
                .Where(b => b.BasketId == request.BasketId)
                .Select(p => p.ProductId)
                .ToList();

            var basketItems = _context.BasketItems
                .Where(b => b.BasketId == request.BasketId)
                .ToList();

            // Calculate the total price considering discount (if any)
            long totalPrice = 0;

            foreach (var item in basketItems)
            {
                // Calculate price after discount (assuming Discount is in percentage)
                var discountedPrice = item.Price;

                if (item.DiscountType.HasValue && item.Discount > 0)
                {
                    discountedPrice = item.Price - (item.Price * item.Discount / 100);
                }

                // Add the price for the item multiplied by its count
                totalPrice += discountedPrice * item.Count;
            }

            long shippingCost = 0; // request.ShippingCost > 0 ? request.ShippingCost : (basket.Shipping?.Cost ?? 0);

            if (basket.UserAddress.CityId == 445 || basket.UserAddress.CityId == 437)
            {
                if (request.ShippingOption != 1 && request.ShippingOption != 2)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "ارور سرور",
                        Status = 200
                    };
                }

                if (request.ShippingOption == 1 || totalPrice <= 250000)
                {
                    shippingCost = 0; //50000;
                    // basket.ShippingDescription = "ارسال داخل شهر در روز های عادی";
                    basket.ShippingDescription = "ارسال داخل شهر و پرداخت هزینه ارسال هنگام تحویل";
                }
                else if (request.ShippingOption == 2 || totalPrice >= 250000)
                {
                    shippingCost = 0;
                    // basket.ShippingDescription = "ارسال داخل شهر چهار شنبه ها";
                    basket.ShippingDescription = "ارسال داخل شهر و هزینه ارسال رایگان است";
                }
            }
            else
            {
                if (request.ShippingOption != 3)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "ارور سرور",
                        Status = 200
                    };
                }

                if (basket.UserAddress.CityId != 445 || basket.UserAddress.CityId != 437)
                {
                    shippingCost = 0;
                    basket.ShippingDescription = "ارسال شهر خارج مشهد و پرداخت هزینه ارسال هنگام تحویل";
                    //return new ResultDto
                    //{
                    //    IsSuccess = false,
                    //    Message = "ارسال به شهر های دیگر با پرداخت هزینه ارسال هنگام تحویل",
                    //    Status = 200
                    //};
                }
            }

            totalPrice += shippingCost;

            basket.ShippingCost = shippingCost;
            _context.Baskets.Update(basket);
            _context.SaveChanges();

            // شماره یکتا برای orderId
            long orderId = DateTime.UtcNow.Ticks % 1000000000000L;

            // TODO: Mellat SOAP gateway removed. Replace CallPaymentGateway below
            // with the new payment gateway integration once it's decided.
            var (resCode, refId) = await CallPaymentGateway(totalPrice, orderId, currentUserId);

            if (resCode == "0")
            {
                var paymentData = new BasketPayment
                {
                    Price = (int)totalPrice,
                    UserId = currentUserId,
                    Status = "Wait",
                    VerificationStatus = "Wait",
                    Authority = refId,
                    BasketId = request.BasketId,
                    Ref = orderId.ToString()
                };
                _context.BasketPayments.Add(paymentData);
                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = refId,
                    Status = 200
                };
            }
            else
            {
                throw new Exception($"خطا در تراکنش درگاه پرداخت. کد خطا: {resCode}");
            }
        }


        public async Task<ResultDto<ResultBasketPaymentVerificationDto>> BasketPaymentVerification(
            MellatCallbackModel model)
        {
            // پیدا کردن رکورد پرداخت با Authority == refId و وضعیت Wait
            var data = _context.BasketPayments.Where(p => p.Authority == model.RefId && p.Status == "Wait")
                .FirstOrDefault();
            if (data == null)
            {
                return new ResultDto<ResultBasketPaymentVerificationDto>
                {
                    Data = new ResultBasketPaymentVerificationDto
                    {
                        BasketId = 0,
                        BasketStatus = "Payment not found",
                        ResultStatus = 0
                    }
                };
            }

            var basket = _context.Baskets.Include(b => b.Shipping).FirstOrDefault(p => p.Id == data.BasketId);
            if (basket == null)
            {
                return new ResultDto<ResultBasketPaymentVerificationDto>
                {
                    Data = new ResultBasketPaymentVerificationDto
                    {
                        BasketId = 0,
                        BasketStatus = "Basket not found",
                        ResultStatus = 0
                    }
                };
            }

            try
            {
                // بررسی موفق بودن پرداخت در درگاه بانک
                if (model.ResCode == "0")
                {
                    // TODO: Mellat SOAP verify/settle removed. Replace the placeholder
                    // calls below once the new payment gateway is integrated.
                    var verifyResult = await VerifyPaymentPlaceholder(model);

                    if (verifyResult == "0")
                    {
                        var settleResult = await SettlePaymentPlaceholder(model);

                        if (settleResult == "0")
                        {
                            data.VerificationStatus = "OK";
                            data.Status = "Paid";
                            _context.BasketPayments.Update(data);

                            basket.PaymentDate = DateTime.Now;
                            basket.BasketStatus = BasketStatus.Finalized;
                            _context.Baskets.Update(basket);
                            _context.SetUserId(data.UserId);

                            LogBasketStatusChange(basket.Id, BasketStatus.Finalized, data.UserId);

                            _context.SaveChanges();
                            return new ResultDto<ResultBasketPaymentVerificationDto>
                            {
                                Data = new ResultBasketPaymentVerificationDto
                                {
                                    BasketId = basket.Id,
                                    BasketStatus = "Paid",
                                    ResultStatus = 1
                                }
                            };
                        }
                        else
                        {
                            data.VerificationStatus = "Wait";
                            data.Status = "UnPaid";
                        }
                    }
                    else
                    {
                        data.VerificationStatus = "Wait";
                        data.Status = "UnPaid";
                    }
                }
                else
                {
                    data.VerificationStatus = "Wait";
                    data.Status = "UnPaid";
                }

                _context.BasketPayments.Update(data);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                data.Status = "Err";
                data.VerificationStatus = "Wait";
                _context.BasketPayments.Update(data);
                _context.SaveChanges();

                return new ResultDto<ResultBasketPaymentVerificationDto>
                {
                    Data = new ResultBasketPaymentVerificationDto
                    {
                        BasketId = basket.Id,
                        BasketStatus = "UnPaid",
                        ResultStatus = 2
                    }
                };
            }

            return new ResultDto<ResultBasketPaymentVerificationDto>
            {
                Data = new ResultBasketPaymentVerificationDto
                {
                    BasketId = basket.Id,
                    BasketStatus = data.Status,
                    ResultStatus = 1
                }
            };
        }

        // ==================== Payment gateway placeholders ====================
        // Mellat SOAP integration has been removed. Fill these in when the new
        // payment gateway is decided.

        private static Task<(string resCode, string refId)> CallPaymentGateway(long totalPrice, long orderId, long currentUserId)
        {
            // TODO: integrate new payment gateway here.
            return Task.FromResult(("0", Guid.NewGuid().ToString()));
        }

        private static Task<string> VerifyPaymentPlaceholder(MellatCallbackModel model)
        {
            // TODO: integrate new payment gateway verify call here.
            return Task.FromResult("0");
        }

        private static Task<string> SettlePaymentPlaceholder(MellatCallbackModel model)
        {
            // TODO: integrate new payment gateway settle call here.
            return Task.FromResult("0");
        }

        public ResultDto ChangeBasketStatus(ChangeBasketStatusDto request, long currentUserId)
        {
            try
            {
                var basket = _context.Baskets.FirstOrDefault(b => b.Id == request.BasketId);

                if (basket == null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 201,
                        Message = "سبد مورد نظر یافت نشد."
                    };
                }

                if (basket.BasketStatus == request.NewStatus)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "وضعیت سبد همین وضعیت است."
                    };
                }

                basket.BasketStatus = request.NewStatus;
                _context.Baskets.Update(basket);
                _context.SetUserId(currentUserId);

                LogBasketStatusChange(basket.Id, request.NewStatus, currentUserId);

                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات مورد نظر با موفقیت ثبت شد."
                };
            }
            catch (Exception ex)
            {
                return new ResultDto
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "خطایی رخ داد."
                };
            }
        }

        private static bool IsCityInShippingList(string? cityIds, object cityId)
        {
            if (string.IsNullOrWhiteSpace(cityIds))
                return false;

            var target = cityId?.ToString();
            if (string.IsNullOrEmpty(target))
                return false;

            return cityIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(idStr => idStr == target);
        }

        private void LogBasketStatusChange(long basketId, BasketStatus newStatus, long createdById)
        {
            _context.BasketStatusLogs.Add(new BasketStatusLog
            {
                BasketId = basketId,
                BasketStatus = newStatus,
                CreatedById = createdById
            });
        }
    }

    public class RequestBasketPeymentDto
    {
        public long BasketId { get; set; }
        public int ShippingCost { get; set; }
        public int? ShippingOption { get; set; }
    }

    public class ChangeBasketStatusDto
    {
        public BasketStatus NewStatus { get; set; }
        public long BasketId { get; set; }
    }

    public class ResultBasketPaymentVerificationDto
    {
        public long BasketId { get; set; }
        public string BasketStatus { get; set; }
        public int ResultStatus { get; set; }
    }

    public class MellatCallbackModel
    {
        public string RefId { get; set; }
        public string ResCode { get; set; }
        public string SaleOrderId { get; set; }
        public string? SaleReferenceId { get; set; }
        public string? CardHolderPan { get; set; }
        public string? CreditCardSaleResponseDetail { get; set; }
        public string? FinalAmount { get; set; }
    }
}