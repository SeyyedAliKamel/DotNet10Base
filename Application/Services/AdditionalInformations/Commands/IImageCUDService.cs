using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Baskets;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Application.Services.AdditionalInformations.Commands
{
    public interface IImageCUDService
    {
        ResultDto<ImageResultDto> AddImage(RequestAddImageDto request, long currentUserId);
        ResultDto<ImageResultDto> EditImage(RequestEditImageDto request, long currentUserId);
        ResultDto<ImageResultDto> RemoveImage(long Id, long currentUserId);
        ResultDto<List<ImageResultDto>> RemoveImagesByReference(ImageTypeEnum imageType, long referenceId, long currentUserId);
    }

    public class ImageCUDService : IImageCUDService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;


        public ImageCUDService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ResultDto<ImageResultDto> AddImage(RequestAddImageDto request, long currentUserId)
        {
            try
            {
                var image = new Image
                {
                    
                    ImageUrl = request.ImageUrl,
                    ImageDescription = request.ImageDescription,
                    ReferenceId = request.ReferenceId,
                    ImageType = request.ImageType,
                    ImageRole = request.ImageRole,
                };

                _context.SetUserId(currentUserId);
                _context.Images.Add(image);
                _context.SaveChanges();
                
                return new ResultDto<ImageResultDto>
                {
                    Data = MapToResult(image),
                    IsSuccess = true,
                    Message = "تصویر با موفقیت ثبت شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                return HandleException<ImageResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        public ResultDto<ImageResultDto> EditImage(RequestEditImageDto request, long currentUserId)
        {
            try
            {
                var image = _context.Images.FirstOrDefault(i => i.Id == request.Id);
                if (image == null)
                    return new ResultDto<ImageResultDto>
                    {
                        IsSuccess = false,
                        Message = "تصویر یافت نشد",
                        Status = 201
                    };

                string oldUrlToDelete = null;
                if (!string.IsNullOrWhiteSpace(request.ImageUrl) && image.ImageUrl != request.ImageUrl)
                {
                    oldUrlToDelete = image.ImageUrl;
                    image.ImageUrl = request.ImageUrl;
                }

                var isBeingActivated = image.ImageType == ImageTypeEnum.BasketItem
                    && !image.IsActive
                    && request.IsActive;

                image.ImageDescription = request.ImageDescription;
                image.ImageRole = request.ImageRole;
                image.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.Images.Update(image);
                _context.SaveChanges();

                var mapped = MapToResult(image);
                mapped.OldImage = oldUrlToDelete;

                return new ResultDto<ImageResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تصویر ویرایش شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                return HandleException<ImageResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        public ResultDto<ImageResultDto> RemoveImage(long Id, long currentUserId)
        {
            try
            {
                var image = _context.Images.FirstOrDefault(i => i.Id == Id);
                if (image == null)
                    return new ResultDto<ImageResultDto>
                    {
                        IsSuccess = false,
                        Message = "تصویر یافت نشد",
                        Status = 201
                    };

                var mapped = MapToResult(image);
                mapped.OldImage = image.ImageUrl;

                _context.SetUserId(currentUserId);
                _context.Images.Remove(image);
                _context.SaveChanges();

                return new ResultDto<ImageResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تصویر حذف شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                return HandleException<ImageResultDto>(ex, Id.ToJson(), currentUserId);
            }
        }

        public ResultDto<List<ImageResultDto>> RemoveImagesByReference(ImageTypeEnum imageType, long referenceId, long currentUserId)
        {
            try
            {
                var images = _context.Images
                    .Where(i => i.ImageType == imageType && i.ReferenceId == referenceId)
                    .ToList();

                var mapped = images.Select(i =>
                {
                    var m = MapToResult(i);
                    m.OldImage = i.ImageUrl;
                    return m;
                }).ToList();

                _context.SetUserId(currentUserId);
                _context.Images.RemoveRange(images);
                _context.SaveChanges();

                return new ResultDto<List<ImageResultDto>>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تصاویر حذف شدند",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ImageResultDto>>(ex, new { imageType, referenceId }.ToJson(), currentUserId);
            }
        }

        // ==================== Helpers ====================
        public ImageResultDto MapToResult(Image image) => new()
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            ImageDescription = image.ImageDescription,
            ReferenceId = image.ReferenceId,
            ImageType = image.ImageType,
            ImageRole = image.ImageRole,
            Image = image.ImageUrl == null ? null : new ResultImageDto
            {

                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Other/" + image.ImageUrl,
                Id = image.Id

            },


        };

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
                entry.State = EntityState.Detached;
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

    public class RequestAddImageDto
    {
        [Required(ErrorMessage = "در ثبت عکس مشکلی پیش آمد")]
        public string ImageUrl { get; set; }
        public string? ImageDescription { get; set; }
        public long? ReferenceId { get; set; }
        public ImageTypeEnum ImageType { get; set; }
        public ImageRoleEnum ImageRole { get; set; }
    }

    public class RequestEditImageDto
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; }
        public string? ImageDescription { get; set; }
        public ImageRoleEnum ImageRole { get; set; }
        public bool IsActive { get; set; }
    }

    public class ImageResultDto
    {
        public long Id { get; set; }
        public ResultImageDto Image { get; set; }
        public string ImageUrl { get; set; }
        public string? ImageDescription { get; set; }
        public long? ReferenceId { get; set; }
        public ImageTypeEnum ImageType { get; set; }
        public ImageRoleEnum ImageRole { get; set; }
        public string? OldImage { get; set; }
    }

    public class RequestUploadBasketItemImagesDto
    {
        [Required(ErrorMessage = "آیتم سبد خرید مشخص نشده است")]
        public long BasketItemId { get; set; }

        [Required(ErrorMessage = "تصویری ارسال نشده است")]
        public List<string> ImageNames { get; set; }

        public List<string?> ImageDescriptions { get; set; }
    }
}