using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Common;
using Common.Dto;
using Domain.Entities.Branches;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NetTopologySuite.Geometries;

namespace Application.Services.Branches.Commands
{
    public interface IBranchCUDService
    {
        // -------- Admin: direct, no approval needed --------
        ResultDto<BranchResultDto> AddBranch(RequestAddBranchDto request, long currentUserId);
        ResultDto<BranchResultDto> EditBranch(RequestEditBranchDto request, long currentUserId);
        ResultDto<BranchResultDto> RemoveBranch(long Id, long currentUserId);
        ResultDto<BranchImageDeleteResultDto> RemoveBranchImage(long imageId, long currentUserId);
        ResultDto EditBranchPrimaryImage(long branchId, long imageId, long currentUserId);

        // -------- Manager: goes through approval --------
        ResultDto<BranchResultDto> EditBranchManager(RequestEditBranchDto request, long currentUserId);
    }

    public class BranchCUDService : IBranchCUDService
    {
        private readonly IDataBaseContext _context;
        private readonly IImageCUDService _imageCUDService;

        public BranchCUDService(IDataBaseContext context, IImageCUDService imageCUDService)
        {
            _context = context;
            _imageCUDService = imageCUDService;
        }

        // -------------------- Add --------------------
        public ResultDto<BranchResultDto> AddBranch(RequestAddBranchDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var city = _context.Cities.FirstOrDefault(c => c.Id == request.CityId);
                if (city is null)
                    return new ResultDto<BranchResultDto>
                    {
                        IsSuccess = false,
                        Message = "شهر یافت نشد",
                        Status = 201
                    };

                Point? location = null;
                if (request.Longtitude != null && request.Latitude != null)
                {
                    location = BuildLocation(request.Longtitude, request.Latitude);
                }

                var Branch = new Branch
                {
                    Title = request.Title,
                    Address = request.Address,
                    PhoneNumber = request.PhoneNumber,
                    WorkHours = request.WorkHours,
                    Holidays = request.Holidays,
                    Longtitude = request.Longtitude,
                    Latitude = request.Latitude,
                    Location = location,
                    CityId = request.CityId,
                    StateId = city.StateId,
                    IsActive = request.IsActive
                };

                _context.SetUserId(currentUserId);
                _context.Branches.Add(Branch);
                _context.SaveChanges();

                // -------------------- Images --------------------
                if (request.ImageNames != null && request.ImageNames.Count > 0)
                {
                    var addImagesResult = AddBranchImages(Branch.Id, request.ImageNames, request.ImageDescriptions, currentUserId);
                    if (!addImagesResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<BranchResultDto>
                        {
                            IsSuccess = false,
                            Message = addImagesResult.Message,
                            Status = addImagesResult.Status
                        };
                    }
                }

                transaction.Commit();

                return new ResultDto<BranchResultDto>
                {
                    Data = MapToResult(Branch),
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ثبت شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<BranchResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Edit --------------------
        public ResultDto<BranchResultDto> EditBranch(RequestEditBranchDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var Branch = _context.Branches.Where(p => p.Id == request.Id).FirstOrDefault();
                if (Branch == null)
                    return new ResultDto<BranchResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                var city = _context.Cities.FirstOrDefault(c => c.Id == request.CityId);
                if (city is null)
                    return new ResultDto<BranchResultDto>
                    {
                        IsSuccess = false,
                        Message = "شهر یافت نشد",
                        Status = 201
                    };

                Point? location = null;
                if (request.Longtitude != null && request.Latitude != null)
                {
                    location = BuildLocation(request.Longtitude, request.Latitude);
                }

                Branch.Title = request.Title;
                Branch.Address = request.Address;
                Branch.PhoneNumber = request.PhoneNumber;
                Branch.WorkHours = request.WorkHours;
                Branch.Holidays = request.Holidays;
                Branch.Longtitude = request.Longtitude;
                Branch.Latitude = request.Latitude;
                Branch.CityId = request.CityId;
                Branch.StateId = city.StateId;
                Branch.IsActive = request.IsActive;
                Branch.Location = location;

                _context.SetUserId(currentUserId);
                _context.Branches.Update(Branch);
                _context.SaveChanges();

                // -------------------- Images (append only, never replace) --------------------
                if (request.ImageNames != null && request.ImageNames.Count > 0)
                {
                    var addImagesResult = AddBranchImages(Branch.Id, request.ImageNames, request.ImageDescriptions, currentUserId);
                    if (!addImagesResult.IsSuccess)
                    {
                        transaction.Rollback();
                        return new ResultDto<BranchResultDto>
                        {
                            IsSuccess = false,
                            Message = addImagesResult.Message,
                            Status = addImagesResult.Status
                        };
                    }
                }

                transaction.Commit();

                var mapped = MapToResult(Branch);

                return new ResultDto<BranchResultDto>
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
                return HandleException<BranchResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Edit (Manager) --------------------
        public ResultDto<BranchResultDto> EditBranchManager(RequestEditBranchDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var Branch = _context.Branches.FirstOrDefault(p => p.Id == request.Id);
                if (Branch == null)
                    return new ResultDto<BranchResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                if (Branch.ManagerUserId != currentUserId)
                    return new ResultDto<BranchResultDto>
                    {
                        IsSuccess = false,
                        Message = "شما اجازه ویرایش این شعبه را ندارید",
                        Status = 403
                    };

                var city = _context.Cities.FirstOrDefault(c => c.Id == request.CityId);
                if (city is null)
                    return new ResultDto<BranchResultDto>
                    {
                        IsSuccess = false,
                        Message = "شهر یافت نشد",
                        Status = 201
                    };

                Branch.Title = request.Title;
                Branch.Address = request.Address;
                Branch.PhoneNumber = request.PhoneNumber;
                Branch.WorkHours = request.WorkHours;
                Branch.Holidays = request.Holidays;
                Branch.Longtitude = request.Longtitude;
                Branch.Latitude = request.Latitude;
                Branch.CityId = request.CityId;
                Branch.StateId = city.StateId;

                Branch.IsActive = false;

                _context.SetUserId(currentUserId);
                _context.Branches.Update(Branch);
                _context.SaveChanges();

                // NOTE: request.ImageNames is intentionally ignored here. Image edits aren't
                // scalar columns so they don't fit the Changes table's model - decide separately
                // whether manager image changes apply immediately or need their own review step.

                transaction.Commit();

                var mapped = MapToResult(Branch);
                mapped.IsPendingApproval = true;

                return new ResultDto<BranchResultDto>
                {
                    Data = mapped,
                    IsSuccess = true,
                    Message = "تغییرات ثبت شد و در انتظار تایید مدیر است",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<BranchResultDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove --------------------
        public ResultDto<BranchResultDto> RemoveBranch(long Id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var Branch = _context.Branches.Where(p => p.Id == Id).FirstOrDefault();

                if (Branch == null)
                    return new ResultDto<BranchResultDto>
                    {
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 201
                    };

                Branch.IsDeleted = true;
                Branch.DeletedAt = DateTime.Now;
                _context.SetUserId(currentUserId);
                _context.Branches.Update(Branch);
                _context.SaveChanges();

                // -------------------- Image cleanup --------------------
                var imageCleanup = _imageCUDService.RemoveImagesByReference(
                    ImageTypeEnum.Branch, Id, currentUserId);

                if (!imageCleanup.IsSuccess)
                    throw new Exception($"پاک کردن تصاویر با موفقیت انجام نشد: {Id}: {imageCleanup.Message}");

                transaction.Commit();

                var mapped = MapToResult(Branch);
                mapped.OldImages = imageCleanup.Data?
                    .Select(i => i.OldImage)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList();

                return new ResultDto<BranchResultDto>
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
                return HandleException<BranchResultDto>(ex, Id.ToJson(), currentUserId);
            }
        }

        // -------------------- RemoveBranchImage --------------------
        public ResultDto<BranchImageDeleteResultDto> RemoveBranchImage(long imageId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var image = _context.Images
                    .FirstOrDefault(i => i.Id == imageId && i.ImageType == ImageTypeEnum.Branch);

                if (image == null)
                {
                    return new ResultDto<BranchImageDeleteResultDto>
                    {
                        IsSuccess = false,
                        Message = "تصویر یافت نشد",
                        Status = 404
                    };
                }

                var wasPrimary = image.ImageRole == ImageRoleEnum.Primary;
                var branchId = image.ReferenceId;

                var removeResult = _imageCUDService.RemoveImage(imageId, currentUserId);

                if (!removeResult.IsSuccess)
                {
                    transaction.Rollback();
                    return new ResultDto<BranchImageDeleteResultDto>
                    {
                        IsSuccess = false,
                        Message = removeResult.Message,
                        Status = removeResult.Status
                    };
                }

                if (wasPrimary && branchId.HasValue)
                {
                    var nextImage = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Branch && i.ReferenceId == branchId.Value)
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
                            return new ResultDto<BranchImageDeleteResultDto>
                            {
                                IsSuccess = false,
                                Message = promoteResult.Message,
                                Status = promoteResult.Status
                            };
                        }
                    }
                }

                transaction.Commit();

                return new ResultDto<BranchImageDeleteResultDto>
                {
                    Data = new BranchImageDeleteResultDto
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
                return HandleException<BranchImageDeleteResultDto>(ex, imageId.ToJson(), currentUserId);
            }
        }

        // -------------------- EditBranchPrimaryImage --------------------
        public ResultDto EditBranchPrimaryImage(long branchId, long imageId, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var images = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Branch && i.ReferenceId == branchId)
                    .ToList();

                if (!images.Any())
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "تصویری برای این شعبه یافت نشد",
                        Status = 404
                    };
                }

