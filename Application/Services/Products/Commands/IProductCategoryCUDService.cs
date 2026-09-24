using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Application.Services.Products.Commands
{
    // ==================== Interface ====================
    public interface IProductCategoryCUDService
    {
        ResultDto<ProductCategoryResultDto> AddProductCategory(RequestAddProductCategoryDto request, long currentUserId);
        ResultDto<ProductCategoryResultDto> EditProductCategory(RequestEditProductCategoryDto request, long currentUserId);
        ResultDto<ProductCategoryResultDto> RemoveProductCategory(long id, long currentUserId);
        ResultDto<ProductCategoryResultDto> RemoveProductCategoryImage(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ProductCategoryCUDService : IProductCategoryCUDService
    {
        private readonly IDataBaseContext _context;
        private readonly ISlugCUDService _slugCUDService;
        private readonly IImageCUDService _imageCUDService;

        public ProductCategoryCUDService(IDataBaseContext context, ISlugCUDService slugCUDService, IImageCUDService imageCUDService)
        {
            _context = context;
            _slugCUDService = slugCUDService;
            _imageCUDService = imageCUDService;
        }

        // -------------------- Add --------------------
        public ResultDto<ProductCategoryResultDto> AddProductCategory(RequestAddProductCategoryDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                if (request.ParentId.HasValue && !_context.ProductCategories.Any(c => c.Id == request.ParentId.Value))
                {
                    return new ResultDto<ProductCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = "دسته بندی والد یافت نشد",
                        Status = 400
                    };
                }

                if (string.IsNullOrWhiteSpace(request.SlugTitle))
                {
                    return new ResultDto<ProductCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = "عنوان اسلاگ الزامی است",
                        Status = 400
                    };
                }

                var category = new ProductCategory
                {
                    Name = request.Name,
                    Description = request.Description,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    ParentId = request.ParentId,
                    IsActive = request.IsActive
                };

                _context.SetUserId(currentUserId);
                _context.ProductCategories.Add(category);
                _context.SaveChanges();

                // -------------------- Slug --------------------
                var slugResult = _slugCUDService.AddSlug(new RequestAddSlugDto
                {
                    Title = request.SlugTitle,
                    Type = SlugTypeEnum.ProductCategory,
                    ReferenceId = category.Id
                }, currentUserId);

                if (!slugResult.IsSuccess)
                {
                    transaction.Rollback();
                    return new ResultDto<ProductCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = slugResult.Message,
                        Status = slugResult.Status
                    };
                }

                // -------------------- Image --------------------
                if (!string.IsNullOrWhiteSpace(request.ImageFileName))
                {
                    var imageResult = _imageCUDService.AddImage(new RequestAddImageDto
                    {
                        ImageUrl = request.ImageFileName,
                        ImageDescription = request.ImageDescription,
                        ReferenceId = category.Id,
                        ImageType = ImageTypeEnum.ProductCategory,
                        ImageRole = ImageRoleEnum.Single,
                    }, currentUserId);

                    if (!imageResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<ProductCategoryResultDto>
                        {
                            IsSuccess = false,
                            Message = imageResult.Message,
                            Status = imageResult.Status
                        };
                    }
                }

                transaction.Commit();

                category.Order = category.Id;
                _context.SaveChanges();

                return new ResultDto<ProductCategoryResultDto>
                {
                    Data = MapToResult(category),
                    IsSuccess = true,
                    Message = "دسته بندی با موفقیت ایجاد گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ProductCategoryResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Edit --------------------
        public ResultDto<ProductCategoryResultDto> EditProductCategory(RequestEditProductCategoryDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            string oldImageToDelete = null;

            try
            {
                var category = _context.ProductCategories.FirstOrDefault(c => c.Id == request.Id);
                if (category == null)
                    return new ResultDto<ProductCategoryResultDto> { IsSuccess = false, Message = "دسته بندی یافت نشد", Status = 400 };

                if (request.ParentId.HasValue)
                {
                    if (request.ParentId.Value == request.Id)
                    {
                        return new ResultDto<ProductCategoryResultDto>
                        {
                            IsSuccess = false,
                            Message = "دسته بندی نمی‌تواند والد خودش باشد",
                            Status = 400
                        };
                    }

                    if (!_context.ProductCategories.Any(c => c.Id == request.ParentId.Value))
                    {
                        return new ResultDto<ProductCategoryResultDto>
                        {
                            IsSuccess = false,
                            Message = "دسته بندی والد یافت نشد",
                            Status = 400
                        };
                    }
                }

                category.Name = request.Name;
                category.Description = request.Description;
                category.MetaTitle = request.MetaTitle;
                category.MetaDescription = request.MetaDescription;
                category.ParentId = request.ParentId;
                category.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.ProductCategories.Update(category);
                _context.SaveChanges();

                // -------------------- Image --------------------
                if (!string.IsNullOrWhiteSpace(request.ImageFileName))
                {
                    var existingImage = _context.Images
                        .FirstOrDefault(i => i.ImageType == ImageTypeEnum.ProductCategory
                                           && i.ReferenceId == category.Id
                                           && i.ImageRole == ImageRoleEnum.Single);

                    if (existingImage == null)
                    {
                        var addResult = _imageCUDService.AddImage(new RequestAddImageDto
                        {
                            ImageUrl = request.ImageFileName,
                            ImageDescription = request.ImageDescription,
                            ReferenceId = category.Id,
                            ImageType = ImageTypeEnum.ProductCategory,
                            ImageRole = ImageRoleEnum.Single,
                        }, currentUserId);

                        if (!addResult.IsSuccess)
                        {
                            transaction.Rollback();
                            return new ResultDto<ProductCategoryResultDto>
                            {
                                IsSuccess = false,
                                Message = addResult.Message,
                                Status = addResult.Status
                            };
                        }
                    }
                    else
                    {
                        var editResult = _imageCUDService.EditImage(new RequestEditImageDto
                        {
                            Id = existingImage.Id,
                            ImageUrl = request.ImageFileName,
                            ImageDescription = request.ImageDescription,
                            ImageRole = ImageRoleEnum.Single,
                        }, currentUserId);

                        if (!editResult.IsSuccess)
                        {
                            transaction.Rollback();
                            return new ResultDto<ProductCategoryResultDto>
                            {
                                IsSuccess = false,
                                Message = editResult.Message,
                                Status = editResult.Status
                            };
                        }

                        oldImageToDelete = editResult.Data?.OldImage;
                    }
                }

                // -------------------- Slug (optional on edit) --------------------
                if (!string.IsNullOrWhiteSpace(request.SlugTitle))
                {
                    var existingSlug = _context.Slugs
                        .FirstOrDefault(s => s.Type == SlugTypeEnum.ProductCategory && s.ReferenceId == category.Id);

                    if (existingSlug == null)
                    {
                        transaction.Rollback();
                        return new ResultDto<ProductCategoryResultDto>
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
                        return new ResultDto<ProductCategoryResultDto>
                        {
                            IsSuccess = false,
                            Message = slugResult.Message,
                            Status = slugResult.Status
                        };
                    }
                }

                transaction.Commit();

                var mapped = MapToResult(category);
                mapped.OldImage = oldImageToDelete;

                return new ResultDto<ProductCategoryResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "دسته بندی با موفقیت به روز گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ProductCategoryResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove --------------------
        public ResultDto<ProductCategoryResultDto> RemoveProductCategory(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var category = _context.ProductCategories.FirstOrDefault(c => c.Id == id);
                if (category == null)
                    return new ResultDto<ProductCategoryResultDto> { IsSuccess = false, Message = "دسته بندی یافت نشد", Status = 400 };

                if (_context.ProductCategories.Any(c => c.ParentId == id))
                {
                    return new ResultDto<ProductCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = "ابتدا زیر دسته‌های این دسته بندی را حذف کنید",
                        Status = 400
                    };
                }

                _context.SetUserId(currentUserId);
                _context.ProductCategories.Remove(category);
                _context.SaveChanges();

                var slugCleanup = _slugCUDService.RemoveSlugsByReference(
                    SlugTypeEnum.ProductCategory, id, currentUserId);

                if (!slugCleanup.IsSuccess)
                    throw new Exception($"پاک کردن اسلاگ با موفقیت انجام نشد: {id}: {slugCleanup.Message}");

                // -------------------- Image cleanup --------------------
                var imageCleanup = _imageCUDService.RemoveImagesByReference(
                    ImageTypeEnum.ProductCategory, id, currentUserId);

                if (!imageCleanup.IsSuccess)
                    throw new Exception($"پاک کردن تصاویر با موفقیت انجام نشد: {id}: {imageCleanup.Message}");

                transaction.Commit();

                var mapped = MapToResult(category);
                mapped.OldImages = imageCleanup.Data?
                    .Select(i => i.OldImage)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList();

                return new ResultDto<ProductCategoryResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "دسته بندی با موفقیت حذف گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ProductCategoryResultDto>(ex, id.ToJson(), currentUserId);
            }
        }

        // -------------------- DeleteImage --------------------
        public ResultDto<ProductCategoryResultDto> RemoveProductCategoryImage(long id, long currentUserId)
        {
            try
            {
                var category = _context.ProductCategories.FirstOrDefault(c => c.Id == id);
                if (category == null)
                    return new ResultDto<ProductCategoryResultDto> { IsSuccess = false, Message = "دسته بندی یافت نشد", Status = 400 };

                var existingImage = _context.Images
                    .FirstOrDefault(i => i.ImageType == ImageTypeEnum.ProductCategory
                                       && i.ReferenceId == category.Id
                                       && i.ImageRole == ImageRoleEnum.Single);

                if (existingImage == null)
                {
                    return new ResultDto<ProductCategoryResultDto>
                    {
                        Data = MapToResult(category),
                        IsSuccess = true,
                        Message = "تصویری برای حذف وجود ندارد",
                        Status = 200
                    };
                }

                var removeResult = _imageCUDService.RemoveImage(existingImage.Id, currentUserId);
                if (!removeResult.IsSuccess)
                {
                    return new ResultDto<ProductCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = removeResult.Message,
                        Status = removeResult.Status
                    };
                }

                var mapped = MapToResult(category);
                mapped.OldImage = removeResult.Data?.OldImage;

                return new ResultDto<ProductCategoryResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تصویر با موفقیت حذف گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ProductCategoryResultDto>(ex, id.ToJson(), currentUserId);
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

        private ResultDto<T> HandleException<T>(Exception ex, string requestParam, long currentUserId)
        {
            DetachAll();

            _context.HandleExceptions.Add(new HandleException
            {
                Message = ex.InnerException?.Message ?? ex.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = currentUserId,
            });
            _context.SaveChanges();

            return new ResultDto<T>
            {
                IsSuccess = false,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                Status = 400
            };
        }

        private static ProductCategoryResultDto MapToResult(ProductCategory category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            MetaTitle = category.MetaTitle,
            MetaDescription = category.MetaDescription,
            ParentId = category.ParentId,
            IsActive = category.IsActive,
        };
    }

    // ==================== DTOs ====================
    public class RequestAddProductCategoryDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageFileName { get; set; }
        public string? ImageDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string SlugTitle { get; set; }
        public long? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RequestEditProductCategoryDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageFileName { get; set; }
        public string? ImageDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string SlugTitle { get; set; }
        public long? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductCategoryResultDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string Slug { get; set; }
        public long? ParentId { get; set; }
        public bool IsActive { get; set; }

        public string? OldImage { get; set; }
        public List<string>? OldImages { get; set; }
    }
}