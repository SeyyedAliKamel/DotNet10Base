using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Mvc;
using Application.Services.Users.Queries;
using AdminWebApi.Models.ViewModels.Product;

namespace AdminWebApi.Controllers.Admin
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class ProductCategoryController : BaseController
    {
        private readonly IProductCategoryCUDService _productCategoryCUDService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IFileUploadService _fileUploadService;

        public ProductCategoryController
               (
                   IProductCategoryCUDService productCategoryCUDService,
                   IFileUploadService fileUploadService,
                   IUserService userService,
                   IProductCategoryService productCategoryService) : base(userService)
        {
            _productCategoryCUDService = productCategoryCUDService;
            _productCategoryService = productCategoryService;
            _fileUploadService = fileUploadService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonResult AddProductCategory([FromForm] AddProductCategoryViewModel request)
        {
            string FileName = null;
            var file = Request.Form.Files.FirstOrDefault();
            if (file != null)
            {
                var safeName = Helper.SanitizeFileName(file.FileName);
                FileName = Guid.NewGuid().ToString() + file.FileName;
                try
                {
                    _fileUploadService.UploadImage(
                        new List<IFormFile> { file },
                        TypeSaveFile.ProductCategory,
                        FileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }
            var result = _productCategoryCUDService.AddProductCategory(new RequestAddProductCategoryDto
            {
                Name = request.Name,
                Description = request.Description,
                ImageFileName = FileName,
                ImageDescription = request.ImageDescription,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                ParentId = request.ParentId,
                SlugTitle = request.SlugTitle,
                IsActive = request.IsActive,
            }, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public JsonResult EditProductCategory([FromForm] EditProductCategoryViewModel request)
        {
            string FileName = null;
            var file = Request.Form.Files.FirstOrDefault();
            if (file != null)
            {
                var safeName = Helper.SanitizeFileName(file.FileName);
                FileName = Guid.NewGuid().ToString() + file.FileName;
                try
                {
                    _fileUploadService.UploadImage(
                        new List<IFormFile> { file },
                        TypeSaveFile.ProductCategory,
                        FileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }
            var result = _productCategoryCUDService.EditProductCategory(new RequestEditProductCategoryDto
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                ImageFileName = FileName,
                ImageDescription = request.ImageDescription,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                ParentId = request.ParentId,
                SlugTitle = request.SlugTitle,
                IsActive = request.IsActive,
            }, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.ProductCategory).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveProductCategory(long Id)
        {
            var result = _productCategoryCUDService.RemoveProductCategory(Id, CurrentUser.Id);

            if (result.IsSuccess && result.Data?.OldImages != null)
            {
                foreach (var url in result.Data.OldImages)
                {
                    try
                    {
                        _fileUploadService.DeleteImage(url, TypeSaveFile.ProductCategory).GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }
                }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveProductCategoryImage(long Id)
        {
            var result = _productCategoryCUDService.RemoveProductCategoryImage(Id, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.ProductCategory).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult ProductCategoryList(RequestListDto request)
        {
            var result = _productCategoryService.GetProductCategoryList(new ProductCategoryListRequestDto
            {
                Page = request.Page,
                PageSize = request.PageSize
            });
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ProductCategoryItem(long Id)
        {
            var result = _productCategoryService.GetProductCategoryItem(Id);
            return Json(result);
        }
    }
}