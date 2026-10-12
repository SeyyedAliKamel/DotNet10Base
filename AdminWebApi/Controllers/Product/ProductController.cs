using AdminWebApi.Models.ViewModels.Product;
using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
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
        private readonly IFileUploadService _fileUploadService;
        public ProductController(
            IProductCUDService productCUDService,
            IProductService productService,
            IFileUploadService fileUploadService,
            IUserService userService
        ) : base(userService)
        {
            _productCUDService = productCUDService;
            _productService = productService;
            _fileUploadService = fileUploadService;
        }



        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonResult AddProduct([FromForm] AddProductViewModel request)
        {
            var FileName = request.Files?
                .Select(f => Guid.NewGuid().ToString() + Helper.SanitizeFileName(f.FileName))
                .ToList() ?? new List<string>();

            if (FileName.Count > 0)
            {
                try
                {
                    _fileUploadService.UploadImages(request.Files, TypeSaveFile.Product, FileName);
                }
                catch (Exception ex)
                {
                    return Json(new ResultDto { IsSuccess = false, Status = 400, Message = "عکس آپلود نشد: " + ex.Message });
                }
            }

            var result = _productCUDService.AddProduct(new RequestAddProductDto
            {
                ProductId = request.ProductId,
                Name = request.Name,
                Description = request.Description,
                LongDescription = request.LongDescription,
                Price = request.Price,
                WholePrice = request.WholePrice,
                Quantity = request.Quantity,
                Weight = request.Weight,
                MinBuyLimit = request.MinBuyLimit,
                MaxBuyLimit = request.MaxBuyLimit,
                Rating = request.Rating,
                BrandId = request.BrandId,
                IsGrounded = request.IsGrounded,
                SlugTitle = request.SlugTitle,
                MetaTitle = request.MetaTitle,
                Attribute = request.Attribute,
                MetaDescription = request.MetaDescription,
                IsActive = request.IsActive,
                CategoryIds = Helper.JsonDeserialize(request.CategoryIds),
                HashtagIds = Helper.JsonDeserialize(request.HashtagIds),
                RelatedProductIds = Helper.JsonDeserialize(request.RelatedProductIds),
                ImageNames = FileName,
            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public JsonResult EditProduct([FromForm] EditProductViewModel request)
        {
            var FileName = request.Files?
                .Select(f => Guid.NewGuid().ToString() + Helper.SanitizeFileName(f.FileName))
                .ToList() ?? new List<string>();

            if (FileName.Count > 0)
            {
                try
                {
                    _fileUploadService.UploadImages(request.Files, TypeSaveFile.Product, FileName);
                }
                catch (Exception ex)
                {
                    return Json(new ResultDto { IsSuccess = false, Status = 400, Message = "عکس آپلود نشد: " + ex.Message });
                }
            }

            var result = _productCUDService.EditProduct(new RequestEditProductDto
            {
                Id = request.Id,
                ProductId = request.ProductId,
                Name = request.Name,
                Description = request.Description,
                LongDescription = request.LongDescription,
                Price = request.Price,
                WholePrice = request.WholePrice,
                Quantity = request.Quantity,
                MinBuyLimit = request.MinBuyLimit,
                MaxBuyLimit = request.MaxBuyLimit,
                Rating = request.Rating,
                IsGrounded = request.IsGrounded,
                Weight = request.Weight,
                BrandId = request.BrandId,
                SlugTitle = request.SlugTitle,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                IsActive = request.IsActive,
                Attribute = request.Attribute,
                CategoryIds = Helper.JsonDeserialize(request.CategoryIds),
                HashtagIds = Helper.JsonDeserialize(request.HashtagIds),
                RelatedProductIds = Helper.JsonDeserialize(request.RelatedProductIds),
                ImageNames = FileName,
            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpPost]
        public JsonResult EditProductPrimaryImage(long productId, long imageId)
        {
            var result = _productCUDService.EditProductPrimaryImage(productId, imageId, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveProduct(long Id)
        {
            var result = _productCUDService.RemoveProduct(Id, CurrentUser.Id);

            if (result.IsSuccess && result.Result is List<string> oldImagesToDelete)
            {
                foreach (var oldImage in oldImagesToDelete)
                {
                    if (string.IsNullOrWhiteSpace(oldImage)) continue;

                    try
                    {
                        _fileUploadService.DeleteImage(oldImage, TypeSaveFile.Product).GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }
                }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveProductImage(long Id)
        {
            var result = _productCUDService.RemoveProductImage(Id, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImageToDelete))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImageToDelete, TypeSaveFile.Product).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult ProductList(RequestListDto request)
        {
            var result = _productService.GetProductList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ProductItem(long Id)
        {
            var result = _productService.GetProductItem(Id);
            return Json(result);
        }
    }
}