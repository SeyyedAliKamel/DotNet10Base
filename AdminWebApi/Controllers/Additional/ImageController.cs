using AdminWebApi.Models.ViewModels.Additional;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.Images.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using EndPoint.WebService.Controllers;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers.Admin
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class ImageController : BaseController
    {
        private readonly IImageCUDService _imageCUDService;
        private readonly IImageService _imageService;
        private readonly IFileUploadService _fileUploadService;

        public ImageController(
            IUserService userService,
            IImageCUDService imageCUDService,
            IImageService imageService,
            IFileUploadService fileUploadService) : base(userService)
        {
            _imageCUDService = imageCUDService;
            _imageService = imageService;
            _fileUploadService = fileUploadService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonResult AddImageEditor([FromForm] AddImageViewModel request)
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return Json(new { success = false, message = "فایلی ارسال نشده است" });

            var fileName = Guid.NewGuid().ToString() + file.FileName;
            try
            {
                _fileUploadService.UploadImage(new List<IFormFile> { file }, TypeSaveFile.Editor, fileName)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
            }

            var result = _imageCUDService.AddImage(new RequestAddImageDto
            {
                ImageUrl = fileName,
                ImageDescription = request.Alt,
                ReferenceId = null,
                ImageType = ImageTypeEnum.Editor,
                ImageRole = ImageRoleEnum.Single,
            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public JsonResult EditImageEditor([FromForm] EditImageViewModel request)
        {
            string fileName = null;
            var file = Request.Form.Files.FirstOrDefault();

            if (file != null)
            {
                fileName = Guid.NewGuid().ToString() + file.FileName;
                try
                {
                    _fileUploadService.UploadImage(new List<IFormFile> { file }, TypeSaveFile.Editor, fileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }

            var result = _imageCUDService.EditImage(new RequestEditImageDto
            {
                Id = request.Id,
                ImageUrl = fileName,
                ImageDescription = request.Alt,
            }, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try { _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.Editor).GetAwaiter().GetResult(); }
                catch { }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveImage(long Id)
        {
            var result = _imageCUDService.RemoveImage(Id, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try { _fileUploadService.DeleteImage(result.Data.OldImage, MapImageType(result.Data.ImageType)).GetAwaiter().GetResult(); }
                catch { }
            }

            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ImageItem(long Id)
        {
            var result = _imageService.GetImageItem(Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ImageListEditor(RequestListDto request)
        {
            var result = _imageService.GetImageList(ImageTypeEnum.Editor, request);
            return Json(result);
        }




        private static TypeSaveFile MapImageType(ImageTypeEnum imageType) => imageType switch
        {
            ImageTypeEnum.Editor => TypeSaveFile.Editor,
            ImageTypeEnum.Product => TypeSaveFile.Product,
            ImageTypeEnum.Content => TypeSaveFile.Content,
            ImageTypeEnum.Branch => TypeSaveFile.Branch,
            ImageTypeEnum.Cheque => TypeSaveFile.Cheque,
            ImageTypeEnum.BasketItem => TypeSaveFile.BasketItem,
            ImageTypeEnum.ProductCategory => TypeSaveFile.ProductCategory,
            ImageTypeEnum.ContentCategory => TypeSaveFile.ContentCategory,
            _ => TypeSaveFile.Public
        };
    }
}