using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.Branches;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static Application.Services.Branches.Queries.BranchService;

namespace Application.Services.Branches.Queries
{
    public interface IBranchService
    {
        ResultDto<ResultBranchItemDto> GetBranchItem(long Id);
        ResultDto<List<ResultBranchListDto>> GetBranchList(RequestBranchListDto request);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<ResultBranchListDto>> GetBranchListFrontEnd(RequestBranchListDto request);
        ResultDto<ResultBranchItemDto> GetBranchItemFrontEnd(long Id);

        // -------- Near me --------
        ResultDto<ResultBranchNearMeDto> GetBranchNearMeList(long? CityId, decimal? Long, decimal? Lati);
    }

    public class BranchService : IBranchService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public BranchService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ResultDto<ResultBranchItemDto> GetBranchItem(long Id)
        {
            try
            {
                var item = _context.Branches.Where(p => p.Id == Id).Select(p => new ResultBranchItemDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Address = p.Address,
                    PhoneNumber = p.PhoneNumber,
                    WorkHours = p.WorkHours,
                    Holidays = p.Holidays,
                    Longtitude = p.Longtitude,
                    Latitude = p.Latitude,
                    CityId = p.CityId,
                    CityName = p.City == null ? null : p.City.Name,
                    StateId = p.StateId,
                    StateName = p.State == null ? null : p.State.Name,
                    Images = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Branch && i.ReferenceId == p.Id)
                        .OrderByDescending(i => i.ImageRole == ImageRoleEnum.Primary)
                        .ThenBy(i => i.Id)
                        .Select(i => new ResultImageDto
                        {
                            Id = i.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Branch/" + i.ImageUrl,
                        })
                        .ToList(),
                    PrimaryImage = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Branch
                                 && i.ReferenceId == p.Id
                                 && i.ImageRole == ImageRoleEnum.Primary)
                        .Select(i => new ResultImageDto
                        {
                            Id = i.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Branch/" + i.ImageUrl,
                        })
                        .FirstOrDefault(),
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,

                }).FirstOrDefault();

                if (item == null)
                {
                    return new ResultDto<ResultBranchItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 404
                    };
                }

                return new ResultDto<ResultBranchItemDto>
                {
                    Data = item,
                    IsSuccess = true,
                    Message = "اطلاعات ارسال شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultBranchItemDto>(ex);
            }
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ResultBranchItemDto> GetBranchItemFrontEnd(long Id)
        {
            try
            {
                var item = _context.Branches.Where(p => p.Id == Id && p.IsActive).Select(p => new ResultBranchItemDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Address = p.Address,
                    PhoneNumber = p.PhoneNumber,
                    WorkHours = p.WorkHours,
                    Holidays = p.Holidays,
                    Longtitude = p.Longtitude,
                    Latitude = p.Latitude,
                    CityId = p.CityId,
                    CityName = p.City == null ? null : p.City.Name,
                    StateId = p.StateId,
                    StateName = p.State == null ? null : p.State.Name,
                    Images = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Branch && i.ReferenceId == p.Id)
                        .OrderByDescending(i => i.ImageRole == ImageRoleEnum.Primary)
                        .ThenBy(i => i.Id)
                        .Select(i => new ResultImageDto
                        {
                            Id = i.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Branch/" + i.ImageUrl,
                        })
                        .ToList(),
                    PrimaryImage = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Branch
                                 && i.ReferenceId == p.Id
                                 && i.ImageRole == ImageRoleEnum.Primary)
                        .Select(i => new ResultImageDto
                        {
                            Id = i.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Branch/" + i.ImageUrl,
                        })
                        .FirstOrDefault(),
                    IsActive = p.IsActive

                }).FirstOrDefault();

                if (item == null)
                {
                    return new ResultDto<ResultBranchItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Message = "اطلاعات یافت نشد",
                        Status = 404
                    };
                }

                return new ResultDto<ResultBranchItemDto>
                {
                    Data = item,
                    IsSuccess = true,
                    Message = "اطلاعات ارسال شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultBranchItemDto>(ex);
            }
        }

        public ResultDto<List<ResultBranchListDto>> GetBranchList(RequestBranchListDto request)
        {
            try
            {
                var Branches = _context.Branches.AsQueryable();

                Branches = Branches.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    Branches = Branches.OrderByDescending(s => s.CreatedAt);
                }

                var rowsCount = 0;
                var branchList = Branches
                    .ToPaged(request.Page, request.PageSize, out rowsCount)
                    .Select(p => new ResultBranchListDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Address = p.Address,
                        PhoneNumber = p.PhoneNumber,
                        WorkHours = p.WorkHours,
                        Holidays = p.Holidays,
                        Longtitude = p.Longtitude,
                        Latitude = p.Latitude,
                        CityId = p.CityId,
                        CityName = p.City == null ? null : p.City.Name,
                        StateId = p.StateId,
                        StateName = p.State == null ? null : p.State.Name,
                        PrimaryImage = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Branch
                                     && i.ReferenceId == p.Id
                                     && i.ImageRole == ImageRoleEnum.Primary)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Branch/" + i.ImageUrl,
                            })
                            .FirstOrDefault(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ResultBranchListDto>>
                {
                    Data = branchList,
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ارسال گردید.",
                    Status = 200,
                    Result = new ResultListDto()
                    {
                        Current = request.Page,
                        Count = rowsCount
                    },
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultBranchListDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<ResultBranchListDto>> GetBranchListFrontEnd(RequestBranchListDto request)
        {
            try
            {
                var Branches = _context.Branches.Where(x => x.IsActive).AsQueryable();

                Branches = Branches.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    Branches = Branches.OrderByDescending(s => s.CreatedAt);
                }

                var rowsCount = 0;
                var branchList = Branches
                    .ToPaged(request.Page, request.PageSize, out rowsCount)
                    .Select(p => new ResultBranchListDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Address = p.Address,
                        PhoneNumber = p.PhoneNumber,
                        WorkHours = p.WorkHours,
                        Holidays = p.Holidays,
                        Longtitude = p.Longtitude,
                        Latitude = p.Latitude,
                        CityId = p.CityId,
                        CityName = p.City == null ? null : p.City.Name,
                        StateId = p.StateId,
                        StateName = p.State == null ? null : p.State.Name,
                        PrimaryImage = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Branch
                                     && i.ReferenceId == p.Id
                                     && i.ImageRole == ImageRoleEnum.Primary)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Branch/" + i.ImageUrl,
                            })
                            .FirstOrDefault(),
                    })
                    .ToList();

                return new ResultDto<List<ResultBranchListDto>>
                {
                    Data = branchList,
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ارسال گردید.",
                    Status = 200,
                    Result = new ResultListDto()
                    {
                        Current = request.Page,
                        Count = rowsCount
                    },
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultBranchListDto>>(ex);
            }
        }

        // -------------------- GetBranchNearMeList --------------------
        // Rules:
        //  - CityId given, Long/Lati NOT given -> just return the active branches in that city (flat list, no pagination).
        //  - Long/Lati given (CityId optional, used only to pre-filter the candidate pool if present)
        //        -> search branches within 2km; if none found, widen to 3km; if still none, widen to 5km.
        //           Returns branches from the FIRST radius that has any results (does not merge tiers).
        //           If nothing found within 5km, Data.Branches is null.
        //  - Neither given -> validation error.
        public ResultDto<ResultBranchNearMeDto> GetBranchNearMeList(long? CityId, decimal? Long, decimal? Lati)
        {
            try
            {
                var hasCoordinates = Long.HasValue && Lati.HasValue;
                var hasCityOnly = !hasCoordinates && CityId.HasValue;

                if (!hasCoordinates && !hasCityOnly)
                {
                    return new ResultDto<ResultBranchNearMeDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Message = "شناسه شهر یا موقعیت مکانی (طول و عرض جغرافیایی) ارسال نشده است",
                        Status = 400
                    };
                }

                var projection = BranchProjection(_context, _configuration);

                if (hasCityOnly)
                {
                    var cityBranches = _context.Branches
                        .Where(p => p.IsActive && p.CityId == CityId)
                        .Select(projection)
                        .ToList();

                    return new ResultDto<ResultBranchNearMeDto>
                    {
                        Data = new ResultBranchNearMeDto { BranchesInCity = cityBranches },
                        IsSuccess = true,
                        Message = "اطلاعات با موفقیت ارسال گردید.",
                        Status = 200
                    };
                }

                var searchPoint = new Point((double)Long.Value, (double)Lati.Value) { SRID = 4326 };

                var baseQuery = _context.Branches.Where(p => p.IsActive);
                if (CityId.HasValue)
                    baseQuery = baseQuery.Where(p => p.CityId == CityId);

                var radiiMeters = new[] { 2_000, 3_000, 5_000 };

                List<ResultBranchListDto>? branches = null;

                foreach (var radius in radiiMeters)
                {
                    branches = baseQuery
                        .Where(p => p.Location.Distance(searchPoint) <= radius)
                        .OrderBy(p => p.Location.Distance(searchPoint))
                        .Select(projection)
                        .ToList();

                    if (branches.Count > 0)
                        break;

                    branches = null;
                }

                return new ResultDto<ResultBranchNearMeDto>
                {
                    Data = new ResultBranchNearMeDto { Branches = branches },
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ارسال گردید.",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultBranchNearMeDto>(ex);
            }
        }

        // =================================== Helpers ===================================
        private static Expression<Func<Branch, ResultBranchListDto>> BranchProjection(IDataBaseContext context, IConfiguration configuration)
        {
            var staticPath = configuration.GetSection("AppSettings")["StaticPath"];

            return p => new ResultBranchListDto
            {
                Id = p.Id,
                Title = p.Title,
                Address = p.Address,
                PhoneNumber = p.PhoneNumber,
                WorkHours = p.WorkHours,
                Holidays = p.Holidays,
                Longtitude = p.Longtitude,
                Latitude = p.Latitude,
                CityId = p.CityId,
                CityName = p.City == null ? null : p.City.Name,
                StateId = p.StateId,
                StateName = p.State == null ? null : p.State.Name,
                PrimaryImage = context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Branch && i.ReferenceId == p.Id && i.ImageRole == ImageRoleEnum.Primary)
                    .Select(i => new ResultImageDto
                    {
                        Id = i.Id,
                        Preview = staticPath + "/Resources/Branch/" + i.ImageUrl,
                    })
                    .FirstOrDefault(),
            };
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

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
                // prevent secondary crash during logging
            }

            return new ResultDto<T>
            {
                IsSuccess = false,
                Status = 500,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد"
            };
        }

        public class ResultBranchItemDto
        {
            public long Id { get; set; }
            public string Title { get; set; }
            public string Address { get; set; }
            public string PhoneNumber { get; set; }
            public string WorkHours { get; set; }
            public string? Holidays { get; set; }
            public decimal? Longtitude { get; set; }
            public decimal? Latitude { get; set; }
            public long? CityId { get; set; }
            public string? CityName { get; set; }
            public long? StateId { get; set; }
            public string? StateName { get; set; }
            public ResultImageDto? PrimaryImage { get; set; }
            public List<ResultImageDto> Images { get; set; } = new List<ResultImageDto>();
            public bool IsActive { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class ResultBranchListDto
        {
            public long Id { get; set; }
            public string Title { get; set; }
            public string Address { get; set; }
            public string PhoneNumber { get; set; }
            public string WorkHours { get; set; }
            public string? Holidays { get; set; }
            public decimal? Longtitude { get; set; }
            public decimal? Latitude { get; set; }
            public long? CityId { get; set; }
            public string? CityName { get; set; }
            public long? StateId { get; set; }
            public string? StateName { get; set; }
            public ResultImageDto? PrimaryImage { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class ResultBranchNearMeDto
        {
            public List<ResultBranchListDto>? BranchesInCity { get; set; }
            public List<ResultBranchListDto>? Branches { get; set; }
        }

        public class RequestBranchListDto
        {
            public int Page { get; set; }
            public int PageSize { get; set; } = 20;
            public string? Sort { get; set; }
            public string? Filter { get; set; }
        }
    }
}