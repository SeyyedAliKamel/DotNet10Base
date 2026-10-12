using Application.Services.AdditionalInformations.Commands;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SettingController : BaseController
    {
        private readonly ISettingCUDService _settingCUDService;
        private readonly ISettingService _settingService;
        public SettingController(
            ISettingCUDService settingCUDService,
            ISettingService settingService,
            IUserService userService
        ) : base(userService)
        {
            _settingCUDService = settingCUDService;
            _settingService = settingService;
        }

        [HttpPost]
        public JsonResult AddSetting(RequestAddSettingDto request)
        {
            var result = _settingCUDService.AddSetting(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditSetting(RequestEditSettingDto request)
        {
            var result = _settingCUDService.EditSetting(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveSetting(long Id)
        {
            var result = _settingCUDService.RemoveSetting(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult SettingList(RequestSettingListDto request)
        {
            var result = _settingService.GetSettingList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult SettingItem(long Id)
        {
            var result = _settingService.GetSettingItem(Id);
            return Json(result);
        }
    }
}