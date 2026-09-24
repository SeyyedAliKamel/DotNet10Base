using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdditionalInformations.Queries
{
    // ==================== Interface ====================
    public interface IMenuService
    {
        ResultDto<List<ResultMenuListDto>> GetMenuList(RequestListDto request);
        ResultDto<ResultMenuItemDto> GetMenuItem(long id);
        ResultDto<List<ResultMenuListDto>> GetMenuListByParentId(long? parentId);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<ResultMenuListDto>> GetMenuListFrontEnd(RequestListDto request);
        ResultDto<ResultMenuItemDto> GetMenuItemFrontEnd(long id);
    }

    // ==================== Implementation ====================
    public class MenuService : IMenuService
    {
        private readonly IDataBaseContext _context;

        public MenuService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<ResultMenuListDto>> GetMenuList(RequestListDto request)
        {
            try
            {
                var query = _context.Menus
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
                    .Select(x => new ResultMenuListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Url = x.Url,
                        Path = x.Path,
                        ParentId = x.ParentId,
                        ParentTitle = x.Parent != null ? x.Parent.Title : null,
                        Type = x.Type,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ResultMenuListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست منوها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultMenuListDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<ResultMenuListDto>> GetMenuListFrontEnd(RequestListDto request)
        {
            try
            {
                var query = _context.Menus
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
                    .Select(x => new ResultMenuListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Url = x.Url,
                        Path = x.Path,
                        ParentId = x.ParentId,
                        ParentTitle = x.Parent != null ? x.Parent.Title : null,
                        Type = x.Type
                    })
                    .ToList();

                return new ResultDto<List<ResultMenuListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست منوها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultMenuListDto>>(ex);
            }
        }

        // -------------------- GetMenuListByParentId --------------------
        public ResultDto<List<ResultMenuListDto>> GetMenuListByParentId(long? parentId)
        {
            try
            {
                var menus = _context.Menus
                    .AsNoTracking()
                    .Where(x => x.ParentId == parentId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new ResultMenuListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Url = x.Url,
                        Path = x.Path,
                        ParentId = x.ParentId,
                        ParentTitle = x.Parent != null ? x.Parent.Title : null,
                        Type = x.Type
                    })
                    .ToList();

                return new ResultDto<List<ResultMenuListDto>>
                {
                    Data = menus,
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست منوها ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultMenuListDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ResultMenuItemDto> GetMenuItem(long id)
        {
            try
            {
                var menu = _context.Menus
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ResultMenuItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Url = x.Url,
                        Path = x.Path,
                        ParentId = x.ParentId,
                        ParentTitle = x.Parent != null ? x.Parent.Title : null,
                        Type = x.Type,
                        IsActive = x.IsActive,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (menu == null)
                {
                    return new ResultDto<ResultMenuItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "منو یافت نشد"
                    };
                }

                return new ResultDto<ResultMenuItemDto>
                {
                    Data = menu,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات منو ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultMenuItemDto>(ex);
            }
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ResultMenuItemDto> GetMenuItemFrontEnd(long id)
        {
            try
            {
                var menu = _context.Menus
                    .AsNoTracking()
                    .Where(x => x.Id == id && x.IsActive)
                    .Select(x => new ResultMenuItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Url = x.Url,
                        Path = x.Path,
                        ParentId = x.ParentId,
                        ParentTitle = x.Parent != null ? x.Parent.Title : null,
                        Type = x.Type,
                        IsActive = x.IsActive
                    })
                    .FirstOrDefault();

                if (menu == null)
                {
                    return new ResultDto<ResultMenuItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "منو یافت نشد"
                    };
                }

                return new ResultDto<ResultMenuItemDto>
                {
                    Data = menu,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات منو ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultMenuItemDto>(ex);
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
    public class ResultMenuListDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string? Url { get; set; }
        public string Path { get; set; }
        public long? ParentId { get; set; }
        public string? ParentTitle { get; set; }
        public MenuTypeEnum Type { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultMenuItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string? Url { get; set; }
        public string Path { get; set; }
        public long? ParentId { get; set; }
        public string? ParentTitle { get; set; }
        public MenuTypeEnum Type { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}