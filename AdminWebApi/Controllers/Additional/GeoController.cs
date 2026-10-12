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
    public class GeoController : BaseController
    {
        private readonly IGeoService _geoService;
        private readonly IGeoCUDService _geoCUDService;
        public GeoController(
            IGeoService geoService,
            IGeoCUDService geoCUDService,
            IUserService userService
        ) : base(userService)
        {
            _geoService = geoService;
            _geoCUDService = geoCUDService;
        }



        [HttpPost]
        public JsonResult CityList(RequestListDto request)
        {
            var result = _geoService.GetCityList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult CityItem(long Id)
        {
            var result = _geoService.GetCityItem(Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult StateList(RequestListDto request)
        {
            var result = _geoService.GetStateList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult StateItem(long Id)
        {
            var result = _geoService.GetStateItem(Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateGeoStatus(RequestToggleGeoStatusDto request)
        {
            var result = _geoCUDService.UpdateGeoStatus(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult AddState(RequestAddStateDto request)
        {
            var result = _geoCUDService.AddState(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditState(RequestEditStateDto request)
        {
            var result = _geoCUDService.EditState(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveState(long Id)
        {
            var result = _geoCUDService.RemoveState(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult AddCity(RequestAddCityDto request)
        {
            var result = _geoCUDService.AddCity(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditCity(RequestEditCityDto request)
        {
            var result = _geoCUDService.EditCity(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveCity(long Id)
        {
            var result = _geoCUDService.RemoveCity(Id, CurrentUser.Id);
            return Json(result);
        }
    }
}