using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Common;
using Common.Dto;
using Domain.Entities.Baskets;
using Domain.Entities.Branches;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Nodes;

namespace Application.Services.Products.Commands
{
    // ==================== Interface ====================
    public interface IProductCUDService
    {
        ResultDto<ProductResultDto> AddProduct(RequestAddProductDto request, long currentUserId);
        ResultDto<ProductResultDto> EditProduct(RequestEditProductDto request, long currentUserId);
        ResultDto RemoveProduct(long id, long currentUserId);
        ResultDto<ProductWishResultDto> AddWish(long productId, long currentUserId);
        ResultDto RemoveWish(long productId, long currentUserId);
        ResultDto<ProductImageDeleteResultDto> RemoveProductImage(long imageId, long currentUserId);
        ResultDto EditProductPrimaryImage(long productId, long imageId, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ProductCUDService : IProductCUDService
    {
        private readonly IDataBaseContext _context;
        private readonly ISlugCUDService _slugCUDService;
        private readonly IImageCUDService _imageCUDService;

        public ProductCUDService(IDataBaseContext context, ISlugCUDService slugCUDService, IImageCUDService imageCUDService)
        {
            _context = context;
            _slugCUDService = slugCUDService;
            _imageCUDService = imageCUDService;
        }

        // -------------------- Add --------------------
        public ResultDto<ProductResultDto> AddProduct(RequestAddProductDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                //// Validate ProductId is unique
                //if (_context.Products.Any(p => p.ProductId == request.ProductId))
                //{
                //    return new ResultDto<ProductResultDto>
                //    {
                //        IsSuccess = false,
                //        Message = "شناسه محصول وارد شده تکراری است",
                //        Status = 400
                //    };
                //}

                // Validate brand up front, same idea as the join-table version used to do
                if (request.BrandId.HasValue && !_context.Brands.Any(b => b.Id == request.BrandId.Value))
                {
                    return new ResultDto<ProductResultDto>
                    {
                        IsSuccess = false,
                        Message = "برند یافت نشد",
                        Status = 400
                    };
                }

                // Validate categories exist
                if (request.CategoryIds != null && request.CategoryIds.Any())
                {
                    var distinctCategoryIds = request.CategoryIds.Distinct().ToList();
                    var foundCategoryCount = _context.ProductCategories
                        .Count(c => distinctCategoryIds.Contains(c.Id));

                    if (foundCategoryCount != distinctCategoryIds.Count)
                    {
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = "یک یا چند دسته‌بندی یافت نشد",
                            Status = 400
                        };
                    }
                }

                // Validate hashtags exist
                if (request.HashtagIds != null && request.HashtagIds.Any())
                {
                    var distinctHashtagIds = request.HashtagIds.Distinct().ToList();
                    var foundHashtagCount = _context.Hashtags
                        .Count(h => distinctHashtagIds.Contains(h.Id));

                    if (foundHashtagCount != distinctHashtagIds.Count)
                    {
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = "یک یا چند هشتگ یافت نشد",
                            Status = 400
                        };
                    }
                }

                // Validate related products exist
                if (request.RelatedProductIds != null && request.RelatedProductIds.Any())
                {
                    var distinctRelatedIds = request.RelatedProductIds.Distinct().ToList();
                    var foundRelatedCount = _context.Products
                        .Count(p => distinctRelatedIds.Contains(p.Id));

                    if (foundRelatedCount != distinctRelatedIds.Count)
                    {
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = "یک یا چند محصول مرتبط یافت نشد",
                            Status = 400
                        };
                    }
                }

                // Validate slug
                if (string.IsNullOrWhiteSpace(request.SlugTitle))
                {
                    return new ResultDto<ProductResultDto>
                    {
                        IsSuccess = false,
                        Message = "عنوان اسلاگ الزامی است",
                        Status = 400
                    };
                }

