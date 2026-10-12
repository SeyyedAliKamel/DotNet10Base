using AdminWebApi.Models.ViewModels.Additional;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
namespace AdminWebApi.Controllers.AdditionalInformation
{
    /// <summary>
    /// Handles operations related to banners.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class BannerController : BaseController
    {
        private readonly IBannerCUDService _bannerCUDService;
        private readonly IBannerService _bannerService;
        private readonly IFileUploadService _fileUploadService;
        public BannerController(
            IBannerCUDService bannerCUDService,
            IBannerService bannerService,
            IFileUploadService fileUploadService,
            IUserService userService
        ) : base(userService)
        {
            _bannerCUDService = bannerCUDService;
            _bannerService = bannerService;
            _fileUploadService = fileUploadService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonResult AddBanner([FromForm] AddBannerViewModel request)
        {
            string? fileName = null;
            if (request.File != null)
            {
                fileName = Guid.NewGuid().ToString() + request.File.FileName;
                try
                {
                    _fileUploadService.UploadImage(
                        new List<IFormFile> { request.File },
                        TypeSaveFile.Banner,
                        fileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Image upload failed: " + ex.Message });
                }
            }

            var result = _bannerCUDService.AddBanner(new RequestAddBannerDto
            {
                Title = request.Title,
                AdminTitle = request.AdminTitle,
                Position = request.Position,
                ImageUrl = fileName,
                Url = request.Url,
                IsActive = request.IsActive
            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public JsonResult EditBanner([FromForm] EditBannerViewModel request)
        {
            string fileName = null;
            if (request.File != null)
            {
                fileName = Guid.NewGuid().ToString() + request.File.FileName;
                try
                {
                    _fileUploadService.UploadImage(
                        new List<IFormFile> { request.File },
                        TypeSaveFile.Banner,
                        fileName)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Image upload failed: " + ex.Message });
                }
            }

            var result = _bannerCUDService.EditBanner(new RequestEditBannerDto
            {
                Id = request.Id,
                Title = request.Title,
                AdminTitle = request.AdminTitle,
                Position = request.Position,
                ImageUrl = fileName,
                Url = request.Url,
                IsActive = request.IsActive
            }, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.Banner).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveBanner(int id)
        {
            var result = _bannerCUDService.RemoveBanner(id, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.Banner).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveBannerImage(int id)
        {
            var result = _bannerCUDService.RemoveBannerImage(id, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImage))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImage, TypeSaveFile.Banner).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult BannerList(RequestListDto request)
        {
            var result = _bannerService.GetBannerList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult BannerItem(int Id)
        {
            var result = _bannerService.GetBannerItem(Id);
            return Json(result);
        }
    }
}