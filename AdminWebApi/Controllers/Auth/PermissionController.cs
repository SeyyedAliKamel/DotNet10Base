using Application.Services.Permissions.Commands;
using Application.Services.Permissions.Queries;
using Application.Services.Users.Queries;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PermissionController : BaseController
    {
        private readonly IPermissionCUDService _permissionCUDService;
        private readonly IPermissionService _permissionService;

        public PermissionController(
            IUserService userService,
            IPermissionCUDService permissionCUDService,
            IPermissionService permissionService
        ) : base(userService)
        {
            _permissionCUDService = permissionCUDService;
            _permissionService = permissionService;
        }



        [HttpPost]
        public JsonResult AddPermission(RequestAddPermissionDto request)
        {
            var result = _permissionCUDService.AddPermission(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditPermission(RequestEditPermissionDto request)
        {
            var result = _permissionCUDService.EditPermission(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemovePermission(long Id)
        {
            var result = _permissionCUDService.RemovePermission(Id, CurrentUser.Id);
            return Json(result);
        }



        [HttpPost]
        public JsonResult PermissionList(PermissionListRequestDto request)
        {
            var result = _permissionService.GetPermissionList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult PermissionItem(long Id)
        {
            var result = _permissionService.GetPermissionItem(Id);
            return Json(result);
        }

        [HttpGet]
        public JsonResult PermissionWithCategoryList()
        {
            var result = _permissionService.GetPermissionWithCategoryList();
            return Json(result);
        }
    }
}