using Application.Interfaces.Contexts;
using Application.Services.Shippings.Queries;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Baskets;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Domain.Entities.Shippings;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using System.Linq.Expressions;
using static Application.Services.Baskets.Queries.BasketService;

namespace Application.Services.Baskets.Queries
{
    public interface IBasketService
    {
        ResultDto<ResultBasketInfoDto> UpdateBasket(RequestUpdateBasketDto request);
        ResultDto<ResultBasketInfoDto> UpdateBasketCount(RequestAddBasketQuantityDto request);
        ResultDto<int> BasketItemCounts(long userId, string publicToken);

        ResultDto<List<ResultCardBasketItemsDto>> CardBasketItems(long userId, string publicToken);
        ResultDto<ResultCardBasketTotalDataDto> CardBasketTotalData(long userId, string publicToken);
        ResultDto<List<ResultCardBasketItemsDto>> GetCurrentUserInvoice(long userId);
        ResultDto<ResultCardBasketTotalDataWithDateDto> CardBasketTotalDataWithDate(long userId, string publicToken);
        ResultDto<List<ResultCardBasketItemsDto>> GetBasketItemList(long userId, string publicToken);
        ResultDto<ResultCardBasketById> GetBasketById(long basketId);
        ResultDto<ResultProductStatusDto> GetProductStatus(long userId);
        ResultDto UpdateBasketAddress(long basketId, long newAddressId, long userId);
        ResultDto<long?> GetCurrentBasketAddressId(long userId);
        ResultDto<ResultBasketDetailsDto> GetBasketDetailsById(long basketId);
        ResultDto<List<UserBasketListDto>> GetAllBasketsByUserId(long userId, RequestUserBasketListDto request);
        ResultDto<string> GetBasketPaymentStatus(long basketId);
        ResultDto<BuyerDetailsDto> GetBuyerDetails(long basketId);
        ResultDto<DateTime?> GetBasketPaymentTime(long basketId);
        ResultDto<ResultBasketDetailsDto> GetCurrentBasket(long userId, string publicToken);

        ResultDto<ResultBasketValidationDto> ValidateBasket(long userId, string publicToken);
        ResultDto<List<ResultBasketListDto>> GetBasketList(RequestListDto request);

        ResultDto<ResultBasketInfoDto> ClearBasket(long userId, string publicToken);
        ResultDto<ResultBasketInfoDto> ClearBasketItem(long userId, string publicToken, long productId);
        ResultDto<List<ResultValidShippingOptionDto>> GetValidShippings(long userId, string publicToken);
        ResultDto ChooseShippingOption(long userId, string publicToken, RequestSetShippingDto request);
    }

    public class BasketService : IBasketService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;
        private readonly IShippingService _shippingService;

        public BasketService(IDataBaseContext context, IConfiguration configuration,
            IShippingService shippingService)
        {
            _context = context;
            _configuration = configuration;
            _shippingService = shippingService;
        }

