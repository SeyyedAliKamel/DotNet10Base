using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System;
using System.Linq;
using static Common.Helper;

namespace Application.Services.PermissionCategories.Commands
{
    // ==================== Interface ====================
    public interface IPermissionCategoryCUDService
    {
        ResultDto AddPermissionCategory(RequestAddPermissionCategoryDto request, long currentUserId);
        ResultDto EditPermissionCategory(RequestEditPermissionCategoryDto request, long currentUserId);
        ResultDto RemovePermissionCategory(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class PermissionCategoryCUDService : IPermissionCategoryCUDService
    {
        private readonly IDataBaseContext _context;

        public PermissionCategoryCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto AddPermissionCategory(RequestAddPermissionCategoryDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var exists = _context.PermissionCategories.Any(x => x.Title == request.Title && !x.IsDeleted);

                if (exists)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "دسته بندی تکراری است",
                        Status = 200,
                    };

                var permissionCategory = new PermissionCategory
                {
                    Title = request.Title
                };

                _context.SetUserId(currentUserId);
                _context.PermissionCategories.Add(permissionCategory);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "دسته بندی جدید ایجاد گردید"
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
        public ResultDto EditPermissionCategory(RequestEditPermissionCategoryDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.PermissionCategories.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "دسته بندی یافت نشد"
                    };

                var duplicate = _context.PermissionCategories.Any(x =>
                    x.Id != request.Id &&
                    x.Title == request.Title &&
                    !x.IsDeleted);

                if (duplicate)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "دسته بندی تکراری است"
                    };

                data.Title = request.Title;

                _context.SetUserId(currentUserId);
                _context.PermissionCategories.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات دسته بندی سطح دسترسی تغییر پیدا کرد"
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

                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
        }

        // -------------------- Remove --------------------
        public ResultDto RemovePermissionCategory(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.PermissionCategories.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "دسته بندی یافت نشد",
                        Status = 200,
                    };

                // Preserving your soft-delete logic from the original Permission code
                existing.IsDeleted = true;

                _context.SetUserId(currentUserId);
                _context.PermissionCategories.Update(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "دسته بندی با موفقیت حذف گردید.",
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

    public class RequestAddPermissionCategoryDto
    {
        public string Title { get; set; }
    }

    public class RequestEditPermissionCategoryDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
    }
}