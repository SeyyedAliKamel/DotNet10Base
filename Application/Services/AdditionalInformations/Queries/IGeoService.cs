using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdditionalInformations.Queries
{
    // ==================== Interface ====================
    public interface IGeoService
    {
        ResultDto<List<ResultCityListDto>> GetCityList(RequestListDto request);
        ResultDto<ResultCityItemDto> GetCityItem(long id);
        ResultDto<List<ResultStateListDto>> GetStateList(RequestListDto request);
        ResultDto<ResultStateItemDto> GetStateItem(long id);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<ResultCityListDto>> GetCityListFrontEnd(RequestListDto request);
        ResultDto<ResultCityItemDto> GetCityItemFrontEnd(long id);
        ResultDto<List<ResultStateListDto>> GetStateListFrontEnd(RequestListDto request);
        ResultDto<ResultStateItemDto> GetStateItemFrontEnd(long id);

        ResultDto<List<ResultStateItemDto>> GetAllGeoFrontEnd();
    }

    // ==================== Implementation ====================
    public class GeoService : IGeoService
    {
        private readonly IDataBaseContext _context;

        public GeoService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetCityList --------------------
        public ResultDto<List<ResultCityListDto>> GetCityList(RequestListDto request)
        {
            try
            {
                var query = _context.Cities
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
                    .Select(x => new ResultCityListDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        StateId = x.StateId,
                        StateName = x.State.Name,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ResultCityListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست شهرها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultCityListDto>>(ex);
            }
        }

        // -------------------- GetCityListFrontEnd --------------------
        public ResultDto<List<ResultCityListDto>> GetCityListFrontEnd(RequestListDto request)
        {
            try
            {
                var query = _context.Cities
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.State.IsActive)
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
                    .Select(x => new ResultCityListDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        StateId = x.StateId,
                        StateName = x.State.Name
                    })
                    .ToList();

                return new ResultDto<List<ResultCityListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست شهرها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultCityListDto>>(ex);
            }
        }

        // -------------------- GetCityItem --------------------
        public ResultDto<ResultCityItemDto> GetCityItem(long id)
        {
            try
            {
                var city = _context.Cities
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ResultCityItemDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        StateId = x.StateId,
                        StateName = x.State.Name,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (city == null)
                {
                    return new ResultDto<ResultCityItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "شهر یافت نشد"
                    };
                }

                return new ResultDto<ResultCityItemDto>
                {
                    Data = city,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات شهر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultCityItemDto>(ex);
            }
        }

        // -------------------- GetCityItemFrontend --------------------
        public ResultDto<ResultCityItemDto> GetCityItemFrontEnd(long id)
        {
            try
            {
                var city = _context.Cities
                    .AsNoTracking()
                    .Where(x => x.Id == id && x.IsActive && x.State.IsActive)
                    .Select(x => new ResultCityItemDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        StateId = x.StateId,
                        StateName = x.State.Name
                    })
                    .FirstOrDefault();

                if (city == null)
                {
                    return new ResultDto<ResultCityItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "شهر یافت نشد"
                    };
                }

                return new ResultDto<ResultCityItemDto>
                {
                    Data = city,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات شهر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultCityItemDto>(ex);
            }
        }

        // -------------------- GetStateList --------------------
        public ResultDto<List<ResultStateListDto>> GetStateList(RequestListDto request)
        {
            try
            {
                var query = _context.States
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
                    .Select(x => new ResultStateListDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        CitiesCount = x.Cities != null ? x.Cities.Count : 0,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ResultStateListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست استان‌ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultStateListDto>>(ex);
            }
        }

        // -------------------- GetStateListFrontEnd --------------------
        public ResultDto<List<ResultStateListDto>> GetStateListFrontEnd(RequestListDto request)
        {
            try
            {
                var query = _context.States
                    .AsNoTracking()
                    .Where(x => x.IsActive)
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
                    .Select(x => new ResultStateListDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        CitiesCount = x.Cities != null ? x.Cities.Count(c => c.IsActive) : 0
                    })
                    .ToList();

                return new ResultDto<List<ResultStateListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست استان‌ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultStateListDto>>(ex);
            }
        }

        // -------------------- GetStateItem --------------------
        public ResultDto<ResultStateItemDto> GetStateItem(long id)
        {
            try
            {
                var state = _context.States
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ResultStateItemDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Cities = x.Cities != null
                            ? x.Cities.Select(c => new ResultStateCityDto
                            {
                                Id = c.Id,
                                Name = c.Name
                            }).ToList()
                            : new List<ResultStateCityDto>(),
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (state == null)
                {
                    return new ResultDto<ResultStateItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "استان یافت نشد"
                    };
                }

                return new ResultDto<ResultStateItemDto>
                {
                    Data = state,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات استان ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultStateItemDto>(ex);
            }
        }

        // -------------------- GetStateItemFrontend --------------------
        public ResultDto<ResultStateItemDto> GetStateItemFrontEnd(long id)
        {
            try
            {
                var state = _context.States
                    .AsNoTracking()
                    .Where(x => x.Id == id && x.IsActive)
                    .Select(x => new ResultStateItemDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Cities = x.Cities != null
                            ? x.Cities.Where(c => c.IsActive).Select(c => new ResultStateCityDto
                            {
                                Id = c.Id,
                                Name = c.Name
                            }).ToList()
                            : new List<ResultStateCityDto>()
                    })
                    .FirstOrDefault();

                if (state == null)
                {
                    return new ResultDto<ResultStateItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "استان یافت نشد"
                    };
                }

                return new ResultDto<ResultStateItemDto>
                {
                    Data = state,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات استان ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultStateItemDto>(ex);
            }
        }

        // -------------------- GetAllGeoFrontEnd --------------------
        public ResultDto<List<ResultStateItemDto>> GetAllGeoFrontEnd()
        {
            try
            {
                var states = _context.States
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(x => new ResultStateItemDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Cities = x.Cities != null
                            ? x.Cities
                                .Where(c => c.IsActive)
                                .OrderBy(c => c.Name)
                                .Select(c => new ResultStateCityDto
                                {
                                    Id = c.Id,
                                    Name = c.Name
                                }).ToList()
                            : new List<ResultStateCityDto>()
                    })
                    .ToList();

                return new ResultDto<List<ResultStateItemDto>>
                {
                    Data = states,
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست کامل استان‌ها و شهرها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultStateItemDto>>(ex);
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

    // ==================== DTOs ====================
    public class ResultCityListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long StateId { get; set; }
        public string StateName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultCityItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long StateId { get; set; }
        public string StateName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultStateListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int CitiesCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultStateItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<ResultStateCityDto> Cities { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultStateCityDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}