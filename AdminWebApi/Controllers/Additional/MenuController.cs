using Application.Services.AdditionalInformations.Commands;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Users.Queries;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;
namespace AdminWebApi.Controllers.AdditionalInformations
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class MenuController : BaseController
    {
        private readonly IMenuCUDService _menuCUDService;
        private readonly IMenuService _menuService;
        public MenuController(
            IMenuCUDService menuCUDService,
            IMenuService menuService,
            IUserService userService
        ) : base(userService)
        {
            _menuCUDService = menuCUDService;
            _menuService = menuService;
        }



        [HttpPost]
        public JsonResult AddMenu(RequestAddMenuDto request)
        {
            var result = _menuCUDService.AddMenu(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditMenu(RequestEditMenuDto request)
        {
            var result = _menuCUDService.EditMenu(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveMenu(long id)
        {
            var result = _menuCUDService.RemoveMenu(id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult MenuList(RequestListDto request)
        {
            var result = _menuService.GetMenuList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult MenuItem(long Id)
        {
            var result = _menuService.GetMenuItem(Id);
            return Json(result);
        }
    }
}