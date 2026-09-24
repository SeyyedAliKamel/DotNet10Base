using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.Products.Commands;
using Common;
using Common.Dto;
using Domain.Entities.Contents;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contents.Commands
{
    public interface IContentCategoryCUDService
    {
        ResultDto<ContentCategoryResultDto> AddContentCategory(RequestAddContentCategoryDto request, long currentUserId);
        ResultDto<ContentCategoryResultDto> EditContentCategory(RequestEditContentCategoryDto request, long currentUserId);
        ResultDto<ContentCategoryResultDto> RemoveContentCategory(long Id, long currentUserId);
        ResultDto<ContentCategoryResultDto> RemoveContentCategoryImage(long Id, long currentUserId);
    }

    public class ContentCategoryCUDService : IContentCategoryCUDService
    {
        private readonly IDataBaseContext _context;
        private readonly ISlugCUDService _slugCUDService;
        private readonly IImageCUDService _imageCUDService;

        public ContentCategoryCUDService(IDataBaseContext context, ISlugCUDService slugCUDService, IImageCUDService imageCUDService)
        {
            _context = context;
            _slugCUDService = slugCUDService;
            _imageCUDService = imageCUDService;
        }

        // -------------------- Create --------------------
        public ResultDto<ContentCategoryResultDto> AddContentCategory(RequestAddContentCategoryDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                if (string.IsNullOrWhiteSpace(request.SlugTitle))
                {
                    return new ResultDto<ContentCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = "عنوان اسلاگ الزامی است",
                        Status = 400
                    };
                }

                var contentCategory = new ContentCategory
                {
                    Name = request.Name,
                    Description = request.Description,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    IsActive = request.IsActive,
                };

                _context.SetUserId(currentUserId);
                _context.ContentCategories.Add(contentCategory);
                _context.SaveChanges();

                // -------------------- Slug --------------------
                var slugResult = _slugCUDService.AddSlug(new RequestAddSlugDto
                {
                    Title = request.SlugTitle,
                    Type = SlugTypeEnum.ContentCategory,
                    ReferenceId = contentCategory.Id
                }, currentUserId);

                if (!slugResult.IsSuccess)
                {
                    transaction.Rollback();
                    return new ResultDto<ContentCategoryResultDto>
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
                        ReferenceId = contentCategory.Id,
                        ImageType = ImageTypeEnum.ContentCategory,
                        ImageRole = ImageRoleEnum.Single,
                    }, currentUserId);

                    if (!imageResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<ContentCategoryResultDto>
                        {
                            IsSuccess = false,
                            Message = imageResult.Message,
                            Status = imageResult.Status
                        };
                    }
                }

                transaction.Commit();

                contentCategory.Order = contentCategory.Id;
                _context.SaveChanges();

                return new ResultDto<ContentCategoryResultDto>
                {
                    Data = MapToResult(contentCategory),
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ثبت شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ContentCategoryResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Update --------------------
        public ResultDto<ContentCategoryResultDto> EditContentCategory(RequestEditContentCategoryDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            string oldImageToDelete = null;

            try
            {
                var contentCategory = _context.ContentCategories.FirstOrDefault(p => p.Id == request.Id);
                if (contentCategory == null)
                    return new ResultDto<ContentCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                contentCategory.Name = request.Name;
                contentCategory.Description = request.Description;
                contentCategory.MetaTitle = request.MetaTitle;
                contentCategory.MetaDescription = request.MetaDescription;
                contentCategory.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.ContentCategories.Update(contentCategory);
                _context.SaveChanges();

                // -------------------- Image --------------------
                if (!string.IsNullOrWhiteSpace(request.ImageFileName))
                {
                    var existingImage = _context.Images
                        .FirstOrDefault(i => i.ImageType == ImageTypeEnum.ContentCategory
                                           && i.ReferenceId == contentCategory.Id
                                           && i.ImageRole == ImageRoleEnum.Single);

                    if (existingImage == null)
                    {
                        var addResult = _imageCUDService.AddImage(new RequestAddImageDto
                        {
                            ImageUrl = request.ImageFileName,
                            ImageDescription = request.ImageDescription,
                            ReferenceId = contentCategory.Id,
                            ImageType = ImageTypeEnum.ContentCategory,
                            ImageRole = ImageRoleEnum.Single,
                        }, currentUserId);

                        if (!addResult.IsSuccess)
                        {
                            transaction.Rollback();
                            return new ResultDto<ContentCategoryResultDto>
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
                            return new ResultDto<ContentCategoryResultDto>
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
                        .FirstOrDefault(s => s.Type == SlugTypeEnum.ContentCategory && s.ReferenceId == contentCategory.Id);

                    if (existingSlug == null)
                    {
                        transaction.Rollback();
                        return new ResultDto<ContentCategoryResultDto>
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
                        return new ResultDto<ContentCategoryResultDto>
                        {
                            IsSuccess = false,
                            Message = slugResult.Message,
                            Status = slugResult.Status
                        };
                    }
                }

                transaction.Commit();

                var mapped = MapToResult(contentCategory);
                mapped.OldImage = oldImageToDelete;

                return new ResultDto<ContentCategoryResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "اطلاعات ثبت گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ContentCategoryResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove --------------------
        public ResultDto<ContentCategoryResultDto> RemoveContentCategory(long Id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var contentCategory = _context.ContentCategories.FirstOrDefault(p => p.Id == Id);
                if (contentCategory == null)
                    return new ResultDto<ContentCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                _context.SetUserId(currentUserId);
                _context.ContentCategories.Remove(contentCategory);
                _context.SaveChanges();

                var slugCleanup = _slugCUDService.RemoveSlugsByReference(
                    SlugTypeEnum.ContentCategory, Id, currentUserId);

                if (!slugCleanup.IsSuccess)
                    throw new Exception($"پاک کردن اسلاگ با موفقیت انجام نشد: {Id}: {slugCleanup.Message}");

                // -------------------- Image cleanup --------------------
                var imageCleanup = _imageCUDService.RemoveImagesByReference(
                    ImageTypeEnum.ContentCategory, Id, currentUserId);

                if (!imageCleanup.IsSuccess)
                    throw new Exception($"پاک کردن تصاویر با موفقیت انجام نشد: {Id}: {imageCleanup.Message}");

                transaction.Commit();

                var mapped = MapToResult(contentCategory);
                mapped.OldImages = imageCleanup.Data?
                    .Select(i => i.OldImage)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList();

                return new ResultDto<ContentCategoryResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت حذف گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ContentCategoryResultDto>(ex, Id.ToJson(), currentUserId);
            }
        }

        // -------------------- DeleteImage --------------------
        public ResultDto<ContentCategoryResultDto> RemoveContentCategoryImage(long Id, long currentUserId)
        {
            try
            {
                var contentCategory = _context.ContentCategories.FirstOrDefault(p => p.Id == Id);
                if (contentCategory == null)
                    return new ResultDto<ContentCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                var existingImage = _context.Images
                    .FirstOrDefault(i => i.ImageType == ImageTypeEnum.ContentCategory
                                       && i.ReferenceId == contentCategory.Id
                                       && i.ImageRole == ImageRoleEnum.Single);

                if (existingImage == null)
                {
                    return new ResultDto<ContentCategoryResultDto>
                    {
                        Data = MapToResult(contentCategory),
                        IsSuccess = true,
                        Message = "تصویری برای حذف وجود ندارد",
                        Status = 200
                    };
                }

                var removeResult = _imageCUDService.RemoveImage(existingImage.Id, currentUserId);
                if (!removeResult.IsSuccess)
                {
                    return new ResultDto<ContentCategoryResultDto>
                    {
                        IsSuccess = false,
                        Message = removeResult.Message,
                        Status = removeResult.Status
                    };
                }

                var mapped = MapToResult(contentCategory);
                mapped.OldImage = removeResult.Data?.OldImage;

                return new ResultDto<ContentCategoryResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تصویر با موفقیت حذف گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ContentCategoryResultDto>(ex, Id.ToJson(), currentUserId);
            }
        }

        // ==================== Helpers ====================
        private static ContentCategoryResultDto MapToResult(ContentCategory contentCategory) => new()
        {
            Id = contentCategory.Id,
            Name = contentCategory.Name,
            Description = contentCategory.Description,
            MetaTitle = contentCategory.MetaTitle,
            MetaDescription = contentCategory.MetaDescription,
            IsActive = contentCategory.IsActive,
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
    }

    // ==================== DTOs ====================

    public class RequestAddContentCategoryDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageFileName { get; set; }
        public string? ImageDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string SlugTitle { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestEditContentCategoryDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageFileName { get; set; }
        public string? ImageDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string? SlugTitle { get; set; }
        public bool IsActive { get; set; }
    }

    public class ContentCategoryResultDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public bool IsActive { get; set; }

        public string? OldImage { get; set; }
        public List<string>? OldImages { get; set; }
    }
}