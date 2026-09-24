using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Products.Queries
{
    // ==================== Interface ====================
    public interface IBrandService
    {
        ResultDto<List<ResultBrandListDto>> GetBrandList(RequestListDto request);
        ResultDto<ResultBrandItemDto> GetBrandItem(long id);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<ResultBrandListDto>> GetBrandListFrontEnd(RequestListDto request);
        ResultDto<ResultBrandItemDto> GetBrandItemFrontEnd(long id);
    }

    // ==================== Implementation ====================
    public class BrandService : IBrandService
    {
        private readonly IDataBaseContext _context;

        public BrandService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<ResultBrandListDto>> GetBrandList(RequestListDto request)
        {
            try
            {
                var query = _context.Brands
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
                    .Select(x => new ResultBrandListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ResultBrandListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست برند ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultBrandListDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<ResultBrandListDto>> GetBrandListFrontEnd(RequestListDto request)
        {
            try
            {
                var query = _context.Brands
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderBy(s => s.Order);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(x => new ResultBrandListDto
                    {
                        Id = x.Id,
                        Title = x.Title
                    })
                    .ToList();

                return new ResultDto<List<ResultBrandListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست برند ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultBrandListDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ResultBrandItemDto> GetBrandItem(long id)
        {
            try
            {
                var brand = _context.Brands
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ResultBrandItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        IsActive = x.IsActive,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (brand == null)
                {
                    return new ResultDto<ResultBrandItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "برند یافت نشد"
                    };
                }

                return new ResultDto<ResultBrandItemDto>
                {
                    Data = brand,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات برند ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultBrandItemDto>(ex);
            }
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ResultBrandItemDto> GetBrandItemFrontEnd(long id)
        {
            try
            {
                var brand = _context.Brands
                    .AsNoTracking()
                    .Where(x => x.Id == id && x.IsActive)
                    .Select(x => new ResultBrandItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        IsActive = x.IsActive
                    })
                    .FirstOrDefault();

                if (brand == null)
                {
                    return new ResultDto<ResultBrandItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "برند یافت نشد"
                    };
                }

                return new ResultDto<ResultBrandItemDto>
                {
                    Data = brand,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات برند ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultBrandItemDto>(ex);
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
    public class ResultBrandListDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultBrandItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}