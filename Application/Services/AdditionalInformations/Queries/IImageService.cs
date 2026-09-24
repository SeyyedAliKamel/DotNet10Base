using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services.Images.Queries
{
    public interface IImageService
    {
        ResultDto<ImageItemDto> GetImageItem(long Id);
        ResultDto<List<ImageItemDto>> GetImageItemByReference(ImageTypeEnum imageType, long referenceId);
        ResultDto<List<ImageItemDto>> GetImageList(ImageTypeEnum imageType, RequestListDto request);
    }

    public class ImageService : IImageService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public ImageService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ResultDto<ImageItemDto> GetImageItem(long Id)
        {
            var image = _context.Images.FirstOrDefault(i => i.Id == Id);
            if (image == null)
                return new ResultDto<ImageItemDto>
                {
                    IsSuccess = false,
                    Message = "تصویر یافت نشد",
                    Status = 404
                };

            return new ResultDto<ImageItemDto>
            {
                Data = MapToDto(image),
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        public ResultDto<List<ImageItemDto>> GetImageItemByReference(ImageTypeEnum imageType, long referenceId)
        {
            var images = _context.Images
                .Where(i => i.ImageType == imageType && i.ReferenceId == referenceId)
                .OrderBy(i => i.ImageRole)
                .ThenBy(i => i.Id)
                .ToList()
                .Select(MapToDto)
                .ToList();

            return new ResultDto<List<ImageItemDto>>
            {
                Data = images,
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        // -------------------- GetList (paged) --------------------
        public ResultDto<List<ImageItemDto>> GetImageList(ImageTypeEnum imageType, RequestListDto request)
        {
            try
            {
                var query = _context.Images
                    .AsNoTracking()
                    .Where(x => x.ImageType == imageType)
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query
                        .OrderBy(x => x.ImageRole)
                        .ThenBy(x => x.Id);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .ToList()
                    .Select(MapToDto)
                    .ToList();

                return new ResultDto<List<ImageItemDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست تصاویر"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ImageItemDto>>(ex);
            }
        }

        // ==================== Helpers ====================
        private ImageItemDto MapToDto(Image image) => new()
        {
            Id = image.Id,
            Image = image.ImageUrl == null ? null : new ResultImageDto
            {

                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Other/" + image.ImageUrl,
                Id = image.Id

            },
            //Title = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Other/" + image.ImageUrl,
            ImageDescription = image.ImageDescription,
            ReferenceId = image.ReferenceId,
            ImageType = image.ImageType,
            ImageRole = image.ImageRole,
        };

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

    public class ImageItemDto
    {
        public long Id { get; set; }
        public ResultImageDto Image { get; set; }
        public string Title { get; set; }
        public string? ImageDescription { get; set; }
        public long? ReferenceId { get; set; }
        public ImageTypeEnum ImageType { get; set; }
        public ImageRoleEnum ImageRole { get; set; }
    }

    public class UserBasketImageItemDto
    {
        public long Id { get; set; }
        public ResultImageDto Image { get; set; }
        public string? ImageDescription { get; set; }
        public long BasketItemId { get; set; }
        public long ProductId { get; set; }
        public bool IsActive { get; set; }
    }
}