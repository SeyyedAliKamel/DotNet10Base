using Application.Interfaces.Contexts;
using Common.Dto;
using Domain.Entities.Users;
using System.Linq;

namespace Application.Services.Permissions.Commands
{
    // ==================== Interface ====================
    public interface IPermissionCUDService
    {
        ResultDto AddPermission(RequestAddPermissionDto request, long currentUserId);
        ResultDto EditPermission(RequestEditPermissionDto request, long currentUserId);
        ResultDto RemovePermission(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class PermissionCUDService : IPermissionCUDService
    {
        private readonly IDataBaseContext _context;

        public PermissionCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Add --------------------
        public ResultDto AddPermission(RequestAddPermissionDto request, long currentUserId)
        {
            var permission = new Permission
            {
                Action = request.Action,
                Controller = request.Controller,
                Name = request.Name,
                FrontEndName = request.Controller + request.Action,
                PermissionCategoryId = request.PermissionCategoryId,
            };

            _context.SetUserId(currentUserId);
            _context.Permissions.Add(permission);
            _context.SaveChanges();

            return new ResultDto
            {
                IsSuccess = true,
                Status = 200,
                Message = "اطلاعات دسترسی با موفقیت ثبت گردید"
            };
        }

        // -------------------- Edit --------------------
        public ResultDto EditPermission(RequestEditPermissionDto request, long currentUserId)
        {
            var permission = _context.Permissions.FirstOrDefault(x => x.Id == request.Id);
            if (permission == null)
                return new ResultDto { IsSuccess = false, Message = "سطح دسترسی یافت نشد", Status = 400 };

            permission.Name = request.Name;
            permission.Controller = request.Controller;
            permission.Action = request.Action;
            permission.FrontEndName = permission.Controller + permission.Action;
            permission.PermissionCategoryId = request.PermissionCategoryId;

            _context.SetUserId(currentUserId);
            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Message = "اطلاعات سطح دسترسی تغییر پیدا کرد", Status = 200 };
        }

        // -------------------- Remove --------------------
        public ResultDto RemovePermission(long id, long currentUserId)
        {
            var permission = _context.Permissions.Find(id);
            if (permission == null)
                return new ResultDto { IsSuccess = false, Message = "سطح دسترسی یافت نشد", Status = 400 };

            _context.SetUserId(currentUserId);
            permission.IsDeleted = true;
            //permission.DeletedAt = DateTime.Now;
            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Message = "سطح دسترسی با موفقیت حذف گردید.", Status = 200 };
        }
    }

    // ==================== DTOs ====================
    public class RequestAddPermissionDto
    {
        public string Name { get; set; }
        public string Controller { get; init; }
        public string Action { get; init; }
        public long PermissionCategoryId { get; set; }
    }

    public class RequestEditPermissionDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public long PermissionCategoryId { get; set; }
    }
}