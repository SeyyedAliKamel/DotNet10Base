using Application.Services.Contents.Commands;
using Application.Services.Contents.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Mvc;
using Application.Services.Users.Queries;
using AdminWebApi.Models.ViewModels.Content;

namespace AdminWebApi.Controllers.Admin
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class ContentCategoryController : BaseController
    {
        private readonly IContentCategoryCUDService _contentCategoryCUDService;
        private readonly IContentCategoryService _contentCategoryService;
        private readonly IFileUploadService _fileUploadService;



        public ContentCategoryController
               (

                   IContentCategoryCUDService contentCategoryCUDService,
                   IFileUploadService fileUploadService,
                   IUserService userService,
                   IContentCategoryService contentCategoryService) : base(userService)
        {
            _contentCategoryCUDService = contentCategoryCUDService;
            _contentCategoryService = contentCategoryService;
            _fileUploadService = fileUploadService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonResult AddContentCategory([FromForm] AddContentCategoryViewModel request)
        {
            string fileName = null;
            var file = Request.Form.Files.FirstOrDefault();
            if (file != null)
            {
                var safeName = Helper.SanitizeFileName(file.FileName);
                fileName = Guid.NewGuid().ToString() + file.FileName;
                try
                {
                    _fileUploadService.UploadImage(
                        new List<IFormFile> { file },
                        TypeSaveFile.ContentCategory,
                        fileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }
            var result = _contentCategoryCUDService.AddContentCategory(new RequestAddContentCategoryDto
            {
                Description = request.Description,
                ImageFileName = fileName,
                ImageDescription = request.ImageDescription,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                Name = request.Name,
                SlugTitle = request.SlugTitle,
                IsActive = request.IsActive,
            }, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public JsonResult EditContentCategory([FromForm] EditContentCategoryViewModel request)
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
                        TypeSaveFile.ContentCategory,
                        FileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }
            var result = _contentCategoryCUDService.EditContentCategory(new RequestEditContentCategoryDto
            {
                Id = request.Id,
                Description = request.Description,
                ImageFileName = FileName,
                Name = request.Name,
                SlugTitle = request.SlugTitle,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                IsActive = request.IsActive,
            }, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.ContentCategory).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveContentCategory(long Id)
        {
            var result = _contentCategoryCUDService.RemoveContentCategory(Id, CurrentUser.Id);

            if (result.IsSuccess && result.Data?.OldImages != null)
            {
                foreach (var url in result.Data.OldImages)
                {
                    try
                    {
                        _fileUploadService.DeleteImage(url, TypeSaveFile.ContentCategory).GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }
                }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveContentCategoryImage(long Id)
        {
            var result = _contentCategoryCUDService.RemoveContentCategoryImage(Id, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.ContentCategory).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult ContentCategoryList(RequestListDto request)
        {
            var result = _contentCategoryService.GetContentCategoryList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ContentCategoryItem(long Id)
        {
            var result = _contentCategoryService.GetContentCategoryItem(Id);
            return Json(result);
        }
    }
}