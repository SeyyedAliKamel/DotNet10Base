using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using FrontendWebApi.Models.ViewModels.Product;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ProductController : BaseController
    {
        private readonly IProductCUDService _productCUDService;
        private readonly IProductService _productService;
        public ProductController(
            IProductCUDService productCUDService,
            IProductService productService,
            IUserService userService
        ) : base(userService)
        {
            _productCUDService = productCUDService;
            _productService = productService;
        }



        [HttpPost("{Id}")]
        public JsonResult AddWishlist(long Id)
        {
            var result = _productCUDService.AddWish(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveWishlist(long Id)
        {
            var result = _productCUDService.RemoveWish(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult Wishlist(WishlistViewModel request)
        {
            var result = _productService.GetWishlist( new ProductWishlistRequestDto
            {
                UserId = CurrentUser.Id,
                Page = request.Page,
                PageSize = request.PageSize
            });
            return Json(result);
        }
    }
}