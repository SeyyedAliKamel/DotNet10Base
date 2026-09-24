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
    public interface IMenuCUDService
    {
        ResultDto AddMenu(RequestAddMenuDto request, long currentUserId);
        ResultDto EditMenu(RequestEditMenuDto request, long currentUserId);
        ResultDto RemoveMenu(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class MenuCUDService : IMenuCUDService
    {
        private readonly IDataBaseContext _context;

        public MenuCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto AddMenu(RequestAddMenuDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                string parentPath = null;

                if (request.ParentId.HasValue)
                {
                    var parent = _context.Menus.FirstOrDefault(x => x.Id == request.ParentId.Value);
                    if (parent is null)
                    {
                        return new ResultDto
                        {
                            IsSuccess = false,
                            Status = 200,
                            Message = "منوی والد یافت نشد"
                        };
                    }

                    parentPath = parent.Path;
                }

                var menu = new Menu
                {
                    Title = request.Title,
                    Url = request.Url,
                    ParentId = request.ParentId,
                    Type = request.Type,
                    Path = "_",
                    IsActive = request.IsActive,
                };

                _context.SetUserId(currentUserId);
                _context.Menus.Add(menu);
                _context.SaveChanges();

                menu.Path = parentPath is null ? menu.Id.ToString() : $"{parentPath}_{menu.Id}";
                _context.Menus.Update(menu);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "اطلاعات منو با موفقیت ثبت شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = request.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);

                _context.SetUserId(currentUserId);
                _context.SaveChanges();

                return new ResultDto()
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
        }

        // -------------------- Update --------------------
        public ResultDto EditMenu(RequestEditMenuDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.Menus.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "اطلاعات اشتباه است"
                    };

                string parentPath = null;

                if (request.ParentId.HasValue)
                {
                    if (request.ParentId.Value == request.Id)
                    {
                        return new ResultDto
                        {
                            IsSuccess = false,
                            Status = 200,
                            Message = "منو نمی‌تواند والد خودش باشد"
                        };
                    }

                    var parent = _context.Menus.FirstOrDefault(x => x.Id == request.ParentId.Value);
                    if (parent is null)
                    {
                        return new ResultDto
                        {
                            IsSuccess = false,
                            Status = 200,
                            Message = "منوی والد یافت نشد"
                        };
                    }

                    var ancestorIds = parent.Path.Split('_');
                    if (ancestorIds.Contains(request.Id.ToString()))
                    {
                        return new ResultDto
                        {
                            IsSuccess = false,
                            Status = 200,
                            Message = "امکان انتخاب یکی از زیرمنوها به عنوان والد وجود ندارد"
                        };
                    }

                    parentPath = parent.Path;
                }

                var oldPath = data.Path;
                var newPath = parentPath is null ? data.Id.ToString() : $"{parentPath}_{data.Id}";

                data.Title = request.Title;
                data.Url = request.Url;
                data.ParentId = request.ParentId;
                data.Type = request.Type;
                data.Path = newPath;
                data.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.Menus.Update(data);

                // parent didn't actually change position -> nothing downstream to fix
                if (oldPath != newPath)
                {
                    var descendantPrefix = oldPath + "_";
                    var descendants = _context.Menus
                        .Where(x => x.Path.StartsWith(descendantPrefix))
                        .ToList();

                    foreach (var descendant in descendants)
                    {
                        descendant.Path = newPath + descendant.Path.Substring(oldPath.Length);
                    }

                    _context.Menus.UpdateRange(descendants);
                }

                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "منو با موفقیت ویرایش شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = request.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);

                _context.SetUserId(currentUserId);
                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
        }

        // -------------------- Delete --------------------
        public ResultDto RemoveMenu(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.Menus.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "منو یافت نشد",
                        Status = 200,
                    };

                var hasChildren = _context.Menus.Any(x => x.ParentId == id);
                if (hasChildren)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "ابتدا زیرمنوهای این منو را حذف کنید",
                        Status = 200,
                    };
                }

                _context.SetUserId(currentUserId);
                _context.Menus.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "منو با موفقیت حذف شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = id.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);

                _context.SetUserId(currentUserId);
                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
        }
    }

    // ==================== DTOs ====================

    public class RequestAddMenuDto
    {
        [Required(ErrorMessage = "تیتر الزامی است.")]
        public string Title { get; set; }
        public string? Url { get; set; }
        public long? ParentId { get; set; }
        public MenuTypeEnum Type { get; set; } = MenuTypeEnum.Internal;
        public bool IsActive { get; set; }
    }

    public class RequestEditMenuDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string? Url { get; set; }
        public long? ParentId { get; set; }
        public MenuTypeEnum Type { get; set; }
        public bool IsActive { get; set; }
    }
}