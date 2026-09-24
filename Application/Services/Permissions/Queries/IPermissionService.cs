using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Permissions.Queries
{
    // ==================== Interface ====================
    public interface IPermissionService
    {
        ResultDto<List<PermissionDto>> GetPermissionList(PermissionListRequestDto request);
        ResultDto<PermissionDto> GetPermissionItem(long id);
        ResultDto<List<PermissionCategoryDto>> GetPermissionWithCategoryList();
    }

    // ==================== Implementation ====================
    public class PermissionService : IPermissionService
    {
        private readonly IDataBaseContext _context;

        public PermissionService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<PermissionDto>> GetPermissionList(PermissionListRequestDto request)
        {
            try
            {
                var query = _context.Permissions
                    .AsNoTracking()
                    .Include(x => x.PermissionCategory)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchKey))
                {
                    query = query.Where(p =>
                        p.Controller.Contains(request.SearchKey) ||
                        p.Name.Contains(request.SearchKey));
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Controller = p.Controller,
                        Action = p.Action,
                        FrontEndName = p.FrontEndName,
                        PermissionCategoryId = p.PermissionCategoryId,
                        CategoryName = p.PermissionCategory.Title,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<PermissionDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست اطلاعات دسترسی ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<PermissionDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<PermissionDto> GetPermissionItem(long id)
        {
            try
            {
                var permission = _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Controller = p.Controller,
                        Action = p.Action,
                        FrontEndName = p.FrontEndName,
                        PermissionCategoryId = p.PermissionCategoryId,
                        CategoryName = p.PermissionCategory.Title,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (permission == null)
                {
                    return new ResultDto<PermissionDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "سطح دسترسی یافت نشد"
                    };
                }

                return new ResultDto<PermissionDto>
                {
                    Data = permission,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات سطح دسترسی ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<PermissionDto>(ex);
            }
        }

        // -------------------- GetPermissionWithCategoryList --------------------
        public ResultDto<List<PermissionCategoryDto>> GetPermissionWithCategoryList()
        {
            try
            {
                var result = _context.PermissionCategories
                    .AsNoTracking()
                    .Select(p => new PermissionCategoryDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Permissions = p.Permissions.Select(x => new PermissionLookupDto
                        {
                            Id = x.Id,
                            Name = x.Name
                        }).ToList()
                    })
                    .ToList();

                return new ResultDto<List<PermissionCategoryDto>>
                {
                    Data = result,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات با موفقیت ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<PermissionCategoryDto>>(ex);
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
                // prevent cascading failure
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
    public class PermissionDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string FrontEndName { get; set; }
        public long PermissionCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PermissionCategoryDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public List<PermissionLookupDto> Permissions { get; set; }
    }

    public class PermissionLookupDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class PermissionListRequestDto
    {
        public string? SearchKey { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}