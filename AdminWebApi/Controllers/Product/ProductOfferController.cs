using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Application.Services.Users.Queries;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers.Product
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ProductOfferController : BaseController
    {
        private readonly IProductOfferCUDService _productOfferCUDService;
        private readonly IProductOfferService _productOfferService;

        public ProductOfferController(
            IProductOfferCUDService productOfferCUDService,
            IProductOfferService productOfferService,
            IUserService userService
        ) : base(userService)
        {
            _productOfferCUDService = productOfferCUDService;
            _productOfferService = productOfferService;
        }

        /// <summary>
        /// Product status values:
        /// 0 = Inactive
        /// 1 = Active
        /// 2 = Repetitive
        /// 3 = Upcoming
        /// </summary>
        [HttpPost]
        public JsonResult AddProductOffer(RequestAddProductOfferDto request)
        {
            var result = _productOfferCUDService.AddProductOffer(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditProductOffer(RequestEditProductOfferDto request)
        {
            var result = _productOfferCUDService.EditProductOffer(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveProductOffer(long id)
        {
            var result = _productOfferCUDService.RemoveProductOffer(id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ProductOfferList(RequestProductOfferListDto request)
        {
            var result = _productOfferService.GetProductOfferList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ProductOfferItem(long Id)
        {
            var result = _productOfferService.GetProductOfferItem(Id);
            return Json(result);
        }
    }
}