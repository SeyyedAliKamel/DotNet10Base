using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Roles.Queries
{
    // ==================== Interface ====================
    public interface IRoleService
    {
        ResultDto<List<RolesDto>> GetRoleList(RoleListRequestDto request);
        ResultDto<RoleItemDto> GetItem(long id);
        ResultDto<List<RolePermissionDto>> GetRolePermissions(long id);
    }

    // ==================== Implementation ====================
    public class RoleService : IRoleService
    {
        private readonly IDataBaseContext _context;

        public RoleService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<RolesDto>> GetRoleList(RoleListRequestDto request)
        {
            try
            {
                var query = _context.Roles
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchKey))
                {
                    query = query.Where(p => p.Name.Contains(request.SearchKey));
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(p => new RolesDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<RolesDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست نقش‌ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<RolesDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<RoleItemDto> GetItem(long id)
        {
            try
            {
                var role = _context.Roles
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new RoleItemDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        PermissionIds = p.Permissions.Select(x => x.Id).ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (role == null)
                {
                    return new ResultDto<RoleItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "نقش یافت نشد"
                    };
                }

                return new ResultDto<RoleItemDto>
                {
                    Data = role,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات نقش ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<RoleItemDto>(ex);
            }
        }

        // -------------------- GetRolePermissions --------------------
        public ResultDto<List<RolePermissionDto>> GetRolePermissions(long id)
        {
            try
            {
                var list = _context.Roles
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .SelectMany(x => x.Permissions)
                    .Select(x => new RolePermissionDto
                    {
                        Id = x.Id,
                        Name = x.Name
                    })
                    .ToList();

                return new ResultDto<List<RolePermissionDto>>
                {
                    Data = list,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<RolePermissionDto>>(ex);
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
    public class RolesDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RoleItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<long>? PermissionIds { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RolePermissionDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class RoleListRequestDto
    {
        public string? SearchKey { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}