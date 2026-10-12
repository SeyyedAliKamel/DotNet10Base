using AdminWebApi.Models.ViewModels.Basket;
using Application.Services.Baskets.Commands;
using Application.Services.Baskets.Queries;
using Application.Services.Payments.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Infrastructure.ExternalApi.PaymentGatewaye;
using Microsoft.AspNetCore.Mvc;
using static Application.Services.Baskets.Queries.BasketService;

namespace AdminWebApi.Controllers.Admin
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class BasketController : BaseController
    {
        private readonly IBasketCUDService _basketCUDService;
        private readonly IBasketService _basketService;
        private readonly IPaymentService _paymentService;

        public BasketController
            (IUserService userService,
            IBasketCUDService basketCUDService,
            IBasketService basketService,
            IPaymentService paymentService
            /*IContentService contentService*/) : base(userService)
        {
            _basketCUDService = basketCUDService;
            _basketService = basketService;
            _paymentService = paymentService;
        }


        /// <summary>
        /// Basket status values:
        /// 0 = Created
        /// 1 = Cancelled
        /// 2 = Finalized
        /// 3 = Rejected
        /// 4 = Sent
        /// </summary>
        [HttpPost]
        public JsonResult ChangeBasketStatus(ChangeBasketStatusDto request)
        {

            var result = _basketCUDService.ChangeBasketStatus(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult BasketDetail(long Id)
        {
            var result = _basketService.GetBasketDetailsById(Id);
            return Json(result);
        }

        /// <summary>
        /// Basket status values:
        /// 0 = Created
        /// 1 = Cancelled
        /// 2 = Finalized
        /// 3 = Rejected
        /// 4 = Sent
        /// </summary>
        [HttpPost]
        public JsonResult BasketList(RequestListDto request)
        {
            var result = _basketService.GetBasketList(request);
            return Json(result);
        }
    }
}
