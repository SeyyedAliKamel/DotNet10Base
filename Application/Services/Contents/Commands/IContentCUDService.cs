using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.Products.Commands;
using Common;
using Common.Dto;
using Domain.Entities.Contents;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Application.Services.Contents.Commands
{
    public interface IContentCUDService
    {
        ResultDto<ContentResultDto> AddContent(RequestAddContentDto request, long currentUserId);
        ResultDto<ContentResultDto> EditContent(RequestEditContentDto request, long currentUserId);
        ResultDto<ContentResultDto> RemoveContent(long Id, long currentUserId);
        ResultDto<ContentResultDto> RemoveContentImage(long Id, long currentUserId);
    }

    public class ContentCUDService : IContentCUDService
    {
        private readonly IDataBaseContext _context;
        private readonly ISlugCUDService _slugCUDService;
        private readonly IImageCUDService _imageCUDService;

        public ContentCUDService(IDataBaseContext context, ISlugCUDService slugCUDService, IImageCUDService imageCUDService)
        {
            _context = context;
            _slugCUDService = slugCUDService;
            _imageCUDService = imageCUDService;
        }

        // -------------------- Add --------------------
        public ResultDto<ContentResultDto> AddContent(RequestAddContentDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                if (string.IsNullOrWhiteSpace(request.SlugTitle))
                {
                    return new ResultDto<ContentResultDto>
                    {
                        IsSuccess = false,
                        Message = "عنوان اسلاگ الزامی است",
                        Status = 400
                    };
                }

                if (!TryParseDate(request.ReleaseDate, out var releaseDate))
                {
                    return new ResultDto<ContentResultDto>
                    {
                        IsSuccess = false,
                        Message = $"فرمت تاریخ انتشار صحیح نیست. فرمت مورد نیاز: {DateFormat}",
                        Status = 400
                    };
                }

                var content = new Content
                {
                    Description = request.Description,
                    Name = request.Name,
                    ShortDescription = request.ShortDescription,
                    MetaTitle = request.MetaTitle,
                    ContentCategoryId = request.ContentCategoryId,
                    MetaDescription = request.MetaDescription,
                    IsActive = request.IsActive,
                    ReleaseDate = releaseDate ?? DateTime.Now,
                    CreatedById = currentUserId
                };

                _context.SetUserId(currentUserId);
                _context.Contents.Add(content);
                _context.SaveChanges();

                // -------------------- Slug --------------------
                var slugResult = _slugCUDService.AddSlug(new RequestAddSlugDto
                {
                    Title = request.SlugTitle,
                    Type = SlugTypeEnum.Content,
                    ReferenceId = content.Id
                }, currentUserId);

                if (!slugResult.IsSuccess)
                {
                    transaction.Rollback();
                    return new ResultDto<ContentResultDto>
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
                        ReferenceId = content.Id,
                        ImageType = ImageTypeEnum.Content,
                        ImageRole = ImageRoleEnum.Single,
                    }, currentUserId);

                    if (!imageResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<ContentResultDto>
                        {
                            IsSuccess = false,
                            Message = imageResult.Message,
                            Status = imageResult.Status
                        };
                    }
                }

                // -------------------- Related contents --------------------
                if (request.ContentRelationIds != null && request.ContentRelationIds.Any())
                {
                    var relations = request.ContentRelationIds
                        .Distinct()
                        .Where(relatedId => relatedId != content.Id)
                        .Select(relatedId => new ContentRelation
                        {
                            ContentId = content.Id,
                            RelatedContentId = relatedId
                        })
                        .ToList();
                    _context.ContentRelations.AddRange(relations);
                    _context.SaveChanges();
                }

                transaction.Commit();

                content.Order = content.Id;
                _context.SaveChanges();

                return new ResultDto<ContentResultDto>
                {
                    Data = MapToResult(content),
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ثبت شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ContentResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Edit --------------------
        public ResultDto<ContentResultDto> EditContent(RequestEditContentDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            string oldImageToDelete = null;

            try
            {
                var Content = _context.Contents.Where(p => p.Id == request.Id).FirstOrDefault();
                if (Content == null)
                    return new ResultDto<ContentResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                if (!TryParseDate(request.ReleaseDate, out var releaseDate))
                {
                    return new ResultDto<ContentResultDto>
                    {
                        IsSuccess = false,
                        Message = $"فرمت تاریخ انتشار صحیح نیست. فرمت مورد نیاز: {DateFormat}",
                        Status = 400
                    };
                }

                Content.Name = request.Name;
                Content.ShortDescription = request.ShortDescription;
                Content.Description = request.Description;
                Content.MetaTitle = request.MetaTitle;
                Content.ContentCategoryId = request.ContentCategoryId;
                Content.MetaDescription = request.MetaDescription;
                Content.IsActive = request.IsActive;
                Content.ReleaseDate = releaseDate ?? DateTime.Now;
                Content.UpdatedById = currentUserId;

                _context.SetUserId(currentUserId);
                _context.Contents.Update(Content);
                _context.SaveChanges();

                // -------------------- Image --------------------
                if (!string.IsNullOrWhiteSpace(request.ImageFileName))
                {
                    var existingImage = _context.Images
                        .FirstOrDefault(i => i.ImageType == ImageTypeEnum.Content
                                           && i.ReferenceId == Content.Id
                                           && i.ImageRole == ImageRoleEnum.Single);

                    if (existingImage == null)
                    {
                        var addResult = _imageCUDService.AddImage(new RequestAddImageDto
                        {
                            ImageUrl = request.ImageFileName,
                            ImageDescription = request.ImageDescription,
                            ReferenceId = Content.Id,
                            ImageType = ImageTypeEnum.Content,
                            ImageRole = ImageRoleEnum.Single,
                        }, currentUserId);

                        if (!addResult.IsSuccess)
                        {
                            transaction.Rollback();
                            return new ResultDto<ContentResultDto>
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
                            return new ResultDto<ContentResultDto>
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
                        .FirstOrDefault(s => s.Type == SlugTypeEnum.Content && s.ReferenceId == Content.Id);

                    if (existingSlug == null)
                    {
                        transaction.Rollback();
                        return new ResultDto<ContentResultDto>
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
                        return new ResultDto<ContentResultDto>
                        {
                            IsSuccess = false,
                            Message = slugResult.Message,
                            Status = slugResult.Status
                        };
                    }
                }

                // -------------------- Replace related contents --------------------
                if (request.ContentRelationIds != null && request.ContentRelationIds.Any())
                {
                    var existingRelations = _context.ContentRelations
                        .Where(r => r.ContentId == Content.Id)
                        .ToList();
                    _context.ContentRelations.RemoveRange(existingRelations);

                    var relations = request.ContentRelationIds
                        .Distinct()
                        .Where(relatedId => relatedId != Content.Id)
                        .Select(relatedId => new ContentRelation
                        {
                            ContentId = Content.Id,
                            RelatedContentId = relatedId
                        })
                        .ToList();

                    _context.ContentRelations.AddRange(relations);
                    _context.SaveChanges();
                }

                transaction.Commit();

                var mapped = MapToResult(Content);
                mapped.OldImage = oldImageToDelete;

                return new ResultDto<ContentResultDto>
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
                return HandleException<ContentResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove --------------------
        public ResultDto<ContentResultDto> RemoveContent(long Id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var Content = _context.Contents.Where(p => p.Id == Id).FirstOrDefault();
                if (Content == null)
                    return new ResultDto<ContentResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                Content.IsDeleted = true;
                Content.DeletedAt = DateTime.Now;
                _context.SetUserId(currentUserId);
                _context.Contents.Update(Content);
                _context.SaveChanges();

                var slugCleanup = _slugCUDService.RemoveSlugsByReference(
                    SlugTypeEnum.Content, Id, currentUserId);

                if (!slugCleanup.IsSuccess)
                    throw new Exception($"پاک کردن اسلاگ با موفقیت انجام نشد: {Id}: {slugCleanup.Message}");

                // -------------------- Image cleanup --------------------
                var imageCleanup = _imageCUDService.RemoveImagesByReference(
                    ImageTypeEnum.Content, Id, currentUserId);

                if (!imageCleanup.IsSuccess)
                    throw new Exception($"پاک کردن تصاویر با موفقیت انجام نشد: {Id}: {imageCleanup.Message}");

                transaction.Commit();

                var mapped = MapToResult(Content);
                mapped.OldImages = imageCleanup.Data?
                    .Select(i => i.OldImage)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList();

                return new ResultDto<ContentResultDto>
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
                return HandleException<ContentResultDto>(ex, Id.ToJson(), currentUserId);
            }
        }

        // -------------------- DeleteImage --------------------
        public ResultDto<ContentResultDto> RemoveContentImage(long Id, long currentUserId)
        {
            try
            {
                var Content = _context.Contents.Where(p => p.Id == Id).FirstOrDefault();
                if (Content == null)
                    return new ResultDto<ContentResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                var existingImage = _context.Images
                    .FirstOrDefault(i => i.ImageType == ImageTypeEnum.Content
                                       && i.ReferenceId == Content.Id
                                       && i.ImageRole == ImageRoleEnum.Single);

                if (existingImage == null)
                {
                    return new ResultDto<ContentResultDto>
                    {
                        Data = MapToResult(Content),
                        IsSuccess = true,
                        Message = "تصویری برای حذف وجود ندارد",
                        Status = 200
                    };
                }

                var removeResult = _imageCUDService.RemoveImage(existingImage.Id, currentUserId);
                if (!removeResult.IsSuccess)
                {
                    return new ResultDto<ContentResultDto>
                    {
                        IsSuccess = false,
                        Message = removeResult.Message,
                        Status = removeResult.Status
                    };
                }

                var mapped = MapToResult(Content);
                mapped.OldImage = removeResult.Data?.OldImage;

                return new ResultDto<ContentResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تصویر با موفقیت حذف گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ContentResultDto>(ex, Id.ToJson(), currentUserId);
            }
        }

        // ==================== Helpers ====================
        private static ContentResultDto MapToResult(Content content) => new()
        {
            Id = content.Id,
            Name = content.Name,
            ShortDescription = content.ShortDescription,
            MetaTitle = content.MetaTitle,
            Description = content.Description,
            ContentCategoryId = content.ContentCategoryId,
            MetaDescription = content.MetaDescription,
            ReleaseDate = content.ReleaseDate,
            IsActive = content.IsActive,
        };

        private static ContentRelationResultDto MapToResult(ContentRelation relation) => new()
        {
            Id = relation.Id,
            ContentId = relation.ContentId,
            RelatedContentId = relation.RelatedContentId
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

        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

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
    }

    // ==================== DTOs ====================

    public class RequestAddContentDto
    {
        public string Name { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? ImageFileName { get; set; }
        public string? ImageDescription { get; set; }
        public long ContentCategoryId { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string SlugTitle { get; set; }
        public List<long> ContentRelationIds { get; set; } = new List<long>();
        public string? ReleaseDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestEditContentDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string? ImageFileName { get; set; }
        public string ImageDescription { get; set; }
        public long ContentCategoryId { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string SlugTitle { get; set; }
        public List<long> ContentRelationIds { get; set; } = new List<long>();
        public string? ReleaseDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestAddContentRelationDto
    {
        public long ContentId { get; set; }
        public long ContentRelationIds { get; set; }
    }

    public class RequestEditContentRelationDto
    {
        public long Id { get; set; }
        public long ContentId { get; set; }
        public long ContentRelationIds { get; set; }
    }

    public class ContentRelationResultDto
    {
        public long Id { get; set; }
        public long ContentId { get; set; }
        public long RelatedContentId { get; set; }
    }

    public class ContentResultDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public long ContentCategoryId { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public DateTime ReleaseDate { get; set; }
        public bool IsActive { get; set; }

        public string? OldImage { get; set; }
        public List<string>? OldImages { get; set; }
    }
}