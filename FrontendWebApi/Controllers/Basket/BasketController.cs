using Application.Services.Baskets.Commands;
using Application.Services.Baskets.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using FrontendWebApi.Models.ViewModels.Basket;
using Microsoft.AspNetCore.Mvc;
using static Application.Services.Baskets.Queries.BasketService;

namespace FrontEndWebApi.Controllers.Basket
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class BasketController : BaseController
    {
        private readonly IBasketCUDService _basketCUDService;
        private readonly IBasketService _basketService;

        public BasketController
            (IUserService userService,
            IBasketCUDService basketCUDService,
            IBasketService basketService
            /*IContentService contentService*/) : base(userService)
        {
            _basketCUDService = basketCUDService;
            _basketService = basketService;
        }



        [HttpPost]
        public JsonResult UpdateBasket(UpdateBasketViewModel request)
        {
            var result = _basketService.UpdateBasket(new RequestUpdateBasketDto
            {
                ProductId = request.ProductId,
                Type = request.Type,
                userId = CurrentUser.Id,
                publicToken = CurrentUser.PublicToken
            });

            var resultBasket = _basketService.CardBasketTotalData(CurrentUser.Id, CurrentUser.PublicToken);
            return Json(new { basket = result, totalPrice = resultBasket });
        }

        [HttpGet]
        public JsonResult ClearBasket()
        {
            var result = _basketService.ClearBasket(CurrentUser.Id, CurrentUser.PublicToken);
            return Json(result);
        }

        [HttpPost("{Id}")]
        public JsonResult ClearBasketItem(long Id)
        {
            var result = _basketService.ClearBasketItem(CurrentUser.Id, CurrentUser.PublicToken, Id);
            return Json(result);
        }

        [HttpGet]
        public JsonResult ValidateBasket()
        {
            var result = _basketService.ValidateBasket(CurrentUser.Id, CurrentUser.PublicToken);
            return Json(result);
        }

        [HttpGet]
        public JsonResult BasketItemCounts()
        {
            var result = _basketService.BasketItemCounts(CurrentUser.Id, CurrentUser.PublicToken);
            return Json(result);
        }

        [HttpGet]
        public JsonResult BasketItemList()
        {
            var result = _basketService.GetBasketItemList(CurrentUser.Id, CurrentUser.PublicToken);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult BasketDetail(long Id)
        {
            var result = _basketService.GetBasketDetailsById(Id);

            if (!result.IsSuccess || result.Data == null)
            {
                return Json(result);
            }

            if (result.Data.UserId != CurrentUser.Id)
            {
                var resultFail = new ResultDto
                {
                    IsSuccess = false,
                    Status = 403,
                    Message = "این سبد به این کاربر مربوط نیست"
                };
                return Json(resultFail);
            }

            return Json(result);
        }

        [HttpGet]
        public JsonResult CurrentBasketDetail()
        {
            var result = _basketService.GetCurrentBasket(CurrentUser.Id, CurrentUser.PublicToken);
            return Json(result);
        }

        [HttpPost]
        public JsonResult BasketList(RequestUserBasketListDto request)
        {
            var result = _basketService.GetAllBasketsByUserId(CurrentUser.Id, request);
            return Json(result);
        }

        [HttpGet]
        public JsonResult GetValidShippings()
        {
            var result = _basketService.GetValidShippings(CurrentUser.Id, CurrentUser.PublicToken);
            return Json(result);
        }

        [HttpPost]
        public JsonResult SetShippingOption(RequestSetShippingDto request)
        {
            var result = _basketService.ChooseShippingOption(CurrentUser.Id, CurrentUser.PublicToken, request);
            return Json(result);
        }
    }
    
}
