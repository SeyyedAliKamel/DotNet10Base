using AdminWebApi.Models.ViewModels.Basket;
using Application.Services.Shippings.Commands;
using Application.Services.Shippings.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers.Admin
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class ShippingController : BaseController
    {
        private readonly IShippingCUDService _shippingCUDService;
        private readonly IShippingService _shippingService;

        public ShippingController
            (IUserService userService,
            IShippingCUDService shippingCUDService,
            IShippingService shippingService
            ) : base(userService)
        {
            _shippingCUDService = shippingCUDService;
            _shippingService = shippingService;
        }

        [HttpPost]
        public JsonResult AddShipping(RequestAddShippingDto request)
        {
            var result = _shippingCUDService.AddShipping(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditShipping(RequestEditShippingDto request)
        {
            var result = _shippingCUDService.EditShipping(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveShipping(long id)
        {
            var result = _shippingCUDService.RemoveShipping(id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ShippingList(RequestListDto request)
        {
            var result = _shippingService.GetShippingList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ShippingItem(long Id)
        {
            var result = _shippingService.GetShippingItem(Id);
            return Json(result);
        }
    }
}