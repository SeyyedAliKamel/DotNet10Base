using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using static Common.Helper;

namespace Application.Services.AdditionalInformations.Commands
{
    // ==================== Interface ====================
    public interface IBannerCUDService
    {
        ResultDto<BannerResultDto> AddBanner(RequestAddBannerDto request, long currentUserId);
        ResultDto<BannerResultDto> EditBanner(RequestEditBannerDto request, long currentUserId);
        ResultDto<BannerResultDto> RemoveBanner(int id, long currentUserId);
        ResultDto<BannerResultDto> RemoveBannerImage(int id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class BannerCUDService : IBannerCUDService
    {
        private readonly IDataBaseContext _context;

        public BannerCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto<BannerResultDto> AddBanner(RequestAddBannerDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var banner = new Banner
                {
                    Title = request.Title,
                    AdminTitle = request.AdminTitle,
                    ImageUrl = request.ImageUrl,
                    Url = request.Url,
                    Position = request.Position,
                    IsActive = request.IsActive
                };

                _context.SetUserId(currentUserId);
                _context.Banners.Add(banner);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto<BannerResultDto>
                {
                    Data = MapToResult(banner),
                    IsSuccess = true,
                    Message = "اطلاعات بنر با موفقیت ثبت شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<BannerResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Update --------------------
        public ResultDto<BannerResultDto> EditBanner(RequestEditBannerDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.Banners.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto<BannerResultDto>
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "اطلاعات اشتباه است"
                    };

                // Capture the previous image before overwriting so the caller can
                // delete the old physical file when it's actually being replaced.
                string oldImageToDelete = null;
                var isImageChanging = !string.IsNullOrWhiteSpace(request.ImageUrl)
                    && !string.Equals(data.ImageUrl, request.ImageUrl, StringComparison.OrdinalIgnoreCase);

                if (isImageChanging && !string.IsNullOrWhiteSpace(data.ImageUrl))
                    oldImageToDelete = data.ImageUrl;

                data.Title = request.Title;
                data.AdminTitle = request.AdminTitle;
                if(request.ImageUrl != null)
                    data.ImageUrl = request.ImageUrl;
                data.Url = request.Url;
                data.Position = request.Position;
                data.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.Banners.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                var mapped = MapToResult(data);
                mapped.OldImage = oldImageToDelete;

                return new ResultDto<BannerResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Status = 200,
                    Message = "بنر با موفقیت ویرایش شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<BannerResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Delete --------------------
        public ResultDto<BannerResultDto> RemoveBanner(int id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.Banners.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto<BannerResultDto>
                    {
                        IsSuccess = false,
                        Message = "بنر یافت نشد",
                        Status = 200,
                    };

                var result = MapToResult(existing);
                result.OldImage = existing.ImageUrl;

                _context.SetUserId(currentUserId);
                _context.Banners.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto<BannerResultDto>
                {
                    Data = result,
                    IsSuccess = true,
                    Message = "بنر با موفقیت حذف شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<BannerResultDto>(ex, id.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove Image Only --------------------
        public ResultDto<BannerResultDto> RemoveBannerImage(int id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.Banners.FirstOrDefault(x => x.Id == id);

                if (data is null)
                    return new ResultDto<BannerResultDto>
                    {
                        IsSuccess = false,
                        Message = "بنر یافت نشد",
                        Status = 200,
                    };

                if (string.IsNullOrWhiteSpace(data.ImageUrl))
                {
                    transaction.Commit();
                    return new ResultDto<BannerResultDto>
                    {
                        Data = MapToResult(data),
                        IsSuccess = true,
                        Message = "تصویری برای حذف وجود ندارد",
                        Status = 200
                    };
                }

                var oldImage = data.ImageUrl;
                data.ImageUrl = null;

                _context.SetUserId(currentUserId);
                _context.Banners.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                var mapped = MapToResult(data);
                mapped.OldImage = oldImage;

                return new ResultDto<BannerResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تصویر با موفقیت حذف گردید",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<BannerResultDto>(ex, id.ToJson(), currentUserId);
            }
        }

        // ==================== Helpers ====================
        private static BannerResultDto MapToResult(Banner banner) => new()
        {
            Id = banner.Id,
            Title = banner.Title,
            AdminTitle = banner.AdminTitle,
            ImageUrl = banner.ImageUrl,
            Url = banner.Url,
            Position = banner.Position,
            IsActive = banner.IsActive
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

    public class RequestAddBannerDto
    {
        [Required(ErrorMessage = "تیتر الزامی است.")]
        public string Title { get; set; }
        public string? AdminTitle { get; set; }
        public string? ImageUrl { get; set; }
        public string? Url { get; set; }
        public BannerPositionEnum Position { get; set; } = BannerPositionEnum.Slider;
        public bool IsActive { get; set; }
    }

    public class RequestEditBannerDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string AdminTitle { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public BannerPositionEnum Position { get; set; }
        public bool IsActive { get; set; }
    }

    public class BannerResultDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string AdminTitle { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public BannerPositionEnum Position { get; set; }
        public bool IsActive { get; set; }

        public string? OldImage { get; set; }
    }
}