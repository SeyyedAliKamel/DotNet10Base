using Application.Interfaces.Contexts;
using Application.Services.Products.Commands;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;

namespace Application.Services.Products.Queries
{
    // ==================== Interface ====================
    public interface IProductService
    {
        ResultDto<List<ProductDto>> GetProductList(RequestListDto request);
        ResultDto<ProductItemDto> GetProductItem(long id);
        ResultDto<List<ProductDto>> GetWishlist(ProductWishlistRequestDto request);

        // -------- Frontend-facing (active only, no WholePrice) --------
        ResultDto<List<ProductFrontEndDto>> GetProductListFrontEnd(ProductListRequestDto request);
        ResultDto<ProductItemFrontendDto> GetProductItemFrontEnd(long productId, long userId, string publicToken);

        // -------- Branch-facing (active only, exposes both Price and WholePrice) --------
        ResultDto<List<ProductBranchDto>> GetProductListBranch(ProductListRequestDto request, long userId, string publicToken, bool splitGrounded = false);
        ResultDto<ProductItemBranchDto> GetProductItemBranch(long productId, long userId, string publicToken);

        ResultDto<List<ProductDto>> GetProductListByCategory(ProductListByCategoryRequestDto request);
    }

    // ==================== Implementation ====================
    public class ProductService : IProductService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public ProductService(IConfiguration configuration, IDataBaseContext context)
        {
            _context = context;
            _configuration = configuration;
        }

