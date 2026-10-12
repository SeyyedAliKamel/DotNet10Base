using AdminWebApi.Models.ViewModels.Role;
using Application.Services.Roles.Commands;
using Application.Services.Roles.Queries;
using Application.Services.Users.Queries;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class RoleController : BaseController
    {
        private readonly IRoleCUDService _roleCUDService;
        private readonly IRoleService _roleService;

        public RoleController(
            IUserService userService,
            IRoleCUDService roleCUDService,
            IRoleService roleService)
            : base(userService)
        {
            _roleCUDService = roleCUDService;
            _roleService = roleService;
        }



        [HttpPost]
        public JsonResult AddRole(RequestAddRoleDto request)
        {
            var result = _roleCUDService.Add(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditRole(RequestEditRoleDto request)
        {
            var result = _roleCUDService.Edit(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveRole(long Id)
        {
            var result = _roleCUDService.Remove(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult UpdateRolePermissions(UpdateRolePermissionDto request)
        {
            var result = _roleCUDService.UpdatePermissions(request, CurrentUser.Id);
            return Json(result);
        }



        [HttpPost]
        public JsonResult RoleList(RoleListRequestDto request)
        {
            var result = _roleService.GetRoleList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult RoleItem(long Id)
        {
            var result = _roleService.GetItem(Id);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult RolePermissions(long Id)
        {
            var result = _roleService.GetRolePermissions(Id);
            return Json(result);
        }
    }
}