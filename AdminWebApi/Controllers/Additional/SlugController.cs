using AdminWebApi.Models.ViewModels.Additional;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
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
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SlugController : BaseController
    {
        private readonly ISlugCUDService _slugCUDService;
        private readonly ISlugService _slugService;
        private readonly IFileUploadService _fileUploadService;

        public SlugController(
            ISlugCUDService slugCUDService,
            ISlugService slugService,
            IFileUploadService fileUploadService,
            IUserService userService
        ) : base(userService)
        {
            _slugCUDService = slugCUDService;
            _slugService = slugService;
            _fileUploadService = fileUploadService;
        }

        /// <summary>
        /// Slug type values:
        /// 0 = Product
        /// 1 = Content
        /// 2 = ProductCategory
        /// 3 = ContentCategory
        /// </summary>
        [HttpPost]
        public JsonResult AddSlug(RequestAddSlugDto request)
        {
            var result = _slugCUDService.AddSlug(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditSlug(RequestEditSlugDto request)
        {
            var result = _slugCUDService.EditSlug(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveSlug(int id)
        {
            var result = _slugCUDService.RemoveSlug(id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult SlugList(RequestListDto request)
        {
            var result = _slugService.GetSlugList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult SlugItem(int Id)
        {
            var result = _slugService.GetSlugItem(Id);
            return Json(result);
        }
    }
}