                var product = new Product
                {
                    ProductId = request.ProductId,
                    Name = request.Name,
                    Description = request.Description,
                    LongDescription = request.LongDescription,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    Price = request.Price,
                    WholePrice = request.WholePrice,
                    Quantity = request.Quantity,
                    Weight = request.Weight,
                    MinBuyLimit = request.MinBuyLimit,
                    MaxBuyLimit = request.MaxBuyLimit,
                    Rating = request.Rating,
                    BrandId = request.BrandId,
                    IsGrounded = (bool)request.IsGrounded,
                    IsActive = (bool)request.IsActive,
                    Attribute = request.Attribute,
                    CreatedById = currentUserId,
                };

                _context.SetUserId(currentUserId);
                _context.Products.Add(product);
                _context.SaveChanges();

                // -------------------- Slug --------------------
                var slugResult = _slugCUDService.AddSlug(new RequestAddSlugDto
                {
                    Title = request.SlugTitle,
                    Type = SlugTypeEnum.Product,
                    ReferenceId = product.Id
                }, currentUserId);

                if (!slugResult.IsSuccess)
                {
                    transaction.Rollback();
                    return new ResultDto<ProductResultDto>
                    {
                        IsSuccess = false,
                        Message = slugResult.Message,
                        Status = slugResult.Status
                    };
                }

                // -------------------- Images --------------------
                if (request.ImageNames != null && request.ImageNames.Count > 0)
                {
                    var addImagesResult = AddProductImages(product.Id, request.ImageNames, request.ImageDescriptions, currentUserId);
                    if (!addImagesResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = addImagesResult.Message,
                            Status = addImagesResult.Status
                        };
                    }
                }

                // -------------------- Categories --------------------
                if (request.CategoryIds != null && request.CategoryIds.Any())
                {
                    var categories = request.CategoryIds
                        .Distinct()
                        .Select(categoryId => new ProductProductCategory
                        {
                            ProductId = product.Id,
                            CategoryId = categoryId
                        })
                        .ToList();

                    _context.ProductProductCategories.AddRange(categories);
                    _context.SaveChanges();
                }

                // -------------------- Hashtags --------------------
                if (request.HashtagIds != null && request.HashtagIds.Any())
                {
                    var hashtags = request.HashtagIds
                        .Distinct()
                        .Select(hashtagId => new ProductHashtag
                        {
                            ProductId = product.Id,
                            HashtagId = hashtagId
                        })
                        .ToList();

                    _context.ProductHashtags.AddRange(hashtags);
                    _context.SaveChanges();
                }

                // -------------------- Related products --------------------
                if (request.RelatedProductIds != null && request.RelatedProductIds.Any())
                {
                    var relations = request.RelatedProductIds
                        .Distinct()
                        .Where(relatedId => relatedId != product.Id) // a product can't relate to itself
                        .Select(relatedId => new ProductRelation
                        {
                            ProductId = product.Id,
                            RelatedProductId = relatedId
                        })
                        .ToList();

                    _context.ProductRelations.AddRange(relations);
                    _context.SaveChanges();
                }

                transaction.Commit();

                product.Order = product.Id;
                _context.SaveChanges();

                return new ResultDto<ProductResultDto>
                {
                    Data = MapToResult(product),
                    IsSuccess = true,
                    Message = "محصول با موفقیت ایجاد گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ProductResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Edit --------------------
        public ResultDto<ProductResultDto> EditProduct(RequestEditProductDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var product = _context.Products
                    .FirstOrDefault(p => p.Id == request.Id);

                if (product == null)
                    return new ResultDto<ProductResultDto> { IsSuccess = false, Message = "محصول یافت نشد", Status = 400 };

                // Validate ProductId is unique (excluding current product)
                //if (_context.Products.Any(p => p.ProductId == request.ProductId && p.Id != product.Id))
                //{
                //    return new ResultDto<ProductResultDto>
                //    {
                //        IsSuccess = false,
                //        Message = "شناسه محصول وارد شده تکراری است",
                //        Status = 400
                //    };
                //}

                if (request.BrandId.HasValue && !_context.Brands.Any(b => b.Id == request.BrandId.Value))
                {
                    return new ResultDto<ProductResultDto>
                    {
                        IsSuccess = false,
                        Message = "برند یافت نشد",
                        Status = 400
                    };
                }

                // Validate categories exist
                if (request.CategoryIds != null && request.CategoryIds.Any())
                {
                    var distinctCategoryIds = request.CategoryIds.Distinct().ToList();
                    var foundCategoryCount = _context.ProductCategories
                        .Count(c => distinctCategoryIds.Contains(c.Id));

                    if (foundCategoryCount != distinctCategoryIds.Count)
                    {
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = "یک یا چند دسته‌بندی یافت نشد",
                            Status = 400
                        };
                    }
                }

                // Validate hashtags exist
                if (request.HashtagIds != null && request.HashtagIds.Any())
                {
                    var distinctHashtagIds = request.HashtagIds.Distinct().ToList();
                    var foundHashtagCount = _context.Hashtags
                        .Count(h => distinctHashtagIds.Contains(h.Id));

                    if (foundHashtagCount != distinctHashtagIds.Count)
                    {
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = "یک یا چند هشتگ یافت نشد",
                            Status = 400
                        };
                    }
                }

