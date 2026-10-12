using Application.Services.Orders.Commands;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers.Order
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class OrderController : BaseController
    {
        private readonly IOrderService _orderService;

        public OrderController(
            IOrderService orderService,
            IUserService userService
        ) : base(userService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public JsonResult EditOrder(RequestEditOrderDto request)
        {
            var result = _orderService.EditOrder(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult FillNulls()
        {
            var result = _orderService.FillNulls(CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ResetOrder(OrderTypeEnum type)
        {
            var result = _orderService.ResetOrder(type, CurrentUser.Id);
            return Json(result);
        }
    }
}