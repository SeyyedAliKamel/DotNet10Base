using AdminWebApi.Models.ViewModels.Content;
using Application.Services.Contents.Commands;
using Application.Services.Contents.Queries;
using Application.Services.Users.Queries;

using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers.Admin
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class ContentController : BaseController
    {
        private readonly IContentCUDService _contentCUDService;
        private readonly IContentService _contentService;
        private readonly IContentBroadcastService _contentBroadcastService;
        private readonly IFileUploadService _fileUploadService;

        public ContentController
            (
                            IUserService userService,
                IContentCUDService contentCUDService,
                IFileUploadService fileUploadService,
                IContentService contentService,
                IContentBroadcastService contentBroadcastService
                ) : base(userService)
        {
            _contentCUDService = contentCUDService;
            _contentService = contentService;
            _contentBroadcastService = contentBroadcastService;
            _fileUploadService = fileUploadService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonResult AddContent([FromForm] AddContentViewModel request)
        {
            string fileName = null;
            if (request.Files != null)
            {
                var safeName = Helper.SanitizeFileName(request.Files.FileName);
                fileName = Guid.NewGuid().ToString() + safeName;
                try
                {
                    _fileUploadService.UploadImage(
                        new List<IFormFile> { request.Files },
                        TypeSaveFile.Content,
                        fileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }

            var result = _contentCUDService.AddContent(new RequestAddContentDto
            {
                Description = request.Description,
                MetaDescription = request.MetaDescription,
                MetaTitle = request.MetaTitle,
                ShortDescription = request.ShortDescription,
                Name = request.Name,
                ContentCategoryId = request.ContentCategoryId,
                ContentRelationIds = Helper.JsonDeserialize(request.ContentRelationIds),
                SlugTitle = request.SlugTitle,
                ReleaseDate = request.ReleaseDate,
                IsActive = request.IsActive,
                ImageFileName = fileName,
            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public JsonResult EditContent([FromForm] EditContentViewModel request)
        {
            string fileName = null;
            var file = Request.Form.Files.FirstOrDefault();

            if (file != null)
            {
                var safeName = Helper.SanitizeFileName(request.Files.FileName);
                fileName = Guid.NewGuid().ToString() + safeName;
                try
                {
                    _fileUploadService.UploadImage(
                        new List<IFormFile> { file },
                        TypeSaveFile.Content,
                        fileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }

            var result = _contentCUDService.EditContent(new RequestEditContentDto
            {
                Id = request.Id,
                Description = request.Description,
                ImageFileName = fileName,
                Name = request.Name,
                MetaDescription = request.MetaDescription,
                MetaTitle = request.MetaTitle,
                ContentCategoryId = request.ContentCategoryId,
                ShortDescription = request.ShortDescription,
                ContentRelationIds = Helper.JsonDeserialize(request.ContentRelationIds),
                SlugTitle = request.SlugTitle,
                ReleaseDate = request.ReleaseDate,
                IsActive = request.IsActive,
            }, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveContent(long Id)
        {
            var result = _contentCUDService.RemoveContent(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveContentImage(long Id)
        {
            var result = _contentCUDService.RemoveContentImage(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ContentList(RequestListDto request)
        {

            var result = _contentService.GetContentList(request);
            return Json(result);

        }

        [HttpGet("{Id}")]
        public JsonResult ContentItem(long Id)
        {
            var result = _contentService.GetContentItem(Id);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ContentShare(long Id)
        {
            var result = _contentBroadcastService.PostContentToAllGroups(Id);
            return Json(result);
        }
    }
}