        // -------------------- UpdateBasket --------------------
        public ResultDto<ResultBasketInfoDto> UpdateBasket(RequestUpdateBasketDto request)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == request.ProductId);

            if (product == null)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    IsSuccess = false,
                    Status = 403,
                    Message = "محصول قابل خرید نیست."
                };
            }

            var basketQuery = _context.Baskets
                .Include(b => b.BasketItems)
                .Where(b => b.BasketStatus == BasketStatus.Created);

            if (request.userId != 0)
                basketQuery = basketQuery.Where(b => b.UserId == request.userId);
            else
                basketQuery = basketQuery.Where(b => b.Token == request.publicToken);

            var basket = basketQuery
                .OrderByDescending(b => b.Id)
                .FirstOrDefault();

            if (request.Type == "increase")
            {
                if (product.Quantity <= 0)
                {
                    return new ResultDto<ResultBasketInfoDto>
                    {
                        IsSuccess = false,
                        Status = 403,
                        Message = "موجودی این محصول به پایان رسیده است."
                    };
                }

                if (basket == null)
                {
                    basket = new Basket
                    {
                        UserId = request.userId == 0 ? null : request.userId,
                        BasketStatus = BasketStatus.Created,
                        Token = request.publicToken,
                        Weight = 0,
                    };

                    _context.Baskets.Add(basket);
                    _context.SaveChanges();

                    basket = _context.Baskets
                        .Include(b => b.BasketItems)
                        .First(b => b.Id == basket.Id);
                }

                var basketItem = _context.BasketItems.FirstOrDefault(x =>
                    x.BasketId == basket.Id &&
                    x.ProductId == product.Id);

                if (basketItem != null)
                {
                    var newCount = basketItem.Count + 1;

                    if (product.MaxBuyLimit.HasValue && newCount > product.MaxBuyLimit.Value)
                    {
                        return new ResultDto<ResultBasketInfoDto>
                        {
                            Data = new ResultBasketInfoDto
                            {
                                BasketId = basket.Id,
                                CountCurrentProduct = basketItem.Count,
                                TotalCount = basket.BasketItems.Count
                            },
                            IsSuccess = false,
                            Status = 403,
                            Message = $"حداکثر تعداد قابل خرید برای این محصول {product.MaxBuyLimit.Value} عدد است."
                        };
                    }

                    if (newCount > product.Quantity)
                    {
                        return new ResultDto<ResultBasketInfoDto>
                        {
                            Data = new ResultBasketInfoDto
                            {
                                BasketId = basket.Id,
                                CountCurrentProduct = basketItem.Count,
                                TotalCount = basket.BasketItems.Count
                            },
                            IsSuccess = false,
                            Status = 403,
                            Message = "موجودی کافی نیست."
                        };
                    }

                    basketItem.Count = newCount;

                    var (offerDiscount, offerDiscountType) = ResolveOfferDiscount(product);
                    basketItem.Price = product.Price;
                    basketItem.Discount = offerDiscount;
                    basketItem.DiscountType = offerDiscountType;

                    _context.BasketItems.Update(basketItem);
                }
                else
                {
                    var initialCount = product.MinBuyLimit.HasValue && product.MinBuyLimit.Value > 1
                        ? product.MinBuyLimit.Value
                        : 1;

                    if (product.MaxBuyLimit.HasValue && initialCount > product.MaxBuyLimit.Value)
                    {
                        return new ResultDto<ResultBasketInfoDto>
                        {
                            IsSuccess = false,
                            Status = 403,
                            Message = "امکان افزودن این محصول با شرایط فعلی وجود ندارد."
                        };
                    }

                    if (initialCount > product.Quantity)
                    {
                        return new ResultDto<ResultBasketInfoDto>
                        {
                            IsSuccess = false,
                            Status = 403,
                            Message = "موجودی کافی نیست."
                        };
                    }

                    var itemPrice = product.Price;
                    var (itemDiscount, itemDiscountType) = ResolveOfferDiscount(product);

                    basketItem = new BasketItem
                    {
                        BasketId = basket.Id,
                        ProductId = product.Id,
                        Count = initialCount,
                        Price = itemPrice,
                        IsChangePrice = false,
                        Discount = itemDiscount,
                        DiscountType = itemDiscountType,
                    };

                    _context.BasketItems.Add(basketItem);
                }

                _context.Baskets.Update(basket);
                _context.SaveChanges();

                SyncStoredBasketWeight(basket);

                basket = _context.Baskets
                    .Include(b => b.BasketItems)
                    .First(b => b.Id == basket.Id);

                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        BasketId = basket.Id,
                        CountCurrentProduct = basketItem.Count,
                        TotalCount = basket.BasketItems.Count
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "محصول به سبد خرید اضافه گردید"
                };
            }

            if (request.Type == "decrease")
            {
                if (basket == null)
                {
                    return new ResultDto<ResultBasketInfoDto>
                    {
                        Data = new ResultBasketInfoDto
                        {
                            CountCurrentProduct = 0,
                            TotalCount = 0
                        },
                        IsSuccess = true,
                        Status = 200,
                        Message = "سبد خرید خالی است."
                    };
                }

                var basketItem = _context.BasketItems.FirstOrDefault(x =>
                    x.BasketId == basket.Id &&
                    x.ProductId == product.Id);

                if (basketItem == null)
                {
                    return new ResultDto<ResultBasketInfoDto>
                    {
                        Data = new ResultBasketInfoDto
                        {
                            BasketId = basket.Id,
                            CountCurrentProduct = 0,
                            TotalCount = basket.BasketItems.Count
                        },
                        IsSuccess = true,
                        Status = 200,
                        Message = "محصول در سبد خرید وجود ندارد."
                    };
                }

                var newCount = basketItem.Count - 1;

                var dropsUnderMin = product.MinBuyLimit.HasValue
                    && product.MinBuyLimit.Value > 1
                    && newCount < product.MinBuyLimit.Value
                    && newCount > 0;

                if (newCount <= 0 || dropsUnderMin)
                {
                    _context.BasketItems.Remove(basketItem);
                }
                else
                {
                    basketItem.Count = newCount;

                    var (offerDiscount, offerDiscountType) = ResolveOfferDiscount(product);
                    basketItem.Price = product.Price;
                    basketItem.Discount = offerDiscount;
                    basketItem.DiscountType = offerDiscountType;

                    _context.BasketItems.Update(basketItem);
                }

                _context.Baskets.Update(basket);
                _context.SaveChanges();

                SyncStoredBasketWeight(basket);

                basket = _context.Baskets
                    .Include(b => b.BasketItems)
                    .First(b => b.Id == basket.Id);

                var updatedItem = basket.BasketItems.FirstOrDefault(x =>
                    x.ProductId == product.Id);

                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        BasketId = basket.Id,
                        CountCurrentProduct = updatedItem?.Count ?? 0,
                        TotalCount = basket.BasketItems.Count
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "محصول از سبد خرید حذف گردید"
                };
            }

            return new ResultDto<ResultBasketInfoDto>
            {
                IsSuccess = false,
                Status = 400,
                Message = "عملیات نامعتبر"
            };
        }

        // -------------------- AddBasketQuantity --------------------
        public ResultDto<ResultBasketInfoDto> UpdateBasketCount(RequestAddBasketQuantityDto request)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == request.ProductId);

            if (product == null)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    IsSuccess = false,
                    Status = 403,
                    Message = "محصول قابل خرید نیست."
                };
            }

            var basketQuery = _context.Baskets
                .Include(b => b.BasketItems)
                .Where(b => b.BasketStatus == BasketStatus.Created);

            if (request.userId != 0)
                basketQuery = basketQuery.Where(b => b.UserId == request.userId);
            else
                basketQuery = basketQuery.Where(b => b.Token == request.publicToken);

            var basket = basketQuery
                .OrderByDescending(b => b.Id)
                .FirstOrDefault();

            if (!IsPurchasable(product))
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    IsSuccess = false,
                    Status = 403,
                    Message = "محصول قابل خرید نیست."
                };
            }

            if (product.Quantity <= 0)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    IsSuccess = false,
                    Status = 403,
                    Message = "موجودی این محصول به پایان رسیده است."
                };
            }

            if (basket == null)
            {
                basket = new Basket
                {
                    UserId = request.userId == 0 ? null : request.userId,
                    BasketStatus = BasketStatus.Created,
                    Token = request.publicToken,
                    Weight = 0,
                };

                _context.Baskets.Add(basket);
                _context.SaveChanges();

                basket = _context.Baskets
                    .Include(b => b.BasketItems)
                    .First(b => b.Id == basket.Id);
            }

            var basketItem = _context.BasketItems.FirstOrDefault(x =>
                x.BasketId == basket.Id &&
                x.ProductId == product.Id);

            var currentCount = basketItem?.Count ?? 0;
            var newCount = request.Count;

            if (newCount <= 0)
            {
                if (basketItem != null)
                {
                    _context.BasketItems.Remove(basketItem);

                    _context.Baskets.Update(basket);
                    _context.SaveChanges();

                    SyncStoredBasketWeight(basket);

                    basket = _context.Baskets
                        .Include(b => b.BasketItems)
                        .First(b => b.Id == basket.Id);
                }

                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        BasketId = basket.Id,
                        CountCurrentProduct = 0,
                        TotalCount = basket.BasketItems.Count
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "محصول از سبد خرید حذف گردید"
                };
            }

            if (product.MinBuyLimit.HasValue && newCount < product.MinBuyLimit.Value)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        BasketId = basket.Id,
                        CountCurrentProduct = currentCount,
                        TotalCount = basket.BasketItems.Count
                    },
                    IsSuccess = false,
                    Status = 403,
                    Message = $"حداقل تعداد قابل خرید برای این محصول {product.MinBuyLimit.Value} عدد است."
                };
            }

            if (product.MaxBuyLimit.HasValue && newCount > product.MaxBuyLimit.Value)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        BasketId = basket.Id,
                        CountCurrentProduct = currentCount,
                        TotalCount = basket.BasketItems.Count
                    },
                    IsSuccess = false,
                    Status = 403,
                    Message = $"حداکثر تعداد قابل خرید برای این محصول {product.MaxBuyLimit.Value} عدد است."
                };
            }

            if (newCount > product.Quantity)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        BasketId = basket.Id,
                        CountCurrentProduct = currentCount,
                        TotalCount = basket.BasketItems.Count
                    },
                    IsSuccess = false,
                    Status = 403,
                    Message = "موجودی کافی نیست."
                };
            }

            var itemPrice = product.Price;
            var (itemDiscount, itemDiscountType) = ResolveOfferDiscount(product);

            var isNewItem = basketItem == null;

            if (basketItem != null)
            {
                basketItem.Count = newCount;
                basketItem.Price = itemPrice;
                basketItem.Discount = itemDiscount;
                basketItem.DiscountType = itemDiscountType;

                _context.BasketItems.Update(basketItem);
            }
            else
            {
                basketItem = new BasketItem
                {
                    BasketId = basket.Id,
                    ProductId = product.Id,
                    Count = newCount,
                    Price = itemPrice,
                    IsChangePrice = false,
                    Discount = itemDiscount,
                    DiscountType = itemDiscountType,
                };

                _context.BasketItems.Add(basketItem);
            }

            _context.Baskets.Update(basket);
            _context.SaveChanges();

            SyncStoredBasketWeight(basket);

            basket = _context.Baskets
                .Include(b => b.BasketItems)
                .First(b => b.Id == basket.Id);

            return new ResultDto<ResultBasketInfoDto>
            {
                Data = new ResultBasketInfoDto
                {
                    BasketId = basket.Id,
                    CountCurrentProduct = basketItem.Count,
                    TotalCount = basket.BasketItems.Count
                },
                IsSuccess = true,
                Status = 200,
                Message = isNewItem ? "محصول به سبد خرید اضافه گردید" : "تعداد محصول در سبد خرید به‌روزرسانی شد"
            };
        }

        public ResultDto<int> BasketItemCounts(long userId, string publicToken)
        {
            var basket = GetActiveBasketQuery(userId, publicToken).FirstOrDefault();

            var itemCount = basket == null
                ? 0
                : _context.BasketItems
                    .Where(p => p.BasketId == basket.Id)
                    .Sum(p => (int?)p.Count) ?? 0;

            return new ResultDto<int>
            {
                Data = itemCount,
                IsSuccess = true,
                Status = 200,
            };
        }

        // -------------------- GetCurrentBasket --------------------
        public ResultDto<ResultBasketDetailsDto> GetCurrentBasket(long userId, string publicToken)
        {
            try
            {
                var basket = GetActiveBasketQuery(userId, publicToken)
                    .Include(b => b.User)
                    .Include(b => b.UserAddress)
                        .ThenInclude(a => a!.City)
                        .ThenInclude(c => c.State)
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .Include(b => b.Shipping)
                    .FirstOrDefault();

                return BuildBasketDetailsResult(basket);
            }
            catch (Exception ex)
            {
                return new ResultDto<ResultBasketDetailsDto>
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "خطایی در بازیابی سبد خرید رخ داد."
                };
            }
        }

        // -------------------- GetBasketDetailsById --------------------
        public ResultDto<ResultBasketDetailsDto> GetBasketDetailsById(long basketId)
        {
            try
            {
                var basket = _context.Baskets
                    .Include(b => b.User)
                    .Include(b => b.UserAddress)
                    .ThenInclude(a => a!.City)
                    .ThenInclude(c => c.State)
                    .Include(b => b.BasketItems)
                    .ThenInclude(bi => bi.Product)
                        .Include(b => b.Shipping)
                    .FirstOrDefault(b => b.Id == basketId);

                return BuildBasketDetailsResult(basket);
            }
            catch (Exception ex)
            {
                return new ResultDto<ResultBasketDetailsDto>
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "خطایی در بازیابی سبد خرید رخ داد."
                };
            }
        }

        /// <summary>
        /// Shared projection used by both GetCurrentBasket and GetBasketDetailsById,
        /// which previously duplicated this whole block.
        /// </summary>
        private ResultDto<ResultBasketDetailsDto> BuildBasketDetailsResult(Basket? basket)
        {
            if (basket == null)
            {
                return new ResultDto<ResultBasketDetailsDto>
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "سبد خرید یافت نشد."
                };
            }

            var basketItems = basket.BasketItems ?? new List<BasketItem>();

            BasketUserAddressDto address = null;
            if (basket.UserAddress != null)
            {
                address = new BasketUserAddressDto
                {
                    Id = basket.UserAddress.Id,
                    UserId = basket.UserAddress.UserId,
                    CityId = basket.UserAddress.CityId,
                    StateId = basket.UserAddress.City.StateId,
                    StateName = basket.UserAddress.City.State.Name,
                    CityName = basket.UserAddress.City.Name,
                    Address = basket.UserAddress.Address,
                    Plaque = basket.UserAddress.Plaque,
                    RecipientFullName = basket.UserAddress.RecipientFullName,
                    Mobile = basket.UserAddress.Mobile,
                    PostalCode = basket.UserAddress.PostalCode,
                    IsActive = basket.UserAddress.IsActive,
                };
            }

            var basketPayments = _context.BasketPayments.Where(bp => bp.BasketId == basket.Id).FirstOrDefault();

            long totalPrice = basketItems.Sum(item => CalcItemTotal(item));

            var productIds = basketItems
                .Where(i => i.Product != null)
                .Select(i => i.Product.Id)
                .Distinct()
                .ToList();

            var primaryImages = productIds.Any()
                ? _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Product
                             && i.ImageRole == ImageRoleEnum.Primary
                             && i.ReferenceId != null
                             && productIds.Contains(i.ReferenceId.Value))
                    .ToList()
                    .GroupBy(i => i.ReferenceId!.Value)
                    .ToDictionary(g => g.Key, g => g.First())
                : new Dictionary<long, Image>();

            var basketItemIds = basketItems.Select(i => i.Id).ToList();

            var statusLogsRaw = _context.BasketStatusLogs
                .Where(l => l.BasketId == basket.Id)
                .OrderBy(l => l.CreatedAt)
                .ToList();

            var changerIds = statusLogsRaw.Select(l => l.CreatedById).Distinct().ToList();
            var changerNames = changerIds.Any()
                ? _context.Users
                    .Where(u => changerIds.Contains(u.Id))
                    .ToList()
                    .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}")
                : new Dictionary<long, string>();

            var statusHistory = statusLogsRaw
                .Select(l => new BasketStatusLogDto
                {
                    Id = l.Id,
                    BasketStatus = ToBasketStatusDto(basket.BasketStatus),
                    CreatedById = (long)l.CreatedById,
                    CreatedByName = changerNames.TryGetValue((long)l.CreatedById, out var name) ? name : null,
                    CreatedAt = ((DateTime)l.CreatedAt).ToPersianDateString()
                })
                .ToList();

            var resultDto = new ResultBasketDetailsDto
            {
                BasketId = basket.Id,
                UserId = basket.UserId,
                Token = basket.Token,
                BasketStatus = ToBasketStatusDto(basket.BasketStatus),
                TotalPrice = totalPrice,
                Weight = ResolveBasketWeight(basket),
                Authority = basketPayments == null ? null : basketPayments.Authority,
                CreatedAt = ((DateTime)basket.CreatedAt).ToPersianDateString(),
                PaymentDate = basket.PaymentDate == null ? "" : ((DateTime)basket.PaymentDate).ToPersianDateString(),
                UserAddress = address,
                UserFullName = basket.User == null ? "کاربر لاگین نشده است" : $"{basket.User.FirstName} {basket.User.LastName}",
                UserMobile = basket.User == null ? "" : basket.User.Mobile,

                ShippingId = basket.ShippingId,
                ShippingDescription = basket.ShippingDescription,
                Shipping = basket.Shipping != null
                ? new ShippingDto
                {
                    Id = basket.Shipping.Id,
                    Name = basket.Shipping.Name,
                    Cost = basket.ShippingCost,
                    Description = basket.Shipping.Description
                }
                : null,


                Items = basketItems.Select(item => new BasketItemDetailsDto
                {
                    ItemId = item.Id,
                    ProductName = item.Product?.Name ?? "",
                    Count = item.Count,
                    Price = item.Price,
                    Discount = item.Discount,
                    IsGrounded = item.IsGrounded,
                    Image = item.Product != null && primaryImages.TryGetValue(item.Product.Id, out var img)
                        ? new ResultImageDto
                        {
                            Id = img.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Product/" + img.ImageUrl
                        }
                        : null,
                }).ToList(),

                StatusHistory = statusHistory
            };

            return new ResultDto<ResultBasketDetailsDto>
            {
                Data = resultDto,
                IsSuccess = true,
                Status = 200
            };
        }

        /// <summary>
        /// Shared basket-item projection reused by CardBasketItems, GetCurrentUserInvoice and
        /// GetBasketItemList, which previously each duplicated this Select().
        /// </summary>
        private Expression<Func<BasketItem, ResultCardBasketItemsDto>> BasketItemDtoProjection(bool includeCategoryName)
        {
            var staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

            if (includeCategoryName)
            {
                return p => new ResultCardBasketItemsDto
                {
                    Id = p.Id,
                    Count = p.Count,
                    Discount = p.Discount,
                    Name = p.Product.Name,
                    CategoryName = string.Join("، ", _context.ProductProductCategories
                        .Where(pc => pc.ProductId == p.Product.Id)
                        .Select(pc => pc.Category.Name)),
                    Price = p.Price,
                    ProductId = p.Product.Id,
                    Image = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Product
                                 && i.ImageRole == ImageRoleEnum.Primary
                                 && i.ReferenceId == p.Product.Id)
                        .Select(i => new ResultImageDto
                        {
                            Preview = staticPath + "/Resources/Product/" + i.ImageUrl,
                            Id = i.Id
                        }).ToList(),
                };
            }

            return p => new ResultCardBasketItemsDto
            {
                Id = p.Id,
                Count = p.Count,
                Discount = p.Discount,
                Name = p.Product.Name,
                Price = p.Price,
                ProductId = p.Product.Id,
                Image = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Product
                             && i.ImageRole == ImageRoleEnum.Primary
                             && i.ReferenceId == p.Product.Id)
                    .Select(i => new ResultImageDto
                    {
                        Preview = staticPath + "/Resources/Product/" + i.ImageUrl,
                        Id = i.Id
                    }).ToList(),
            };
        }

        // -------------------- CardBasketItems --------------------
        public ResultDto<List<ResultCardBasketItemsDto>> CardBasketItems(long userId, string publicToken)
        {
            try
            {
                var basketId = GetActiveBasketQuery(userId, publicToken)
                    .Select(b => (long?)b.Id)
                    .FirstOrDefault();

                var result = _context.BasketItems.Where(p => p.BasketId == basketId)
                    .Select(BasketItemDtoProjection(includeCategoryName: false))
                    .ToList();

                return new ResultDto<List<ResultCardBasketItemsDto>>
                {
                    Data = result,
                    IsSuccess = true,
                    Status = 200,
                };
            }
            catch (Exception e)
            {
                return new ResultDto<List<ResultCardBasketItemsDto>>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 200,
                };
            }
        }

        // -------------------- GetCurrentUserInvoice --------------------
        public ResultDto<List<ResultCardBasketItemsDto>> GetCurrentUserInvoice(long userId)
        {
            var basket = _context.Baskets.Where(p => p.BasketStatus == BasketStatus.Created && p.UserId == userId).FirstOrDefault();
            var result = new List<ResultCardBasketItemsDto>();
            if (basket != null)
            {
                if (basket.UserAddressId == null)
                {
                    var userAddress = _context.UserAddresses.FirstOrDefault(p => p.UserId == userId);
                    basket.UserAddressId = userAddress != null ? userAddress.Id : null;
                    _context.Baskets.Update(basket);
                    _context.SaveChanges();
                }

                result = _context.BasketItems.Where(p => p.BasketId == basket.Id)
                    .Select(BasketItemDtoProjection(includeCategoryName: true))
                    .ToList();
            }
            return new ResultDto<List<ResultCardBasketItemsDto>>
            {
                Data = result,
                IsSuccess = true,
                Status = 200,
            };
        }

        public ResultDto<ResultCardBasketTotalDataDto> CardBasketTotalData(long userId, string publicToken)
        {
            try
            {
                var result = GetActiveBasketQuery(userId, publicToken)
                    .Include(p => p.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .FirstOrDefault();

                if (result == null)
                {
                    return new ResultDto<ResultCardBasketTotalDataDto>
                    {
                        Data = new ResultCardBasketTotalDataDto
                        {
                            Count = 0
                        },
                        IsSuccess = false,
                        Status = 404,
                        Message = "سبد خرید یافت نشد."
                    };
                }

                long totalPrice = result.BasketItems.Sum(item => CalcItemTotal(item));

                return new ResultDto<ResultCardBasketTotalDataDto>
                {
                    Data = new ResultCardBasketTotalDataDto
                    {
                        BasketId = result.Id,
                        Tax = 0,
                        TotalPrice = totalPrice,
                        Weight = ResolveBasketWeight(result),
                        Count = result.BasketItems.Count()
                    },
                    IsSuccess = true,
                    Status = 200,
                };
            }
            catch (Exception e)
            {
                return new ResultDto<ResultCardBasketTotalDataDto>
                {
                    Data = new ResultCardBasketTotalDataDto
                    {
                        Count = 0
                    },
                    IsSuccess = false,
                    Status = 200,
                };
            }
        }

        public ResultDto<ResultCardBasketTotalDataWithDateDto> CardBasketTotalDataWithDate(long userId, string publicToken)
        {
            try
            {
                var result = GetActiveBasketQuery(userId, publicToken)
                    .Include(p => p.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .FirstOrDefault();

                if (result == null)
                {
                    return new ResultDto<ResultCardBasketTotalDataWithDateDto>
                    {
                        Data = new ResultCardBasketTotalDataWithDateDto
                        {
                            Count = 0
                        },
                        IsSuccess = false,
                        Status = 404,
                        Message = "سبد خرید یافت نشد."
                    };
                }

                long totalPrice = result.BasketItems.Sum(item => CalcItemTotal(item));

                return new ResultDto<ResultCardBasketTotalDataWithDateDto>
                {
                    Data = new ResultCardBasketTotalDataWithDateDto
                    {
                        BasketId = result.Id,
                        Tax = 0,
                        TotalPrice = totalPrice,
                        Weight = ResolveBasketWeight(result),
                        Count = result.BasketItems.Count(),
                        CreatedAt = result.CreatedAt
                    },
                    IsSuccess = true,
                    Status = 200,
                };
            }
            catch (Exception e)
            {
                throw;
            }
        }

        // -------------------- GetBasketById --------------------
        public ResultDto<ResultCardBasketById> GetBasketById(long basketId)
        {
            var basket = _context.Baskets
                .Include(b => b.BasketItems)
                .ThenInclude(bi => bi.Product)
                .Include(b => b.Shipping)
                .FirstOrDefault(b => b.Id == basketId);

            if (basket == null)
            {
                return new ResultDto<ResultCardBasketById>
                {
                    IsSuccess = false,
                    Message = "سبد خرید مورد نظر یافت نشد."
                };
            }

            var totalPrice = basket.BasketItems.Sum(item => CalcItemTotal(item));
            var tax = 0;
            var count = basket.BasketItems.Count;

            var resultBasket = new ResultCardBasketById
            {
                BasketId = basket.Id,
                AddressId = basket.UserAddressId,
                TotalPrice = totalPrice,
                Tax = tax,
                Weight = ResolveBasketWeight(basket),
                Count = count,
                BasketItems = basket.BasketItems.Select(item => new ResultCardBasketItemsDto
                {
                    Id = item.Id,
                    Name = item.Product.Name,
                    CategoryName = string.Join("، ", _context.ProductProductCategories
                        .Where(pc => pc.ProductId == item.Product.Id)
                        .Select(pc => pc.Category.Name)),
                    Price = item.Price,
                    Discount = item.Discount,
                    Count = item.Count,
                    ProductId = item.Product.Id,
                    Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Product
                                     && i.ImageRole == ImageRoleEnum.Primary
                                     && i.ReferenceId == item.Product.Id)
                            .Select(i => new ResultImageDto
                            {
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Product/" + i.ImageUrl,
                                Id = i.Id
                            }).ToList(),
                }).ToList(),
                BasketStatus = ToBasketStatusDto(basket.BasketStatus),
                Shipping = basket.Shipping != null
                    ? new ShippingDto
                    {
                        Id = basket.Shipping.Id,
                        Name = basket.Shipping.Name,
                        Cost = basket.ShippingCost,
                        Description = basket.Shipping.Description
                    }
                    : null
            };

            return new ResultDto<ResultCardBasketById>
            {
                IsSuccess = true,
                Data = resultBasket
            };
        }

        public ResultDto<ResultProductStatusDto> GetProductStatus(long userId)
        {
            try
            {
                var purchasedCount = _context.Baskets
                    .Where(p => p.BasketStatus == BasketStatus.Sent && p.UserId == userId)
                    .Include(p => p.BasketItems)
                    .SelectMany(p => p.BasketItems)
                    .Sum(i => i.Count);

                var pendingPaymentCount = _context.Baskets
                    .Where(p => p.BasketStatus == BasketStatus.Created && p.UserId == userId)
                    .Include(p => p.BasketItems)
                    .SelectMany(p => p.BasketItems)
                    .Sum(i => i.Count);

                var canceledCount = _context.Baskets
                    .Where(p => p.BasketStatus == BasketStatus.Cancelled && p.UserId == userId)
                    .Include(p => p.BasketItems)
                    .SelectMany(p => p.BasketItems)
                    .Sum(i => i.Count);

                var result = new ResultProductStatusDto
                {
                    PurchasedProducts = purchasedCount,
                    PendingPaymentProducts = pendingPaymentCount,
                    CanceledProducts = canceledCount
                };

                return new ResultDto<ResultProductStatusDto>
                {
                    Data = result,
                    IsSuccess = true,
                    Status = 200
                };
            }
            catch (Exception e)
            {
                return new ResultDto<ResultProductStatusDto>
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "خطایی رخ داده است."
                };
            }
        }

        public ResultDto UpdateBasketAddress(long basketId, long newAddressId, long userId)
        {
            var basket = _context.Baskets.FirstOrDefault(b => b.Id == basketId && b.UserId == userId);
            if (basket == null)
            {
                return new ResultDto
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "اطلاعات مورد نظر یافت نشد."
                };
            }

            var userAddress = _context.UserAddresses.FirstOrDefault(a => a.Id == newAddressId && a.UserId == userId);
            if (userAddress == null)
            {
                return new ResultDto
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "آدرس مورد نظر یافت نشد."
                };
            }

            basket.UserAddressId = newAddressId;

            _context.Baskets.Update(basket);
            _context.SaveChanges();

            return new ResultDto
            {
                IsSuccess = true,
                Status = 200,
                Message = "آدرس با موفقیت ثبت گردید."
            };
        }

        public ResultDto<long?> GetCurrentBasketAddressId(long userId)
        {
            try
            {
                var basket = _context.Baskets
                    .Where(b => b.BasketStatus == BasketStatus.Created && b.UserId == userId)
                    .FirstOrDefault();

                if (basket == null)
                {
                    return new ResultDto<long?>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 201,
                        Message = "سبد خرید یافت نشد."
                    };
                }

                return new ResultDto<long?>
                {
                    Data = basket.UserAddressId,
                    IsSuccess = true,
                    Status = 200,
                    Message = "آدرس سبد خرید با موفقیت بازیابی شد."
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<long?>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 201,
                    Message = "خطایی در بازیابی سبد خرید رخ داد."
                };
            }
        }

        // -------------------- GetBasketItemList --------------------
        public ResultDto<List<ResultCardBasketItemsDto>> GetBasketItemList(long userId, string publicToken)
        {
            try
            {
                var basket = GetActiveBasketQuery(userId, publicToken).FirstOrDefault();

                if (basket == null)
                {
                    return new ResultDto<List<ResultCardBasketItemsDto>>
                    {
                        Data = new List<ResultCardBasketItemsDto>(),
                        IsSuccess = true,
                        Status = 200,
                        Message = "سبد خرید یافت نشد."
                    };
                }

                var result = _context.BasketItems
                    .Where(p => p.BasketId == basket.Id)
                    .Select(BasketItemDtoProjection(includeCategoryName: true))
                    .ToList();

                return new ResultDto<List<ResultCardBasketItemsDto>>
                {
                    Data = result,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات با موفقیت ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<List<ResultCardBasketItemsDto>>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 500,
                    Message = "خطایی در بازیابی اقلام سبد خرید رخ داد."
                };
            }
        }

        // -------------------- GetAllBasketsByUserId --------------------
        public ResultDto<List<UserBasketListDto>> GetAllBasketsByUserId(long userId, RequestUserBasketListDto request)
        {
            try
            {
                var query = _context.Baskets
                    .Where(b => b.UserId == userId)
                    .AsQueryable();

                if (request.Status.HasValue)
                {
                    query = query.Where(b => b.BasketStatus == request.Status.Value);
                }

                //query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                //if (string.IsNullOrWhiteSpace(request.Sort))
                //{
                query = query.OrderByDescending(b => b.CreatedAt);
                //}

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var userBaskets = query
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .ToPaged(page, pageSize, out rowsCount)
                    .ToList();

                if (!userBaskets.Any())
                {
                    return new ResultDto<List<UserBasketListDto>>
                    {
                        Data = new List<UserBasketListDto>(),
                        IsSuccess = true,
                        Status = 200,
                        Message = "سبد خرید یافت نشد.",
                        Result = new ResultListDto
                        {
                            Current = page,
                            Count = rowsCount
                        }
                    };
                }

                // Only need primary images for up to the first 3 products shown per basket, for this page
                var previewProductIds = userBaskets
                    .SelectMany(b => (b.BasketItems ?? new List<BasketItem>()).Take(3))
                    .Select(bi => bi.ProductId)
                    .Distinct()
                    .ToList();

                var primaryImages = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Product
                             && i.ImageRole == ImageRoleEnum.Primary
                             && i.ReferenceId != null
                             && previewProductIds.Contains(i.ReferenceId.Value))
                    .ToList()
                    .GroupBy(i => i.ReferenceId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var resultDto = userBaskets.Select(basket =>
                {
                    var items = basket.BasketItems ?? new List<BasketItem>();
                    var totalPrice = items.Sum(item => CalcItemTotal(item));

                    return new UserBasketListDto
                    {
                        BasketId = basket.Id,
                        BasketStatus = ToBasketStatusDto(basket.BasketStatus),
                        TotalPrice = totalPrice,
                        Weight = ResolveBasketWeight(basket),
                        TotalItems = items.Sum(item => item.Count),
                        CreatedAt = basket.CreatedAt,
                        PaymentDate = basket.PaymentDate == null ? "" : ((DateTime)basket.PaymentDate).ToPersianDateString(),
                        Products = items
                            .Take(3)
                            .Select(bi => new BasketProductPreviewDto
                            {
                                ProductId = bi.ProductId,
                                Name = bi.Product?.Name,
                                Image = primaryImages.TryGetValue(bi.ProductId, out var img)
                                    ? new ResultImageDto
                                    {
                                        Id = img.Id,
                                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Product/" + img.ImageUrl
                                    }
                                    : null
                            })
                            .ToList()
                    };
                }).ToList();

                return new ResultDto<List<UserBasketListDto>>
                {
                    Data = resultDto,
                    IsSuccess = true,
                    Status = 200,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<List<UserBasketListDto>>
                {
                    IsSuccess = false,
                    Status = 500,
                    Message = "خطایی در بازیابی سبد خرید رخ داد."
                };
            }
        }

        // -------------------- GetBasketList --------------------
        public ResultDto<List<ResultBasketListDto>> GetBasketList(RequestListDto request)
        {
            try
            {
                var list = _context.Baskets
                    .Include(p => p.User)
                    .Include(p => p.UserAddress)
                        .ThenInclude(a => a.City)
                    .Include(p => p.BasketItems)
                    .AsQueryable();

                list = list.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    list = list.OrderByDescending(p => p.Id);
                }

                var rowsCount = 0;
                var basketList = list
                    .ToPaged(request.Page, request.PageSize, out rowsCount)
                    .Select(p => new ResultBasketListDto
                    {
                        Id = p.Id,
                        BasketId = p.Id,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        PaymentDate = p.PaymentDate,
                        UserInfo = p.User == null ? "" : $"{p.User.FirstName} {p.User.LastName}",
                        TotalPrice = p.BasketItems == null ? 0 : p.BasketItems.Sum(item =>
                            item.DiscountType == DiscountTypeFixedAmount
                                ? (item.Price - item.Discount) * item.Count
                                : (item.Price - (item.Price * item.Discount / 100)) * item.Count),
                        Weight = p.Weight ?? 0,
                        TotalItems = p.BasketItems == null ? 0 : p.BasketItems.Sum(i => i.Count),
                        BasketStatus = ToBasketStatusDto(p.BasketStatus),
                        City = p.UserAddress == null || p.UserAddress.City == null ? "" : p.UserAddress.City.Name
                    })
                    .ToList();

                // Mark every basket on this page as seen by admin (only touches rows not already flagged)
                var pageBasketIds = basketList.Select(b => b.BasketId).ToList();
                if (pageBasketIds.Count > 0)
                {
                    _context.Baskets
                        .Where(b => pageBasketIds.Contains(b.Id) && !b.SeenByAdmin)
                        .ExecuteUpdate(s => s.SetProperty(b => b.SeenByAdmin, true));
                }

                var createdIds = basketList
                    .Where(b => b.BasketStatus.Status == (int)BasketStatus.Created)
                    .Select(b => b.BasketId)
                    .ToList();

                var liveWeights = CalculateBasketWeightsFromDb(createdIds);

                foreach (var b in basketList.Where(b => b.BasketStatus.Status == (int)BasketStatus.Created))
                {
                    b.Weight = liveWeights.TryGetValue(b.BasketId, out var liveWeight) ? liveWeight : 0;
                }

                return new ResultDto<List<ResultBasketListDto>>
                {
                    Data = basketList,
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ارسال گردید",
                    Status = 200,
                    Result = new ResultListDto()
                    {
                        Current = request.Page,
                        Count = rowsCount
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<List<ResultBasketListDto>>
                {
                    IsSuccess = false,
                    Status = 500,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد"
                };
            }
        }

        public ResultDto<string> GetBasketPaymentStatus(long basketId)
        {
            try
            {
                var paymentStatus = _context.BasketPayments
                    .Where(bp => bp.BasketId == basketId)
                    .OrderByDescending(bp => bp.CreatedAt)
                    .Select(bp => bp.Status)
                    .FirstOrDefault();

                if (paymentStatus == null)
                {
                    return new ResultDto<string>
                    {
                        Data = "UnPaid",
                        IsSuccess = true,
                        Status = 200,
                        Message = "اطلاعات با موفقیت یافت شد."
                    };
                }

                return new ResultDto<string>
                {
                    Data = paymentStatus,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات با موفقیت یافت شد."
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<string>
                {
                    Data = "UnPaid",
                    IsSuccess = false,
                    Status = 500,
                    Message = "خطای غیر منتظره."
                };
            }
        }

        public ResultDto<BuyerDetailsDto> GetBuyerDetails(long basketId)
        {
            var basket = _context.Baskets
                .FirstOrDefault(b => b.Id == basketId);

            if (basket == null)
            {
                return new ResultDto<BuyerDetailsDto>
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "سبد خرید یافت نشد."
                };
            }

            var user = _context.Users
                .FirstOrDefault(u => u.Id == basket.UserId);

            if (user == null)
            {
                return new ResultDto<BuyerDetailsDto>
                {
                    IsSuccess = false,
                    Status = 201,
                    Message = "کاربر مورد نظر یافت نشد."
                };
            }

            var buyerDetails = new BuyerDetailsDto
            {
                FullName = $"{user.FirstName} {user.LastName}",
            };

            return new ResultDto<BuyerDetailsDto>
            {
                Data = buyerDetails,
                IsSuccess = true,
                Status = 200
            };
        }

        public ResultDto<DateTime?> GetBasketPaymentTime(long basketId)
        {
            var basketPayment = _context.BasketPayments
                .Where(bp => bp.BasketId == basketId)
                .OrderByDescending(bp => bp.CreatedAt)
                .FirstOrDefault();

            if (basketPayment == null)
            {
                return new ResultDto<DateTime?>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 201,
                    Message = "پرداختی برای این سبد خرید یافت نشد."
                };
            }

            return new ResultDto<DateTime?>
            {
                Data = basketPayment.CreatedAt,
                IsSuccess = true,
                Status = 200,
                Message = "زمان پرداخت با موفقیت بازیابی شد."
            };
        }

        // -------------------- ValidateBasketForCheckout --------------------
        public ResultDto<ResultBasketValidationDto> ValidateBasket(long userId, string publicToken)
        {
            try
            {
                var basket = GetActiveBasketQuery(userId, publicToken)
                    .Include(b => b.BasketItems)
                    .ThenInclude(bi => bi.Product)
                    .FirstOrDefault();

                if (basket == null)
                {
                    return new ResultDto<ResultBasketValidationDto>
                    {
                        IsSuccess = false,
                        Status = 404,
                        Message = "سبد خرید یافت نشد."
                    };
                }

                var problems = new List<BasketItemValidationDto>();
                var itemsChanged = false;

                foreach (var item in basket.BasketItems)
                {
                    var product = item.Product;

                    if (product == null || !IsPurchasable(product))
                    {
                        problems.Add(new BasketItemValidationDto
                        {
                            BasketItemId = item.Id,
                            ProductId = item.ProductId,
                            Reason = "محصول قابل خرید نیست."
                        });
                        continue;
                    }

                    if (item.Count > product.Quantity)
                    {
                        problems.Add(new BasketItemValidationDto
                        {
                            BasketItemId = item.Id,
                            ProductId = item.ProductId,
                            Reason = $"موجودی کافی نیست. موجودی فعلی: {product.Quantity}",
                            AvailableQuantity = product.Quantity
                        });
                        continue;
                    }

                    if (product.MaxBuyLimit.HasValue && item.Count > product.MaxBuyLimit.Value)
                    {
                        problems.Add(new BasketItemValidationDto
                        {
                            BasketItemId = item.Id,
                            ProductId = item.ProductId,
                            Reason = $"حداکثر تعداد قابل خرید {product.MaxBuyLimit.Value} عدد است."
                        });
                        continue;
                    }

                    if (product.MinBuyLimit.HasValue && product.MinBuyLimit.Value > 1 && item.Count < product.MinBuyLimit.Value)
                    {
                        problems.Add(new BasketItemValidationDto
                        {
                            BasketItemId = item.Id,
                            ProductId = item.ProductId,
                            Reason = $"حداقل تعداد قابل خرید {product.MinBuyLimit.Value} عدد است."
                        });
                        continue;
                    }

                    // Offer discount may have changed (expired, started, percentage edited,
                    // or removed) since this item was added. Basket stays valid either way —
                    // just re-snapshot the current discount onto the item.
                    var isOfferManaged = item.DiscountType == null || item.DiscountType == DiscountTypeProductOffer;

                    if (isOfferManaged)
                    {
                        var (currentOfferDiscount, currentOfferDiscountType) = ResolveOfferDiscount(product);

                        if (item.Discount != currentOfferDiscount || item.DiscountType != currentOfferDiscountType)
                        {
                            item.Discount = currentOfferDiscount;
                            item.DiscountType = currentOfferDiscountType;
                            _context.BasketItems.Update(item);
                            itemsChanged = true;
                        }
                    }
                }

                // Product weights may have changed since the basket was last touched.
                // Re-snapshot the stored weight so anything reading Basket.Weight after checkout is correct.
                var liveWeight = CalcBasketWeight(basket.BasketItems);
                var weightChanged = basket.Weight != liveWeight;

                if (weightChanged)
                {
                    basket.Weight = liveWeight;
                    _context.Baskets.Update(basket);
                }

                if (itemsChanged || weightChanged)
                {
                    _context.SaveChanges();
                }

                var isValid = problems.Count == 0;

                return new ResultDto<ResultBasketValidationDto>
                {
                    Data = new ResultBasketValidationDto
                    {
                        ItemProblems = problems
                    },
                    IsSuccess = isValid,
                    Status = isValid ? 200 : 409,
                    Message = isValid
                        ? "سبد خرید معتبر است."
                        : "برخی از اقلام سبد خرید نیاز به بررسی دارند."
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<ResultBasketValidationDto>
                {
                    IsSuccess = false,
                    Status = 500,
                    Message = "خطایی در بررسی سبد خرید رخ داد."
                };
            }
        }

        // -------------------- ClearBasket --------------------
        public ResultDto<ResultBasketInfoDto> ClearBasket(long userId, string publicToken)
        {
            var basket = GetActiveBasketQuery(userId, publicToken)
                .Include(b => b.BasketItems)
                .FirstOrDefault();

            if (basket == null || !basket.BasketItems.Any())
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        CountCurrentProduct = 0,
                        TotalCount = 0
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "سبد خرید خالی است."
                };
            }

            _context.BasketItems.RemoveRange(basket.BasketItems);

            basket.Weight = 0;

            _context.Baskets.Update(basket);
            _context.SaveChanges();

            return new ResultDto<ResultBasketInfoDto>
            {
                Data = new ResultBasketInfoDto
                {
                    BasketId = basket.Id,
                    CountCurrentProduct = 0,
                    TotalCount = 0
                },
                IsSuccess = true,
                Status = 200,
                Message = "سبد خرید با موفقیت خالی شد."
            };
        }

        // -------------------- ClearBasketItem --------------------
        public ResultDto<ResultBasketInfoDto> ClearBasketItem(long userId, string publicToken, long productId)
        {
            var basket = GetActiveBasketQuery(userId, publicToken)
                .Include(b => b.BasketItems)
                .FirstOrDefault();

            if (basket == null)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        CountCurrentProduct = 0,
                        TotalCount = 0
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "سبد خرید یافت نشد."
                };
            }

            var basketItem = _context.BasketItems.FirstOrDefault(x =>
                x.BasketId == basket.Id &&
                x.ProductId == productId);

            if (basketItem == null)
            {
                return new ResultDto<ResultBasketInfoDto>
                {
                    Data = new ResultBasketInfoDto
                    {
                        BasketId = basket.Id,
                        CountCurrentProduct = 0,
                        TotalCount = basket.BasketItems.Count
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "محصول در سبد خرید وجود ندارد."
                };
            }

            _context.BasketItems.Remove(basketItem);

            _context.Baskets.Update(basket);
            _context.SaveChanges();

            SyncStoredBasketWeight(basket);

            var remainingCount = _context.BasketItems.Count(x => x.BasketId == basket.Id);

            return new ResultDto<ResultBasketInfoDto>
            {
                Data = new ResultBasketInfoDto
                {
                    BasketId = basket.Id,
                    CountCurrentProduct = 0,
                    TotalCount = remainingCount
                },
                IsSuccess = true,
                Status = 200,
                Message = "محصول از سبد خرید حذف گردید."
            };
        }

        // -------------------- ChooseShippingOption --------------------
        public ResultDto ChooseShippingOption(long userId, string publicToken, RequestSetShippingDto request)
        {
            try
            {
                var basket = GetActiveBasketQuery(userId, publicToken)
                    .Include(b => b.UserAddress)
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .FirstOrDefault();

                // Always re-validate on the server: weight/address may have changed since the options were shown
                var options = GetShippingOptionsForBasket(basket);

                if (!options.IsSuccess || options.Data == null)
                {
                    return new ResultDto { IsSuccess = false, Status = options.Status, Message = options.Message };
                }

                var chosen = options.Data.FirstOrDefault(o => o.ShippingId == request.Id);

                if (chosen == null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "این روش ارسال برای شهر و وزن این سبد خرید معتبر نیست."
                    };
                }

                basket!.ShippingId = chosen.ShippingId;
                //basket.ShippingDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
                basket.ShippingCost = chosen.Price;
                basket.Weight = CalcBasketWeight(basket.BasketItems);

                // basket is already tracked, so no Update() call
                _context.SaveChanges();

                return new ResultDto { IsSuccess = true, Status = 200, Message = "روش ارسال با موفقیت ثبت شد." };
            }
            catch (Exception ex)
            {
                return new ResultDto { IsSuccess = false, Status = 500, Message = "خطایی در ثبت روش ارسال رخ داد." };
            }
        }

        // -------------------- GetValidShippingOptions --------------------
        public ResultDto<List<ResultValidShippingOptionDto>> GetValidShippings(long userId, string publicToken)
        {
            var basket = GetActiveBasketQuery(userId, publicToken)
                .AsNoTracking()
                .Include(b => b.UserAddress)
                .Include(b => b.BasketItems)
                    .ThenInclude(bi => bi.Product)
                .FirstOrDefault();

            return GetShippingOptionsForBasket(basket);
        }

        // ==================== Helpers ====================

        private const int DiscountTypePercentage = 1;
        private const int DiscountTypeFixedAmount = 2;

        private static bool IsPurchasable(Product product) => product.Price > 0;

        private static long CalcItemTotal(BasketItem item) =>
            item.DiscountType == DiscountTypeFixedAmount
                ? (item.Price - item.Discount) * item.Count
                : (item.Price - (item.Price * item.Discount / 100)) * item.Count;

        private static int CalcBasketWeight(IEnumerable<BasketItem>? items)
        {
            if (items == null)
                return 0;

            decimal totalGrams = items.Sum(i => (decimal)(i.Product?.Weight ?? 0) * i.Count);

            return CeilWeight(totalGrams / 1000m);
        }

        /// <summary>Rounds the total weight up to a whole number: 1.0 -> 1, 1.1 -> 2.</summary>
        private static int CeilWeight(decimal weight) =>
            weight <= 0 ? 0 : (int)Math.Ceiling(weight);

        private static int ResolveBasketWeight(Basket basket) =>
            basket.BasketStatus == BasketStatus.Created
                ? CalcBasketWeight(basket.BasketItems)
                : basket.Weight ?? 0;

        private Dictionary<long, int> CalculateBasketWeightsFromDb(List<long> basketIds)
        {
            if (!basketIds.Any())
                return new Dictionary<long, int>();

            return _context.BasketItems
                .Where(i => basketIds.Contains(i.BasketId))
                .Select(i => new { i.BasketId, i.Count, Weight = i.Product.Weight })
                .ToList()
                .GroupBy(r => r.BasketId)
                .ToDictionary(
                    g => g.Key,
                    g => CeilWeight(g.Sum(r => (decimal)(r.Weight ?? 0) * r.Count) / 1000m));
        }

        private int CalculateBasketWeightFromDb(long basketId)
        {
            var rows = _context.BasketItems
                .Where(i => i.BasketId == basketId)
                .Select(i => new { i.Count, Weight = i.Product.Weight })
                .ToList();

            decimal totalGrams = rows.Sum(r => (decimal)(r.Weight ?? 0) * r.Count);

            return CeilWeight(totalGrams / 1000m);
        }

        private void SyncStoredBasketWeight(Basket basket)
        {
            var weight = CalculateBasketWeightFromDb(basket.Id);

            if (basket.Weight == weight)
                return;

            basket.Weight = weight;
            _context.Baskets.Update(basket);
            _context.SaveChanges();
        }

        private IQueryable<Basket> GetActiveBasketQuery(long userId, string publicToken)
        {
            var query = _context.Baskets.Where(b => b.BasketStatus == BasketStatus.Created);

            return userId != 0
                ? query.Where(b => b.UserId == userId)
                : query.Where(b => b.Token == publicToken);
        }

        private const int DiscountTypeProductOffer = 1;

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

        private static string GetBasketStatusText(BasketStatus status) => status switch
        {
            BasketStatus.Created => "در حال ثبت",
            BasketStatus.Cancelled => "لغو شده",
            BasketStatus.Finalized => "نهایی شده",
            BasketStatus.Rejected => "رد شده",
            BasketStatus.Sent => "ارسال شده",
            _ => status.ToString()
        };

        private static BasketStatusDto ToBasketStatusDto(BasketStatus? status)
        {
            var value = status ?? BasketStatus.Created;
            return new BasketStatusDto
            {
                Status = (int)value,
                Text = GetBasketStatusText(value)
            };
        }

        private static BasketStatusDto ToBasketStatusDto(BasketStatus status) =>
            ToBasketStatusDto((BasketStatus?)status);

        private ResultDto<List<ResultValidShippingOptionDto>> GetShippingOptionsForBasket(Basket? basket)
        {
            if (basket == null)
                return new ResultDto<List<ResultValidShippingOptionDto>>
                { IsSuccess = false, Status = 404, Message = "سبد خرید یافت نشد." };

            if (!basket.BasketItems.Any())
                return new ResultDto<List<ResultValidShippingOptionDto>>
                { IsSuccess = false, Status = 200, Message = "سبد خرید خالی است." };

            var price = basket.BasketItems == null ? 0 : basket.BasketItems.Sum(item =>
                            item.DiscountType == DiscountTypeFixedAmount
                                ? (item.Price - item.Discount) * item.Count
                                : (item.Price - (item.Price * item.Discount / 100)) * item.Count);

            var weight = CalcBasketWeight(basket.BasketItems);
            long? cityId = basket.UserAddress?.CityId;

            return _shippingService.GetValidShippingOptions(weight, cityId, basket.ShippingId ?? 0, price);
        }


        // ==================== DTOs ====================

        public class RequestUpdateBasketDto
        {
            public long ProductId { get; set; }
            public string Type { get; set; }
            public long? userId { get; set; }
            public string? publicToken { get; set; }
        }

        public class ResultBasketInfoDto
        {
            public long BasketId { get; set; }
            public int CountCurrentProduct { get; set; }
            public int TotalCount { get; set; }
        }

        public class ResultCardBasketItemsDto
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string CategoryName { get; set; }
            public long? AddressId { get; set; }
            public List<ResultImageDto> Image { get; set; }
            public long Price { get; set; }
            public long Discount { get; set; }
            public long ProductId { get; set; }
            public int Count { get; set; }
        }
        public class ResultCardBasketTotalDataDto
        {
            public long BasketId { get; set; }
            public long TotalPrice { get; set; }
            public long SendPrice { get; set; }
            public long Tax { get; set; }
            public int Weight { get; set; }
            public long Count { get; set; }
        }

        public class ResultCardBasketTotalDataWithDateDto
        {
            public long BasketId { get; set; }
            public long TotalPrice { get; set; }
            public long SendPrice { get; set; }
            public long Tax { get; set; }
            public int Weight { get; set; }
            public long Count { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class ResultCardBasketById
        {
            public long BasketId { get; set; }
            public long? AddressId { get; set; }
            public long TotalPrice { get; set; }
            public long SendPrice { get; set; }
            public long Tax { get; set; }
            public int Weight { get; set; }
            public long Count { get; set; }
            public List<ResultCardBasketItemsDto> BasketItems { get; set; }
            public BasketStatusDto BasketStatus { get; set; }
            public ShippingDto Shipping { get; set; }
        }

        public class ShippingDto
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public long Cost { get; set; }
            public string? Description { get; set; }
        }

        public class ResultProductStatusDto
        {
            public int PurchasedProducts { get; set; }
            public int PendingPaymentProducts { get; set; }
            public int CanceledProducts { get; set; }
        }

        public class BasketUserAddressDto
        {
            public long Id { get; set; }
            public int UserId { get; set; }
            public long CityId { get; set; }
            public string CityName { get; set; }
            public long StateId { get; set; }
            public string StateName { get; set; }
            public string Address { get; set; }
            public string Plaque { get; set; }
            public string RecipientFullName { get; set; }
            public string Mobile { get; set; }
            public string PostalCode { get; set; }
            public bool IsActive { get; set; }
        }

        public class ResultBasketDetailsDto
        {
            // Identity
            public long BasketId { get; set; }
            public long? UserId { get; set; }
            public string? Token { get; set; }
            public BasketStatusDto BasketStatus { get; set; }

            // User
            public string UserFullName { get; set; }
            public string UserMobile { get; set; }
            public BasketUserAddressDto UserAddress { get; set; }

            // Pricing
            public long TotalPrice { get; set; }
            public long? DiscountScorePrice { get; set; }
            public long SendPrice { get; set; }
            public int Weight { get; set; }

            // Payment / dates
            public string? Authority { get; set; }
            public string CreatedAt { get; set; }
            public string PaymentDate { get; set; }

            // Shippings
            public long? ShippingId { get; set; }
            public string? ShippingDescription { get; set; }
            public ShippingDto? Shipping { get; set; }

            // Misc
            public bool HasSurveyParticipated { get; set; }
            public List<BasketStatusLogDto> StatusHistory { get; set; } = new List<BasketStatusLogDto>();

            // Items (last)
            public List<BasketItemDetailsDto> Items { get; set; }
        }

        public class BasketItemDetailsDto
        {
            public long ItemId { get; set; }
            public string ProductName { get; set; }
            public int Count { get; set; }
            public long Price { get; set; }
            public long Discount { get; set; }
            public ResultImageDto Image { get; set; }
            public bool IsGrounded { get; set; }
            public List<BasketItemUserImageDto> UserImages { get; set; } = new();
            public bool HasUserImage { get; set; }
        }

        public class BasketItemUserImageDto
        {
            public long Id { get; set; }
            public string ImageUrl { get; set; }
            public string? ImageDescription { get; set; }
            public bool IsActive { get; set; }
        }

        public class BasketStatusLogDto
        {
            public long Id { get; set; }
            public BasketStatusDto BasketStatus { get; set; }
            public long CreatedById { get; set; }
            public string? CreatedByName { get; set; }
            public string CreatedAt { get; set; }
        }

        public class UserBasketListDto
        {
            public long BasketId { get; set; }
            public BasketStatusDto BasketStatus { get; set; }
            public long TotalPrice { get; set; }
            public int Weight { get; set; }
            public int TotalItems { get; set; }
            public DateTime CreatedAt { get; set; }
            public string PaymentDate { get; set; }
            public bool HasSurveyParticipated { get; set; }
            public List<BasketProductPreviewDto> Products { get; set; } = new List<BasketProductPreviewDto>();
        }

        public class BasketProductPreviewDto
        {
            public long ProductId { get; set; }
            public string Name { get; set; }
            public ResultImageDto Image { get; set; }
        }

        public class BuyerDetailsDto
        {
            public string FullName { get; set; }
            public string MeliCode { get; set; }
        }

        public class ResultBasketListDto
        {
            public long Id { get; set; }
            public long BasketId { get; set; }
            public BasketStatusDto BasketStatus { get; set; }
            public long? TotalPrice { get; set; }
            public int Weight { get; set; }
            public int? TotalItems { get; set; }
            public string City { get; set; }
            public string UserInfo { get; set; }
            public bool AdminSeen { get; set; }
            public DateTime? PaymentDate { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class BasketItemValidationDto
        {
            public long BasketItemId { get; set; }
            public long ProductId { get; set; }
            public string Reason { get; set; }
            public int? AvailableQuantity { get; set; }
        }
        public class ResultBasketValidationDto
        {
            public List<BasketItemValidationDto> ItemProblems { get; set; } = new();
        }

        public class RequestBasketListDto
        {
            public long UserId { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; } = 20;
            public BasketStatus? Status { get; set; }
        }
        public class RequestUserBasketListDto
        {
            public int Page { get; set; }
            public int PageSize { get; set; } = 20;
            public BasketStatus? Status { get; set; }
        }

        public class BasketStatusDto
        {
            public int Status { get; set; }
            public string Text { get; set; }
        }
        public class RequestSetShippingDto
        {
            public long Id { get; set; }
        }
        public class RequestAddBasketQuantityDto
        {
            public long userId { get; set; }
            public string publicToken { get; set; }
            public long ProductId { get; set; }
            public int Count { get; set; }
        }
    }
}