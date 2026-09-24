using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.PermissionCategories.Queries
{
    // ==================== Interface ====================
    public interface IPermissionCategoryService
    {
        ResultDto<List<PermissionCategoryDto>> GetPermissionCategoryList(PermissionCategoryListRequestDto request);
        ResultDto<PermissionCategoryDto> GetPermissionCategoryItem(long id);
    }

    // ==================== Implementation ====================
    public class PermissionCategoryService : IPermissionCategoryService
    {
        private readonly IDataBaseContext _context;

        public PermissionCategoryService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<PermissionCategoryDto>> GetPermissionCategoryList(PermissionCategoryListRequestDto request)
        {
            try
            {
                var query = _context.PermissionCategories
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchKey))
                {
                    query = query.Where(p => p.Title.Contains(request.SearchKey));
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new PermissionCategoryDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<PermissionCategoryDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست اطلاعات دسته بندی ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<PermissionCategoryDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<PermissionCategoryDto> GetPermissionCategoryItem(long id)
        {
            try
            {
                var item = _context.PermissionCategories
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new PermissionCategoryDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (item == null)
                {
                    return new ResultDto<PermissionCategoryDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "دسته بندی یافت نشد"
                    };
                }

                return new ResultDto<PermissionCategoryDto>
                {
                    Data = item,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات با موفقیت ارسال شد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<PermissionCategoryDto>(ex);
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
                // avoid crashing inside logging
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
    public class PermissionCategoryDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PermissionCategoryListRequestDto
    {
        public string? SearchKey { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}