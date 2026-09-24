using Application.Interfaces.Contexts;
using Common.Dto;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services.Roles.Commands
{
    // ==================== Interface ====================
    public interface IRoleCUDService
    {
        ResultDto Add(RequestAddRoleDto request, long currentUserId);
        ResultDto Edit(RequestEditRoleDto request, long currentUserId);
        ResultDto Remove(long id, long currentUserId);
        ResultDto UpdatePermissions(UpdateRolePermissionDto request, long currentUserId);
    }

    // ==================== Implementation ====================
    public class RoleCUDService : IRoleCUDService
    {
        private readonly IDataBaseContext _context;

        public RoleCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Add --------------------
        public ResultDto Add(RequestAddRoleDto request, long currentUserId)
        {
            var role = new Role { Name = request.Name };
            _context.SetUserId(currentUserId);
            _context.Roles.Add(role);
            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Message = "نقش جدید ایجاد گردید", Status = 200 };
        }

        // -------------------- Edit --------------------
        public ResultDto Edit(RequestEditRoleDto request, long currentUserId)
        {
            var role = _context.Roles.FirstOrDefault(x => x.Id == request.Id);
            if (role == null)
                return new ResultDto { IsSuccess = false, Message = "نقش یافت نشد", Status = 400 };

            role.Name = request.Name;
            _context.SetUserId(currentUserId);
            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Message = "عنوان نقش تغییر پیدا کرد", Status = 200 };
        }

        // -------------------- Remove --------------------
        public ResultDto Remove(long id, long currentUserId)
        {
            var role = _context.Roles.Find(id);
            if (role == null)
                return new ResultDto { IsSuccess = false, Message = "نقش یافت نشد", Status = 400 };

            role.IsDeleted = true;
            _context.SetUserId(currentUserId);
            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Message = "نقش با موفقیت حذف گردید.", Status = 200 };
        }

        // -------------------- UpdatePermissions --------------------
        public ResultDto UpdatePermissions(UpdateRolePermissionDto request, long currentUserId)
        {
            _context.SetUserId(currentUserId);
            var role = _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefault(r => r.Id == request.RoleId);
            if (role == null)
                return new ResultDto { IsSuccess = false, Status = 404, Message = "نقش یافت نشد" };

            role.Permissions.Clear();
            var permissions = _context.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id))
                .ToList();
            foreach (var permission in permissions)
                role.Permissions.Add(permission);

            _context.SaveChanges();

            return new ResultDto { IsSuccess = true, Status = 200, Message = "دسترسی های نقش به روز رسانی گردید" };
        }
    }

    // ==================== DTOs ====================
    public class RequestAddRoleDto
    {
        public string Name { get; set; }
    }
    public class RequestEditRoleDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class UpdateRolePermissionDto
    {
        public long RoleId { get; set; }
        public List<long> PermissionIds { get; set; } = new();
    }
}