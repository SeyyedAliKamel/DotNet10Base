using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Services.AdditionalInformations.Queries
{
    // ==================== Interface ====================
    public interface IBannerService
    {
        ResultDto<List<ResultBannerListDto>> GetBannerList(RequestListDto request);
        ResultDto<ResultBannerItemDto> GetBannerItem(long id);
        ResultDto<List<ResultBannerListDto>> GetBannerListByPosition(RequestBannerByPositionDto requestBannerByPositionDto);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<ResultBannerListDto>> GetBannerListFrontEnd(RequestListDto request);
        ResultDto<ResultBannerItemDto> GetBannerItemFrontEnd(long id);
    }

    // ==================== Implementation ====================
    public class BannerService : IBannerService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public BannerService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

        }

        // -------------------- GetList --------------------
        public ResultDto<List<ResultBannerListDto>> GetBannerList(RequestListDto request)
        {
            try
            {
                var query = _context.Banners
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
                    .Select(x => new ResultBannerListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        AdminTitle = x.AdminTitle,
                        Image = x.ImageUrl == null ? null : new ResultImageDto
                        {

                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Banner/" + x.ImageUrl,
                            Id = x.Id

                        },
                        Url = x.Url,
                        Position = x.Position,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToList();

                return new ResultDto<List<ResultBannerListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست بنر ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultBannerListDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<ResultBannerListDto>> GetBannerListFrontEnd(RequestListDto request)
        {
            try
            {
                var query = _context.Banners
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
                    .Select(x => new ResultBannerListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        AdminTitle = x.AdminTitle,
                        Image = x.ImageUrl == null ? null : new ResultImageDto
                        {
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Banner/" + x.ImageUrl,
                            Id = x.Id
                        },
                        Url = x.Url,
                        Position = x.Position
                    })
                    .ToList();

                return new ResultDto<List<ResultBannerListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست بنر ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultBannerListDto>>(ex);
            }
        }

        // -------------------- GetBannerListByPosition --------------------
        public ResultDto<List<ResultBannerListDto>> GetBannerListByPosition(RequestBannerByPositionDto request)
        {
            try
            {
                var query = _context.Banners
                    .AsNoTracking()
                    //.Where(x => x.IsActive)
                    .AsQueryable();

                if (request.Position.HasValue)
                {
                    query = query.Where(x => x.Position == request.Position.Value);
                }

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderByDescending(x => x.CreatedAt);
                }

                if (request.Count.HasValue && request.Count.Value > 0)
                {
                    query = query.Take(request.Count.Value);
                }

                var banners = query
                    .Select(x => new ResultBannerListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        AdminTitle = x.AdminTitle,
                        Image = x.ImageUrl == null ? null : new ResultImageDto
                        {
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Banner/" + x.ImageUrl,
                            Id = x.Id
                        },
                        Url = x.Url,
                        Position = x.Position
                    })
                    .ToList();

                return new ResultDto<List<ResultBannerListDto>>
                {
                    Data = banners,
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست بنرها ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultBannerListDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ResultBannerItemDto> GetBannerItem(long id)
        {
            try
            {
                var banner = _context.Banners
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ResultBannerItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        AdminTitle = x.AdminTitle,
                        Image = x.ImageUrl == null ? null : new ResultImageDto
                        {

                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Banner/" + x.ImageUrl,
                            Id = x.Id

                        },
                        Url = x.Url,
                        Position = x.Position,
                        IsActive = x.IsActive,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt
                    })
                    .FirstOrDefault();

                if (banner == null)
                {
                    return new ResultDto<ResultBannerItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "بنر یافت نشد"
                    };
                }

                return new ResultDto<ResultBannerItemDto>
                {
                    Data = banner,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات بنر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultBannerItemDto>(ex);
            }
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ResultBannerItemDto> GetBannerItemFrontEnd(long id)
        {
            try
            {
                var banner = _context.Banners
                    .AsNoTracking()
                    .Where(x => x.Id == id && x.IsActive)
                    .Select(x => new ResultBannerItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        AdminTitle = x.AdminTitle,
                        Image = x.ImageUrl == null ? null : new ResultImageDto
                        {

                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Banner/" + x.ImageUrl,
                            Id = x.Id

                        },
                        Url = x.Url,
                        Position = x.Position,
                        IsActive = x.IsActive
                    })
                    .FirstOrDefault();

                if (banner == null)
                {
                    return new ResultDto<ResultBannerItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "بنر یافت نشد"
                    };
                }

                return new ResultDto<ResultBannerItemDto>
                {
                    Data = banner,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات بنر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultBannerItemDto>(ex);
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
    public class ResultBannerListDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string AdminTitle { get; set; }
        public ResultImageDto Image { get; set; }
        public string Url { get; set; }
        public BannerPositionEnum Position { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultBannerItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string AdminTitle { get; set; }
        public ResultImageDto Image { get; set; }
        public string Url { get; set; }
        public BannerPositionEnum Position { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RequestBannerByPositionDto
    {
        public BannerPositionEnum? Position { get; set; }
        public int? Count { get; set; }
        public string? Sort { get; set; }
        public string? Filter { get; set; }
    }
}