        // -------------------- GetList (everything, admin) --------------------
        public ResultDto<List<ProductDto>> GetProductList(RequestListDto request)
        {
            try
            {
                string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

                var query = _context.Products
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
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Description = p.Description,
                        MetaTitle = p.MetaTitle,
                        MetaDescription = p.MetaDescription,
                        Attribute = p.Attribute,
                        Price = p.Price,
                        WholePrice = p.WholePrice,
                        Quantity = p.Quantity,
                        Rating = p.Rating,
                        IsGrounded = p.IsGrounded,
                        IsActive = p.IsActive,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Title,
                        MainImageUrl = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Product
                                     && i.ImageRole == ImageRoleEnum.Primary
                                     && i.ReferenceId == p.Id)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault() == null ? null :
                                staticPath + "/Resources/Product/" + _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.Product
                                             && i.ImageRole == ImageRoleEnum.Primary
                                             && i.ReferenceId == p.Id)
                                    .Select(i => i.ImageUrl)
                                    .FirstOrDefault(),
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        Offers = _context.ProductOffers
                            .Where(o => o.ProductId == (int)p.Id && o.OfferStatus == OfferStatus.Active)
                            .Select(o => new ProductOfferDto
                            {
                                Id = o.Id,
                                ProductId = o.ProductId,
                                OfferPercentage = o.OfferPercentage,
                                OfferStatus = o.OfferStatus,
                                OfferEnd = o.OfferEnd
                            })
                            .ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ProductDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست محصولات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd (IsActive only, no WholePrice) --------------------
        public ResultDto<List<ProductFrontEndDto>> GetProductListFrontEnd(ProductListRequestDto request)
        {
            try
            {
                string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

                var query = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderBy(s => s.Order);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new ProductFrontEndDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Description = p.Description,
                        MetaTitle = p.MetaTitle,
                        MetaDescription = p.MetaDescription,
                        Attribute = p.Attribute,
                        Price = p.Price,
                        Quantity = p.Quantity,
                        Rating = p.Rating,
                        IsGrounded = p.IsGrounded,
                        IsActive = p.IsActive,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Title,
                        MainImageUrl = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Product
                                     && i.ImageRole == ImageRoleEnum.Primary
                                     && i.ReferenceId == p.Id)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault() == null ? null :
                                staticPath + "/Resources/Product/" + _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.Product
                                             && i.ImageRole == ImageRoleEnum.Primary
                                             && i.ReferenceId == p.Id)
                                    .Select(i => i.ImageUrl)
                                    .FirstOrDefault(),
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        Offers = _context.ProductOffers
                            .Where(o => o.ProductId == (int)p.Id && o.OfferStatus == OfferStatus.Active)
                            .Select(o => new ProductOfferDto
                            {
                                Id = o.Id,
                                ProductId = o.ProductId,
                                OfferPercentage = o.OfferPercentage,
                                OfferStatus = o.OfferStatus,
                                OfferEnd = o.OfferEnd
                            })
                            .ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ProductFrontEndDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست محصولات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductFrontEndDto>>(ex);
            }
        }

        // -------------------- GetListBranch (IsActive only, Price + WholePrice) --------------------
        public ResultDto<List<ProductBranchDto>> GetProductListBranch(ProductListRequestDto request, long userId, string publicToken, bool splitGrounded = false)
        {
            try
            {
                string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

                var query = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .Where(p => p.WholePrice > 0)
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
                    .Select(p => new ProductBranchDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Description = p.Description,
                        MetaTitle = p.MetaTitle,
                        MetaDescription = p.MetaDescription,
                        Attribute = p.Attribute,
                        Price = p.Price,
                        WholePrice = p.WholePrice,
                        Quantity = p.Quantity,
                        Rating = p.Rating,
                        IsGrounded = p.IsGrounded,
                        IsActive = p.IsActive,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Title,
                        MainImageUrl = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Product
                                     && i.ImageRole == ImageRoleEnum.Primary
                                     && i.ReferenceId == p.Id)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault() == null ? null :
                                staticPath + "/Resources/Product/" + _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.Product
                                             && i.ImageRole == ImageRoleEnum.Primary
                                             && i.ReferenceId == p.Id)
                                    .Select(i => i.ImageUrl)
                                    .FirstOrDefault(),
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        Offers = _context.ProductOffers
                            .Where(o => o.ProductId == (int)p.Id && o.OfferStatus == OfferStatus.Active)
                            .Select(o => new ProductOfferDto
                            {
                                Id = o.Id,
                                ProductId = o.ProductId,
                                OfferPercentage = o.OfferPercentage,
                                OfferStatus = o.OfferStatus,
                                OfferEnd = o.OfferEnd
                            })
                            .ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                // Groundable products get a second row for the ground variant, right after the plain one.
                if (splitGrounded)
                {
                    list = list
                        .SelectMany(p => p.IsGrounded
                            ? new[] { p, p.AsGroundedItem() }
                            : new[] { p })
                        .ToList();
                }

                FillBasketCounts(list, userId, publicToken, isWholePrice: true, splitGrounded);

                return new ResultDto<List<ProductBranchDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست محصولات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductBranchDto>>(ex);
            }
        }

        // Fills how many of each product are in the caller's current basket.
        // A product can sit in the basket as two separate lines (ground / not ground).
        // splitGrounded: each row counts only the basket line matching its IsGroundedItem; otherwise both lines are summed.
        private void FillBasketCounts(List<ProductBranchDto> products, long userId, string publicToken, bool isWholePrice, bool splitGrounded)
        {
            if (products.Count == 0 || (userId == 0 && string.IsNullOrEmpty(publicToken)))
                return;

            var basketQuery = _context.Baskets.Where(b => b.BasketStatus == BasketStatus.Created);

            basketQuery = userId != 0
                ? basketQuery.Where(b => b.UserId == userId && b.IsWholePrice == isWholePrice)
                : basketQuery.Where(b => b.Token == publicToken);

            var basketId = basketQuery
                .OrderByDescending(b => b.Id)
                .Select(b => (long?)b.Id)
                .FirstOrDefault();

            if (basketId == null)
                return;

            var basketItems = _context.BasketItems
                .AsNoTracking()
                .Where(bi => bi.BasketId == basketId)
                .Select(bi => new { bi.ProductId, bi.IsGrounded, bi.Count })
                .ToList();

            foreach (var product in products)
            {
                product.BasketCount = basketItems
                    .Where(bi => bi.ProductId == product.Id
                              && (!splitGrounded || bi.IsGrounded == product.IsGroundedItem))
                    .Sum(bi => bi.Count);
            }
        }

        // -------------------- GetItem (everything, admin) --------------------
        public ResultDto<ProductItemDto> GetProductItem(long id)
        {
            try
            {
                string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

                var product = _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new ProductItemDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Description = p.Description,
                        LongDescription = p.LongDescription,
                        MetaTitle = p.MetaTitle,
                        MetaDescription = p.MetaDescription,
                        Attribute = p.Attribute,
                        Price = p.Price,
                        WholePrice = p.WholePrice,
                        Quantity = p.Quantity,
                        MinBuyLimit = p.MinBuyLimit,
                        MaxBuyLimit = p.MaxBuyLimit,
                        Rating = p.Rating,
                        IsGrounded = p.IsGrounded,
                        IsActive = p.IsActive,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Title,
                        Hashtags = p.Hashtags
                            .Select(ph => new HashtagItemDto
                            {
                                Id = ph.Hashtag.Id,
                                Title = ph.Hashtag.Title
                            })
                            .ToList(),
                        Images = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Product && i.ReferenceId == p.Id)
                            .OrderByDescending(i => i.ImageRole == ImageRoleEnum.Primary)
                            .ThenBy(i => i.Id)
                            .Select(i => new ProductImageDto
                            {
                                Id = i.Id,
                                ImageUrl = i.ImageUrl == null ? null :
                                    staticPath + "/Resources/Product/" + i.ImageUrl,
                                ImageDescription = i.ImageDescription,
                                ImageTypeEnum = i.ImageRole
                            })
                            .ToList(),
                        UserImages = _context.BasketItemImages
                            .Where(bi => bi.ProductId == p.Id && bi.Image.IsActive)
                            .Select(bi => new ProductUserImageDto
                            {
                                Id = bi.Image.Id,
                                ImageUrl = staticPath + "/Resources/BasketItem/" + bi.Image.ImageUrl,
                                ImageDescription = bi.Image.ImageDescription,
                                UserId = bi.UserId
                            })
                            .ToList(),
                        Categories = _context.ProductProductCategories
                            .Where(pc => pc.ProductId == p.Id)
                            .Select(pc => new ProductCategoryDto
                            {
                                Id = pc.CategoryId,
                                Name = pc.Category.Name,
                                Slug = _context.Slugs
                                    .Where(s => s.ReferenceId == pc.Id && s.Type == SlugTypeEnum.ProductCategory)
                                    .Select(s => s.Title)
                                    .FirstOrDefault(),
                            })
                            .ToList(),
                        Relations = _context.ProductRelations
                            .Where(r => r.ProductId == p.Id)
                            .Select(r => new ProductRelationItemDto
                            {
                                Id = r.RelatedProductId,
                                Name = r.RelatedProduct.Name,
                            })
                            .ToList(),
                        Offers = _context.ProductOffers
                            .Where(o => o.ProductId == (int)p.Id)
                            .Select(o => new ProductOfferItemDto
                            {
                                Id = o.Id,
                                ProductId = o.ProductId,
                                OfferDescription = o.OfferDescription,
                                OfferPercentage = o.OfferPercentage,
                                OfferStatus = o.OfferStatus,
                                OfferStart = o.OfferStart,
                                OfferEnd = o.OfferEnd,
                                OfferLimit = o.OfferLimit
                            })
                            .ToList(),
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (product == null)
                {
                    return new ResultDto<ProductItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "محصول یافت نشد"
                    };
                }

                return new ResultDto<ProductItemDto>
                {
                    Data = product,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات محصول ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ProductItemDto>(ex);
            }
        }

        // --------------------- GetItemFrontEnd (IsActive only, no WholePrice) ------------------
        public ResultDto<ProductItemFrontendDto> GetProductItemFrontEnd(long productId, long userId, string publicToken)
        {
            var basket = _context.Baskets.AsQueryable();
            var wishlist = _context.ProductWishes.AsQueryable();
            if (userId != 0)
            {
                basket = basket.Where(p => p.BasketStatus == BasketStatus.Created && p.UserId == userId);
            }
            else
            {
                basket = basket.Where(p => p.Token == publicToken);
            }

            var item = _context.Products.Where(p => p.Id == productId && p.IsActive).Select(p => new ProductItemFrontendDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                LongDescription = p.LongDescription,
                MetaTitle = p.MetaTitle,
                MetaDescription = p.MetaDescription,
                Attribute = p.Attribute,
                Price = p.Price,
                Quantity = p.Quantity,
                MinBuyLimit = p.MinBuyLimit,
                MaxBuyLimit = p.MaxBuyLimit,
                Rating = p.Rating,
                IsGrounded = p.IsGrounded,
                IsActive = p.IsActive,
                BrandId = p.BrandId,
                BrandName = p.Brand.Title,
                Hashtags = p.Hashtags
                    .Select(ph => new HashtagItemDto
                    {
                        Id = ph.Hashtag.Id,
                        Title = ph.Hashtag.Title
                    })
                    .ToList(),
                Images = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Product && i.ReferenceId == p.Id)
                    .OrderByDescending(i => i.ImageRole == ImageRoleEnum.Primary)
                    .ThenBy(i => i.Id)
                    .Select(i => new ResultImageDto
                    {
                        Id = i.Id,
                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Product/" + i.ImageUrl
                    })
                    .Take(2)
                    .ToList(),
                UserImages = _context.BasketItemImages
                    .Where(bi => bi.ProductId == p.Id && bi.Image.IsActive)
                    .Select(bi => new ProductUserImageDto
                    {
                        Id = bi.Image.Id,
                        ImageUrl = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/BasketItem/" + bi.Image.ImageUrl,
                        ImageDescription = bi.Image.ImageDescription,
                        UserId = bi.UserId
                    })
                    .ToList(),
                Categories = _context.ProductProductCategories
                    .Where(pc => pc.ProductId == p.Id)
                    .Select(pc => new ProductCategoryDto
                    {
                        Id = pc.CategoryId,
                        Name = pc.Category.Name,
                        Slug = _context.Slugs
                                    .Where(s => s.ReferenceId == pc.Id && s.Type == SlugTypeEnum.ProductCategory)
                                    .Select(s => s.Title)
                                    .FirstOrDefault(),
                    })
                    .ToList(),
                Offers = _context.ProductOffers
                    .Where(o => o.ProductId == p.Id && o.OfferStatus == OfferStatus.Active)
                    .Select(o => new ProductOfferItemDto
                    {
                        Id = o.Id,
                        ProductId = o.ProductId,
                        OfferDescription = o.OfferDescription,
                        OfferPercentage = o.OfferPercentage,
                        OfferStatus = o.OfferStatus,
                        OfferStart = o.OfferStart,
                        OfferEnd = o.OfferEnd,
                        OfferLimit = o.OfferLimit
                    })
                    .ToList(),
                Slug = _context.Slugs
                    .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                    .Select(s => s.Title)
                    .FirstOrDefault(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            }).FirstOrDefault();

            if (item == null)
            {
                return new ResultDto<ProductItemFrontendDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 404,
                    Message = "محصول یافت نشد"
                };
            }

            var currentBasket = basket.FirstOrDefault();
            if (currentBasket != null)
            {
                var basketItem = _context.BasketItems
                    .Where(p => p.BasketId == currentBasket.Id && p.ProductId == item.Id)
                    .FirstOrDefault();
                item.BasketCount = basketItem?.Count ?? 0;
            }

            wishlist = wishlist.Where(p => p.ProductId == item.Id && p.UserId == userId);
            item.IsInWishlist = wishlist.Any();

            return new ResultDto<ProductItemFrontendDto>
            {
                Data = item,
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        // --------------------- GetItemBranch (IsActive only, Price + WholePrice) ------------------
        public ResultDto<ProductItemBranchDto> GetProductItemBranch(long productId, long userId, string publicToken)
        {
            var basket = _context.Baskets.AsQueryable();
            var wishlist = _context.ProductWishes.AsQueryable();
            if (userId != 0)
            {
                basket = basket.Where(p => p.BasketStatus == BasketStatus.Created && p.UserId == userId);
            }
            else
            {
                basket = basket.Where(p => p.Token == publicToken);
            }

            var item = _context.Products.Where(p => p.Id == productId && p.IsActive).Select(p => new ProductItemBranchDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                LongDescription = p.LongDescription,
                MetaTitle = p.MetaTitle,
                MetaDescription = p.MetaDescription,
                Attribute = p.Attribute,
                Price = p.Price,
                WholePrice = p.WholePrice,
                Quantity = p.Quantity,
                MinBuyLimit = p.MinBuyLimit,
                MaxBuyLimit = p.MaxBuyLimit,
                Rating = p.Rating,
                IsGrounded = p.IsGrounded,
                IsActive = p.IsActive,
                BrandId = p.BrandId,
                BrandName = p.Brand.Title,
                Hashtags = p.Hashtags
                    .Select(ph => new HashtagItemDto
                    {
                        Id = ph.Hashtag.Id,
                        Title = ph.Hashtag.Title
                    })
                    .ToList(),
                Images = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Product && i.ReferenceId == p.Id)
                    .OrderByDescending(i => i.ImageRole == ImageRoleEnum.Primary)
                    .ThenBy(i => i.Id)
                    .Select(i => new ResultImageDto
                    {
                        Id = i.Id,
                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Product/" + i.ImageUrl
                    })
                    .Take(2)
                    .ToList(),
                UserImages = _context.BasketItemImages
                    .Where(bi => bi.ProductId == p.Id && bi.Image.IsActive)
                    .Select(bi => new ProductUserImageDto
                    {
                        Id = bi.Image.Id,
                        ImageUrl = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/BasketItem/" + bi.Image.ImageUrl,
                        ImageDescription = bi.Image.ImageDescription,
                        UserId = bi.UserId
                    })
                    .ToList(),
                Categories = _context.ProductProductCategories
                    .Where(pc => pc.ProductId == p.Id)
                    .Select(pc => new ProductCategoryDto
                    {
                        Id = pc.CategoryId,
                        Name = pc.Category.Name,
                        Slug = _context.Slugs
                                    .Where(s => s.ReferenceId == pc.Id && s.Type == SlugTypeEnum.ProductCategory)
                                    .Select(s => s.Title)
                                    .FirstOrDefault(),
                    })
                    .ToList(),
                Offers = _context.ProductOffers
                    .Where(o => o.ProductId == p.Id && o.OfferStatus == OfferStatus.Active)
                    .Select(o => new ProductOfferItemDto
                    {
                        Id = o.Id,
                        ProductId = o.ProductId,
                        OfferDescription = o.OfferDescription,
                        OfferPercentage = o.OfferPercentage,
                        OfferStatus = o.OfferStatus,
                        OfferStart = o.OfferStart,
                        OfferEnd = o.OfferEnd,
                        OfferLimit = o.OfferLimit
                    })
                    .ToList(),
                Slug = _context.Slugs
                    .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                    .Select(s => s.Title)
                    .FirstOrDefault(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            }).FirstOrDefault();

            if (item == null)
            {
                return new ResultDto<ProductItemBranchDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 404,
                    Message = "محصول یافت نشد"
                };
            }

            var currentBasket = basket.FirstOrDefault();
            if (currentBasket != null)
            {
                var basketItem = _context.BasketItems
                    .Where(p => p.BasketId == currentBasket.Id && p.ProductId == item.Id)
                    .FirstOrDefault();
                item.BasketCount = basketItem?.Count ?? 0;
            }

            wishlist = wishlist.Where(p => p.ProductId == item.Id && p.UserId == userId);
            item.IsInWishlist = wishlist.Any();

            return new ResultDto<ProductItemBranchDto>
            {
                Data = item,
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        // -------------------- GetListByCategory --------------------
        public ResultDto<List<ProductDto>> GetProductListByCategory(ProductListByCategoryRequestDto request)
        {
            try
            {
                if (request.Count <= 0)
                {
                    return new ResultDto<List<ProductDto>>
                    {
                        Data = new List<ProductDto>(),
                        IsSuccess = true,
                        Status = 200,
                        Message = "لیست محصولات"
                    };
                }

                if (request.Type != 1 && request.Type != 2)
                {
                    return new ResultDto<List<ProductDto>>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 400,
                        Message = "نوع درخواست نامعتبر است"
                    };
                }

                string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

                // Base pool: active products, filtered to brand "daniloos" for type 2
                var baseIdsQuery = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive);

                if (request.Type == 2)
                {
                    baseIdsQuery = baseIdsQuery.Where(p => p.Brand.Title == "Daniloos" || p.BrandId == 1);
                }

                var baseIds = baseIdsQuery.Select(p => p.Id);

                // categoryId -> productIds (within the base pool) belonging to it
                var categoryProductMap = _context.ProductProductCategories
                    .AsNoTracking()
                    .Where(pc => baseIds.Contains(pc.ProductId))
                    .GroupBy(pc => pc.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        ProductIds = g.Select(x => x.ProductId).Distinct().ToList()
                    })
                    .ToList()
                    .Where(x => x.ProductIds.Any())
                    .ToList();

                if (!categoryProductMap.Any())
                {
                    return new ResultDto<List<ProductDto>>
                    {
                        Data = new List<ProductDto>(),
                        IsSuccess = true,
                        Status = 200,
                        Message = "موردی یافت نشد"
                    };
                }

                var random = new Random();
                var selectedProductIds = new HashSet<long>();

                if (request.Type == 1)
                {
                    // One random product per category, never picking the same product twice.
                    // Categories are shuffled so, if Count < number of categories,
                    // which categories "win" is also random.
                    var shuffledCategories = categoryProductMap.OrderBy(_ => random.Next()).ToList();

                    foreach (var cat in shuffledCategories)
                    {
                        var availableIds = cat.ProductIds.Where(id => !selectedProductIds.Contains(id)).ToList();
                        if (!availableIds.Any()) continue;

                        selectedProductIds.Add(availableIds[random.Next(availableIds.Count)]);
                    }
                }
                else // Type == 2
                {
                    // Split Count evenly across categories, redistributing any shortfall
                    // (a category with too few products) onto categories that have room.
                    var categoryPool = categoryProductMap
                        .Select(c => new
                        {
                            c.CategoryId,
                            Available = new Queue<long>(c.ProductIds.OrderBy(_ => random.Next()))
                        })
                        .ToList();

                    int remaining = request.Count;
                    bool progressMade = true;

                    while (remaining > 0 && progressMade)
                    {
                        progressMade = false;

                        var activeCats = categoryPool.Where(c => c.Available.Count > 0).ToList();
                        if (!activeCats.Any()) break;

                        int share = Math.Max(1, remaining / activeCats.Count);

                        foreach (var cat in activeCats)
                        {
                            if (remaining <= 0) break;

                            int takeCount = Math.Min(share, Math.Min(cat.Available.Count, remaining));
                            for (int i = 0; i < takeCount; i++)
                            {
                                selectedProductIds.Add(cat.Available.Dequeue());
                                remaining--;
                                progressMade = true;
                            }
                        }
                    }
                }

                var idsList = selectedProductIds.ToList();

                var list = _context.Products
                    .AsNoTracking()
                    .Where(p => idsList.Contains(p.Id))
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Description = p.Description,
                        MetaTitle = p.MetaTitle,
                        MetaDescription = p.MetaDescription,
                        Attribute = p.Attribute,
                        Price = p.Price,
                        Quantity = p.Quantity,
                        Rating = p.Rating,
                        IsGrounded = p.IsGrounded,
                        IsActive = p.IsActive,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Title,
                        MainImageUrl = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Product
                                     && i.ImageRole == ImageRoleEnum.Primary
                                     && i.ReferenceId == p.Id)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault() == null ? null :
                                staticPath + "/Resources/Product/" + _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.Product
                                             && i.ImageRole == ImageRoleEnum.Primary
                                             && i.ReferenceId == p.Id)
                                    .Select(i => i.ImageUrl)
                                    .FirstOrDefault(),
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        Offers = _context.ProductOffers
                            .Where(o => o.ProductId == (int)p.Id && o.OfferStatus == OfferStatus.Active)
                            .Select(o => new ProductOfferDto
                            {
                                Id = o.Id,
                                ProductId = o.ProductId,
                                OfferPercentage = o.OfferPercentage,
                                OfferStatus = o.OfferStatus,
                                OfferEnd = o.OfferEnd
                            })
                            .ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                // Shuffle so results aren't grouped by category-fill order
                list = list.OrderBy(_ => random.Next()).ToList();

                return new ResultDto<List<ProductDto>>
                {
                    Data = list,
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست محصولات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductDto>>(ex);
            }
        }

        // -------------------- GetWishlist --------------------
        public ResultDto<List<ProductDto>> GetWishlist(ProductWishlistRequestDto request)
        {
            try
            {
                string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

                var query = _context.ProductWishes
                    .AsNoTracking()
                    .Where(w => w.UserId == request.UserId)
                    .Select(w => w.Product)
                    .Where(p => p.IsActive);

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
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Description = p.Description,
                        MetaTitle = p.MetaTitle,
                        MetaDescription = p.MetaDescription,
                        Attribute = p.Attribute,
                        Price = p.Price,
                        Quantity = p.Quantity,
                        Rating = p.Rating,
                        IsActive = p.IsActive,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Title,
                        MainImageUrl = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Product
                                     && i.ImageRole == ImageRoleEnum.Primary
                                     && i.ReferenceId == p.Id)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault() == null ? null :
                                staticPath + "/Resources/Product/" + _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.Product
                                             && i.ImageRole == ImageRoleEnum.Primary
                                             && i.ReferenceId == p.Id)
                                    .Select(i => i.ImageUrl)
                                    .FirstOrDefault(),
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        Offers = _context.ProductOffers
                            .Where(o => o.ProductId == p.Id && o.OfferStatus == OfferStatus.Active)
                            .Select(o => new ProductOfferDto
                            {
                                Id = o.Id,
                                ProductId = o.ProductId,
                                OfferPercentage = o.OfferPercentage,
                                OfferStatus = o.OfferStatus,
                                OfferEnd = o.OfferEnd
                            })
                            .ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ProductDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست علاقه مندی ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductDto>>(ex);
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

    // ==================== DTOs — List level ====================

    /// <summary>Admin/full product listing. Includes WholePrice. No IsActive filter applied by caller.</summary>
    public class ProductDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public float Rating { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsActive { get; set; }
        public long? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? MainImageUrl { get; set; }
        public string? Slug { get; set; }
        public List<ProductOfferDto> Offers { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Frontend product listing. IsActive == true only. No WholePrice.</summary>
    public class ProductFrontEndDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public float Rating { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsActive { get; set; }
        public long? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? MainImageUrl { get; set; }
        public string? Slug { get; set; }
        public List<ProductOfferDto> Offers { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Branch product listing. IsActive == true only. Includes both Price and WholePrice, plus basket/grounded info.</summary>
    public class ProductBranchDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public float Rating { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsActive { get; set; }
        public long? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? MainImageUrl { get; set; }
        public string? Slug { get; set; }
        public List<ProductOfferDto> Offers { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Filled only by GetProductListBranch
        public bool IsGroundedItem { get; set; }  // this row is the ground variant (splitGrounded only)
        public int BasketCount { get; set; }      // count in the basket for this row's variant

        public ProductBranchDto AsGroundedItem()
        {
            var copy = (ProductBranchDto)MemberwiseClone();
            copy.IsGroundedItem = true;
            return copy;
        }
    }

    // ==================== DTOs — Item level ====================

    /// <summary>Admin/full product item. Includes WholePrice. No IsActive filter applied by caller.</summary>
    public class ProductItemDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }
        public float Rating { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsActive { get; set; }
        public long? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? Slug { get; set; }
        public List<HashtagItemDto> Hashtags { get; set; } = new();
        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductUserImageDto> UserImages { get; set; } = new();
        public List<ProductCategoryDto> Categories { get; set; } = new();
        public List<ProductRelationItemDto> Relations { get; set; } = new();
        public List<ProductOfferItemDto> Offers { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Frontend product item. IsActive == true only. No WholePrice.</summary>
    public class ProductItemFrontendDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }
        public float Rating { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsActive { get; set; }
        public long? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? Slug { get; set; }
        public List<HashtagItemDto> Hashtags { get; set; } = new();
        public List<ResultImageDto> Images { get; set; } = new();
        public List<ProductUserImageDto> UserImages { get; set; } = new();
        public List<ProductCategoryDto> Categories { get; set; } = new();
        public List<ProductOfferItemDto> Offers { get; set; } = new();
        public int BasketCount { get; set; }
        public bool IsInWishlist { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Branch product item. IsActive == true only. Includes both Price and WholePrice.</summary>
    public class ProductItemBranchDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }
        public float Rating { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsActive { get; set; }
        public long? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? Slug { get; set; }
        public List<HashtagItemDto> Hashtags { get; set; } = new();
        public List<ResultImageDto> Images { get; set; } = new();
        public List<ProductUserImageDto> UserImages { get; set; } = new();
        public List<ProductCategoryDto> Categories { get; set; } = new();
        public List<ProductOfferItemDto> Offers { get; set; } = new();
        public int BasketCount { get; set; }
        public bool IsInWishlist { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ==================== DTOs — Shared / supporting ====================

    public class ResultImageDto
    {
        public long Id { get; set; }
        public string Preview { get; set; }
    }

    public class HashtagItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public bool ShowMenu { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductImageDto
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; }
        public string? ImageDescription { get; set; }
        public ImageRoleEnum ImageTypeEnum { get; set; }
    }

    public class ProductRelationItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class ProductUserImageDto
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; }
        public string? ImageDescription { get; set; }
        public long UserId { get; set; }
    }

    // ==================== DTOs — Requests ====================

    public class ProductListRequestDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Sort { get; set; }
        public string? Filter { get; set; }
    }

    public class ProductWishlistRequestDto
    {
        public long UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Sort { get; set; }
        public string? Filter { get; set; }
    }

    public class ProductListByCategoryRequestDto
    {
        public int Count { get; set; }
        public int Type { get; set; }
        // 1 = one random product per category (no dupes)
        // 2 = Brand "daniloos", distributed across categories to hit Count
    }
}