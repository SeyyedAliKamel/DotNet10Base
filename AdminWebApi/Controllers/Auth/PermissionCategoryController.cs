using Application.Services.PermissionCategories.Commands;
using Application.Services.PermissionCategories.Queries;
using Application.Services.Users.Queries;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PermissionCategoryController : BaseController
    {
        private readonly IPermissionCategoryCUDService _permissionCategoryCUDService;
        private readonly IPermissionCategoryService _permissionCategoryService;

        public PermissionCategoryController(
            IUserService userService,
            IPermissionCategoryCUDService permissionCategoryCUDService,
            IPermissionCategoryService permissionCategoryService
        ) : base(userService)
        {
            _permissionCategoryCUDService = permissionCategoryCUDService;
            _permissionCategoryService = permissionCategoryService;
        }



        [HttpPost]
        public JsonResult AddPermissionCategory(RequestAddPermissionCategoryDto request)
        {
            var result = _permissionCategoryCUDService.AddPermissionCategory(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditPermissionCategory(RequestEditPermissionCategoryDto request)
        {
            var result = _permissionCategoryCUDService.EditPermissionCategory(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemovePermissionCategory(int Id)
        {
            var result = _permissionCategoryCUDService.RemovePermissionCategory(Id, CurrentUser.Id);
            return Json(result);
        }



        [HttpPost]
        public JsonResult PermissionCategoryList(PermissionCategoryListRequestDto request)
        {
            var result = _permissionCategoryService.GetPermissionCategoryList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult PermissionCategoryItem(long Id)
        {
            var result = _permissionCategoryService.GetPermissionCategoryItem(Id);
            return Json(result);
        }
    }
}