                // Validate related products exist
                if (request.RelatedProductIds != null && request.RelatedProductIds.Any())
                {
                    var distinctRelatedIds = request.RelatedProductIds
                        .Distinct()
                        .Where(relatedId => relatedId != product.Id)
                        .ToList();

                    var foundRelatedCount = _context.Products
                        .Count(p => distinctRelatedIds.Contains(p.Id));

                    if (foundRelatedCount != distinctRelatedIds.Count)
                    {
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = "یک یا چند محصول مرتبط یافت نشد",
                            Status = 400
                        };
                    }
                }

                product.ProductId = request.ProductId;
                product.Name = request.Name;
                product.Description = request.Description;
                product.LongDescription = request.LongDescription;
                product.MetaTitle = request.MetaTitle;
                product.MetaDescription = request.MetaDescription;
                product.Price = request.Price;
                product.WholePrice = request.WholePrice;
                product.Quantity = request.Quantity;
                product.Weight = request.Weight;
                product.MinBuyLimit = request.MinBuyLimit;
                product.MaxBuyLimit = request.MaxBuyLimit;
                product.IsGrounded = (bool)request.IsGrounded;
                product.Rating = request.Rating;
                product.BrandId = request.BrandId;
                product.IsActive = (bool)request.IsActive;
                product.Attribute = request.Attribute;
                product.UpdatedById = currentUserId;

                _context.SetUserId(currentUserId);
                _context.SaveChanges();

                SyncBasketItemsForProduct(product);

                // -------------------- Slug (optional on edit) --------------------
                if (!string.IsNullOrWhiteSpace(request.SlugTitle))
                {
                    var existingSlug = _context.Slugs
                        .FirstOrDefault(s => s.Type == SlugTypeEnum.Product && s.ReferenceId == product.Id);

                    if (existingSlug == null)
                    {
                        transaction.Rollback();
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = "اسلاگ این محصول یافت نشد",
                            Status = 400
                        };
                    }

                    var slugResult = _slugCUDService.EditSlug(new RequestEditSlugDto
                    {
                        Id = existingSlug.Id,
                        Title = request.SlugTitle,
                    }, currentUserId);

                    if (!slugResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = slugResult.Message,
                            Status = slugResult.Status
                        };
                    }
                }

                // -------------------- Add images (append only, never replace) --------------------
                if (request.ImageNames != null && request.ImageNames.Count > 0)
                {
                    var addImagesResult = AddProductImages(product.Id, request.ImageNames, request.ImageDescriptions, currentUserId);
                    if (!addImagesResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<ProductResultDto>
                        {
                            IsSuccess = false,
                            Message = addImagesResult.Message,
                            Status = addImagesResult.Status
                        };
                    }
                }

                // -------------------- Categories --------------------
                if (request.CategoryIds != null && request.CategoryIds.Any())
                {
                    var existingCategories = _context.ProductProductCategories
                        .Where(c => c.ProductId == product.Id)
                        .ToList();
                    _context.ProductProductCategories.RemoveRange(existingCategories);

                    var categories = request.CategoryIds
                        .Distinct()
                        .Select(categoryId => new ProductProductCategory
                        {
                            ProductId = product.Id,
                            CategoryId = categoryId
                        })
                        .ToList();

                    _context.ProductProductCategories.AddRange(categories);
                    _context.SaveChanges();
                }

                // -------------------- Hashtags --------------------
                if (request.HashtagIds != null && request.HashtagIds.Any())
                {
                    var existingHashtags = _context.ProductHashtags
                        .Where(h => h.ProductId == product.Id)
                        .ToList();
                    _context.ProductHashtags.RemoveRange(existingHashtags);

                    var hashtags = request.HashtagIds
                        .Distinct()
                        .Select(hashtagId => new ProductHashtag
                        {
                            ProductId = product.Id,
                            HashtagId = hashtagId
                        })
                        .ToList();

                    _context.ProductHashtags.AddRange(hashtags);
                    _context.SaveChanges();
                }

                // -------------------- Related products --------------------
                if (request.RelatedProductIds != null && request.RelatedProductIds.Any())
                {
                    var existingRelations = _context.ProductRelations
                        .Where(r => r.ProductId == product.Id)
                        .ToList();
                    _context.ProductRelations.RemoveRange(existingRelations);

                    var relations = request.RelatedProductIds
                        .Distinct()
                        .Where(relatedId => relatedId != product.Id)
                        .Select(relatedId => new ProductRelation
                        {
                            ProductId = product.Id,
                            RelatedProductId = relatedId
                        })
                        .ToList();

                    _context.ProductRelations.AddRange(relations);
                    _context.SaveChanges();
                }

                transaction.Commit();

                var mapped = MapToResult(product);

                return new ResultDto<ProductResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "محصول با موفقیت به روز گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ProductResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove --------------------
        public ResultDto RemoveProduct(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var product = _context.Products.Find(id);

                if (product == null)
                    return new ResultDto { IsSuccess = false, Message = "محصول یافت نشد", Status = 400 };

                RemoveProductFromActiveBaskets(product.Id);

                _context.SetUserId(currentUserId);
                _context.Products.Remove(product);
                _context.SaveChanges();

                var slugCleanup = _slugCUDService.RemoveSlugsByReference(
                    SlugTypeEnum.Product, id, currentUserId);

                if (!slugCleanup.IsSuccess)
                    throw new Exception($"پاک کردن اسلاگ با موفقیت انجام نشد: {id}: {slugCleanup.Message}");

                // -------------------- Image cleanup --------------------
                var imageCleanup = _imageCUDService.RemoveImagesByReference(
                    ImageTypeEnum.Product, id, currentUserId);

                if (!imageCleanup.IsSuccess)
                    throw new Exception($"پاک کردن تصاویر با موفقیت انجام نشد: {id}: {imageCleanup.Message}");

                transaction.Commit();

                var oldImagesToDelete = imageCleanup.Data?
                    .Select(i => i.OldImage)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList() ?? new List<string>();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "محصول با موفقیت حذف گردید",
                    Status = 200,
                    Result = oldImagesToDelete
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                DetachAll();

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message ?? ex.Message,
                    RequestParameter = id.ToJson(),
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

        // -------------------- RemoveProductImage --------------------
        public ResultDto<ProductImageDeleteResultDto> RemoveProductImage(long imageId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var image = _context.Images
                    .FirstOrDefault(i => i.Id == imageId && i.ImageType == ImageTypeEnum.Product);

                if (image == null)
                {
                    return new ResultDto<ProductImageDeleteResultDto>
                    {
                        IsSuccess = false,
                        Message = "تصویر یافت نشد",
                        Status = 404
                    };
                }

                var wasPrimary = image.ImageRole == ImageRoleEnum.Primary;
                var productId = image.ReferenceId;

                var removeResult = _imageCUDService.RemoveImage(imageId, currentUserId);

                if (!removeResult.IsSuccess)
                {
                    transaction.Rollback();
                    return new ResultDto<ProductImageDeleteResultDto>
                    {
                        IsSuccess = false,
                        Message = removeResult.Message,
                        Status = removeResult.Status
                    };
                }

                if (wasPrimary && productId.HasValue)
                {
                    var nextImage = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Product && i.ReferenceId == productId.Value)
                        .OrderBy(i => i.Id)
                        .FirstOrDefault();

                    if (nextImage != null)
                    {
                        var promoteResult = _imageCUDService.EditImage(new RequestEditImageDto
                        {
                            Id = nextImage.Id,
                            ImageUrl = nextImage.ImageUrl,
                            ImageDescription = nextImage.ImageDescription,
                            ImageRole = ImageRoleEnum.Primary
                        }, currentUserId);

                        if (!promoteResult.IsSuccess)
                        {
                            transaction.Rollback();
                            return new ResultDto<ProductImageDeleteResultDto>
                            {
                                IsSuccess = false,
                                Message = promoteResult.Message,
                                Status = promoteResult.Status
                            };
                        }
                    }
                }

                transaction.Commit();

                return new ResultDto<ProductImageDeleteResultDto>
                {
                    Data = new ProductImageDeleteResultDto
                    {
                        Id = imageId,
                        OldImageToDelete = removeResult.Data?.OldImage
                    },
                    IsSuccess = true,
                    Message = "تصویر با موفقیت حذف گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ProductImageDeleteResultDto>(ex, imageId.ToJson(), currentUserId);
            }
        }

        // -------------------- EditProductPrimaryImage --------------------
        public ResultDto EditProductPrimaryImage(long productId, long imageId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var images = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Product && i.ReferenceId == productId)
                    .ToList();

                if (!images.Any())
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "تصویری برای این محصول یافت نشد",
                        Status = 404
                    };
                }

                var targetImage = images.FirstOrDefault(i => i.Id == imageId);

                if (targetImage == null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "تصویر یافت نشد یا متعلق به این محصول نیست",
                        Status = 404
                    };
                }

                if (targetImage.ImageRole == ImageRoleEnum.Primary)
                {
                    return new ResultDto
                    {
                        IsSuccess = true,
                        Message = "این تصویر از قبل تصویر اصلی است",
                        Status = 200
                    };
                }

                var currentPrimaries = images.Where(i => i.ImageRole == ImageRoleEnum.Primary && i.Id != imageId).ToList();
                foreach (var img in currentPrimaries)
                {
                    var demoteResult = _imageCUDService.EditImage(new RequestEditImageDto
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl,
                        ImageDescription = img.ImageDescription,
                        ImageRole = ImageRoleEnum.Secondary
                    }, currentUserId);

                    if (!demoteResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto
                        {
                            IsSuccess = false,
                            Message = demoteResult.Message,
                            Status = demoteResult.Status
                        };
                    }
                }

                var promoteResult = _imageCUDService.EditImage(new RequestEditImageDto
                {
                    Id = targetImage.Id,
                    ImageUrl = targetImage.ImageUrl,
                    ImageDescription = targetImage.ImageDescription,
                    ImageRole = ImageRoleEnum.Primary
                }, currentUserId);

                if (!promoteResult.IsSuccess)
                {
                    transaction.Rollback();
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = promoteResult.Message,
                        Status = promoteResult.Status
                    };
                }

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "تصویر اصلی با موفقیت تغییر یافت",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                DetachAll();

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message ?? ex.Message,
                    RequestParameter = new { productId, imageId }.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);
                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400
                };
            }
        }

        // -------------------- AddWish --------------------
        public ResultDto<ProductWishResultDto> AddWish(long productId, long currentUserId)
        {
            var product = _context.Products
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return new ResultDto<ProductWishResultDto>
                {
                    IsSuccess = false,
                    Message = "محصول یافت نشد",
                    Status = 404
                };

            if (!product.IsActive)
                return new ResultDto<ProductWishResultDto>
                {
                    IsSuccess = false,
                    Message = "این محصول غیرفعال است",
                    Status = 400
                };

            var existing = _context.ProductWishes
                .FirstOrDefault(w => w.ProductId == productId && w.UserId == currentUserId);

            if (existing != null)
            {
                return new ResultDto<ProductWishResultDto>
                {
                    Data = MapToResult(existing),
                    IsSuccess = true,
                    Message = "محصول قبلا به علاقه مندی ها اضافه شده است",
                    Status = 200
                };
            }

            var wish = new ProductWish
            {
                ProductId = productId,
                UserId = currentUserId
            };

            _context.ProductWishes.Add(wish);
            _context.SaveChanges();

            return new ResultDto<ProductWishResultDto>
            {
                Data = MapToResult(wish),
                IsSuccess = true,
                Message = "محصول با موفقیت به علاقه مندی ها اضافه گردید",
                Status = 200
            };
        }

        // -------------------- RemoveWish --------------------
        public ResultDto RemoveWish(long productId, long currentUserId)
        {
            var wish = _context.ProductWishes
                .FirstOrDefault(w => w.ProductId == productId && w.UserId == currentUserId);

            if (wish == null)
                return new ResultDto { IsSuccess = false, Message = "محصول در علاقه مندی ها یافت نشد", Status = 400 };

            _context.ProductWishes.Remove(wish);
            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Message = "محصول با موفقیت از علاقه مندی ها حذف گردید", Status = 200 };
        }

        // ==================== Helpers ====================
        private ResultDto AddProductImages(long productId, List<string> imageNames, List<string?> imageDescriptions, long currentUserId)
        {
            var hasPrimary = _context.Images
                .Any(i => i.ImageType == ImageTypeEnum.Product
                        && i.ReferenceId == productId
                        && i.ImageRole == ImageRoleEnum.Primary);

            for (int i = 0; i < imageNames.Count; i++)
            {
                var role = (!hasPrimary && i == 0) ? ImageRoleEnum.Primary : ImageRoleEnum.Secondary;
                var description = imageDescriptions != null && imageDescriptions.Count > i ? imageDescriptions[i] : null;

                var imageResult = _imageCUDService.AddImage(new RequestAddImageDto
                {
                    ImageUrl = imageNames[i],
                    ImageDescription = description,
                    ReferenceId = productId,
                    ImageType = ImageTypeEnum.Product,
                    ImageRole = role,
                }, currentUserId);

                if (!imageResult.IsSuccess)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = imageResult.Message,
                        Status = imageResult.Status
                    };
                }
            }

            return new ResultDto { IsSuccess = true, Status = 200 };
        }

        private void SyncBasketItemsForProduct(Product product)
        {
            var affectedItems = _context.BasketItems
                .Include(bi => bi.Basket)
                .Where(bi => bi.ProductId == product.Id
                          && bi.Basket.BasketStatus == BasketStatus.Created)
                .ToList();

            if (!affectedItems.Any())
                return;

            var itemsToRemove = affectedItems
                .Where(bi => !product.IsActive
                          || product.Quantity <= 0
                          || (bi.IsGrounded && !product.IsGrounded))
                .ToList();

            var itemsToUpdate = affectedItems.Except(itemsToRemove).ToList();

            var (offerDiscount, offerDiscountType) = ResolveOfferDiscount(product);

            var touchedBaskets = new List<Basket>();

            foreach (var item in itemsToUpdate)
            {
                // Only overwrite the discount if it's the kind we manage automatically
                // (i.e. not something a manual/admin override set on the item).
                var isOfferManaged = item.DiscountType == null || item.DiscountType == DiscountTypeProductOffer;

                var priceChanged = item.Price != product.Price;
                var discountChanged = isOfferManaged
                    && (item.Discount != offerDiscount || item.DiscountType != offerDiscountType);

                if (!priceChanged && !discountChanged)
                    continue;

                item.Price = product.Price;
                if (isOfferManaged)
                {
                    item.Discount = offerDiscount;
                    item.DiscountType = offerDiscountType;
                }

                _context.BasketItems.Update(item);
                touchedBaskets.Add(item.Basket);
            }

            if (itemsToRemove.Any())
            {
                touchedBaskets.AddRange(itemsToRemove.Select(bi => bi.Basket));
                _context.BasketItems.RemoveRange(itemsToRemove);
            }

            if (!touchedBaskets.Any())
                return;

            ClearBasketDiscountsIfAny(touchedBaskets.Distinct());

            _context.SaveChanges();
        }

        private void RemoveProductFromActiveBaskets(long productId)
        {
            var affectedItems = _context.BasketItems
                .Include(bi => bi.Basket)
                .Where(bi => bi.ProductId == productId
                          && bi.Basket.BasketStatus == BasketStatus.Created)
                .ToList();

            if (!affectedItems.Any())
                return;

            var affectedBaskets = affectedItems
                .Select(bi => bi.Basket)
                .Distinct()
                .ToList();

            _context.BasketItems.RemoveRange(affectedItems);
            ClearBasketDiscountsIfAny(affectedBaskets);

            _context.SaveChanges();
        }

        private void ClearBasketDiscountsIfAny(IEnumerable<Basket> baskets)
        {
            foreach (var basket in baskets)
            {
                if (basket.DiscountId == null && basket.DiscountCode == null && basket.DiscountPrice == null)
                    continue;

                var reservation = _context.Discounts.FirstOrDefault(d =>
                    d.BasketId == basket.Id &&
                    d.DiscountCodeId == basket.DiscountId!.Value &&
                    !d.Used);

                if (reservation != null)
                    _context.Discounts.Remove(reservation);

                basket.DiscountId = null;
                basket.DiscountCode = null;
                basket.DiscountPrice = null;
                basket.PriceAfterDiscount = null;
            }
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

        private static ProductResultDto MapToResult(Product product) => new()
        {
            Id = (int)product.Id,
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            LongDescription = product.LongDescription,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            Attribute = product.Attribute,
            Price = product.Price,
            WholePrice = product.WholePrice,
            Quantity = product.Quantity,
            Weight = product.Weight,
            MinBuyLimit = product.MinBuyLimit,
            MaxBuyLimit = product.MaxBuyLimit,
            Rating = product.Rating,
            BrandId = product.BrandId,
            IsGrounded = product.IsGrounded,
            IsActive = product.IsActive,
        };

        private static ProductRelationResultDto MapToResult(ProductRelation relation) => new()
        {
            Id = relation.Id,
            ProductId = relation.ProductId,
            RelatedProductId = relation.RelatedProductId
        };

        private static ProductWishResultDto MapToResult(ProductWish wish) => new()
        {
            Id = wish.Id,
            ProductId = wish.ProductId,
            UserId = wish.UserId
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
                Status = 400
            };
        }
    }

    // ==================== DTOs ====================

    public class RequestAddProductDto
    {
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }
        public float Rating { get; set; }
        public long? BrandId { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsGrounded { get; set; }
        public string SlugTitle { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public List<long> CategoryIds { get; set; } = new List<long>();
        public List<long> HashtagIds { get; set; } = new List<long>();
        public List<long> RelatedProductIds { get; set; } = new List<long>();
        public List<string>? ImageNames { get; set; }
        public List<string?>? ImageDescriptions { get; set; }
    }

    public class RequestEditProductDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }
        public float Rating { get; set; }
        public long? BrandId { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsGrounded { get; set; }
        public string? SlugTitle { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public List<long> CategoryIds { get; set; } = new List<long>();
        public List<long> HashtagIds { get; set; } = new List<long>();
        public List<long> RelatedProductIds { get; set; } = new List<long>();
        public List<string>? ImageNames { get; set; }
        public List<string?>? ImageDescriptions { get; set; }
    }

    public class ProductResultDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? Weight { get; set; }
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }
        public float Rating { get; set; }
        public long? BrandId { get; set; }
        public string? SlugTitle { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Attribute { get; set; }
        public bool? IsGrounded { get; set; }
        public bool? IsActive { get; set; }
        public List<string> OldImages { get; set; } = new List<string>();
    }

    public class ProductImageDeleteResultDto
    {
        public long Id { get; set; }
        public string? OldImageToDelete { get; set; }
    }

    public class RequestAddProductRelationDto
    {
        public long ProductId { get; set; }
        public long RelatedProductId { get; set; }
    }

    public class RequestEditProductRelationDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public long RelatedProductId { get; set; }
    }

    public class ProductRelationResultDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public long RelatedProductId { get; set; }
    }

    public class ProductWishResultDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public long? UserId { get; set; }
    }
}