                var targetImage = images.FirstOrDefault(i => i.Id == imageId);

                if (targetImage == null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "تصویر یافت نشد یا متعلق به این شعبه نیست",
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

                // reuse the same generic-T helper; discard the typed Data since this endpoint returns non-generic ResultDto
                var handled = HandleException<object>(ex, new { branchId, imageId }.ToJson(), currentUserId);

                return new ResultDto
                {
                    IsSuccess = handled.IsSuccess,
                    Message = handled.Message,
                    Status = handled.Status
                };
            }
        }

        // ==================== Helpers ====================
        private ResultDto AddBranchImages(long branchId, List<string> imageNames, List<string?> imageDescriptions, long currentUserId)
        {
            var hasPrimary = _context.Images
                .Any(i => i.ImageType == ImageTypeEnum.Branch
                        && i.ReferenceId == branchId
                        && i.ImageRole == ImageRoleEnum.Primary);

            for (int i = 0; i < imageNames.Count; i++)
            {
                var role = (!hasPrimary && i == 0) ? ImageRoleEnum.Primary : ImageRoleEnum.Secondary;
                var description = imageDescriptions != null && imageDescriptions.Count > i ? imageDescriptions[i] : null;

                var imageResult = _imageCUDService.AddImage(new RequestAddImageDto
                {
                    ImageUrl = imageNames[i],
                    ImageDescription = description,
                    ReferenceId = branchId,
                    ImageType = ImageTypeEnum.Branch,
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

        private static BranchResultDto MapToResult(Branch branch) => new()
        {
            Id = branch.Id,
            Title = branch.Title,
            Address = branch.Address,
            PhoneNumber = branch.PhoneNumber,
            WorkHours = branch.WorkHours,
            Holidays = branch.Holidays,
            Longtitude = branch.Longtitude,
            Latitude = branch.Latitude,
            CityId = branch.CityId,
            StateId = branch.StateId,
            IsActive = branch.IsActive,
            ManagerUserId = branch.ManagerUserId
        };

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }

        // currentUserId is nullable here on purpose: some failure paths (or future callers)
        // may not have a reliably-known user to attribute the error to, so we log null instead
        // of guessing or throwing a second exception trying to resolve one.
        private ResultDto<T> HandleException<T>(Exception ex, string requestParam, long? currentUserId)
        {
            try
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
            }
            catch
            {
                // prevent secondary crash during logging
            }

            return new ResultDto<T>
            {
                IsSuccess = false,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                Status = 400,
            };
        }

        private static Point BuildLocation(decimal? longitude, decimal? latitude)
        {
            if (longitude is null || latitude is null)
                throw new ValidationException("موقعیت مکانی (طول و عرض جغرافیایی) الزامی است");

            return new NetTopologySuite.Geometries.Point((double)longitude.Value, (double)latitude.Value)
            {
                SRID = 4326
            };
        }
    }

    // ==================== DTOs ====================

    public class RequestAddBranchDto
    {
        public string? Title { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WorkHours { get; set; }
        public string? Holidays { get; set; }
        public List<string>? ImageNames { get; set; }
        public List<string?>? ImageDescriptions { get; set; }
        public decimal? Longtitude { get; set; }
        public decimal? Latitude { get; set; }
        public long? CityId { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestEditBranchDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WorkHours { get; set; }
        public string? Holidays { get; set; }
        public List<string>? ImageNames { get; set; }
        public List<string?>? ImageDescriptions { get; set; }
        public decimal? Longtitude { get; set; }
        public decimal? Latitude { get; set; }
        public long? CityId { get; set; }
        public bool IsActive { get; set; }
    }

    public class BranchResultDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WorkHours { get; set; }
        public string? Holidays { get; set; }
        public decimal? Longtitude { get; set; }
        public decimal? Latitude { get; set; }
        public long? CityId { get; set; }
        public long? StateId { get; set; }
        public bool IsActive { get; set; }
        public long? ManagerUserId { get; set; }
        public bool IsPendingApproval { get; set; }

        public List<string>? OldImages { get; set; }
    }

    public class BranchImageDeleteResultDto
    {
        public long Id { get; set; }
        public string? OldImageToDelete { get; set; }
    }
}