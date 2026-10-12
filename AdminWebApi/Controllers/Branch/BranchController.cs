using AdminWebApi.Models.ViewModels.Branch;
using Application.Services.Branches.Commands;
using Application.Services.Branches.Queries;
using Application.Services.Users.Queries;

using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using static Application.Services.Branches.Queries.BranchService;

namespace AdminWebApi.Controllers.Admin
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class BranchController : BaseController
    {
        private readonly IBranchCUDService _branchCUDService;
        private readonly IBranchService _branchService;
        private readonly IFileUploadService _fileUploadService;

        public BranchController
            (
                IUserService userService,
                IBranchCUDService branchCUDService,
                IFileUploadService fileUploadService,
                IBranchService branchService
            ) : base(userService)
        {
            _branchCUDService = branchCUDService;
            _branchService = branchService;
            _fileUploadService = fileUploadService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonResult AddBranch([FromForm] AddBranchViewModel request)
        {
            var FileName = request.Files?
                .Select(f => Guid.NewGuid().ToString() + Helper.SanitizeFileName(f.FileName))
                .ToList() ?? new List<string>();

            if (FileName.Count > 0)
            {
                try
                {
                    _fileUploadService.UploadImages(request.Files, TypeSaveFile.Branch, FileName);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }

            var result = _branchCUDService.AddBranch(new RequestAddBranchDto
            {
                Title = request.Title,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                WorkHours = request.WorkHours,
                Holidays = request.Holidays,
                Longtitude = request.Longtitude,
                Latitude = request.Latitude,
                CityId = request.CityId,
                IsActive = request.IsActive,
                ImageNames = FileName,
            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public JsonResult EditBranch([FromForm] EditBranchViewModel request)
        {
            var FileName = request.Files?
                .Select(f => Guid.NewGuid().ToString() + Helper.SanitizeFileName(f.FileName))
                .ToList() ?? new List<string>();

            if (FileName.Count > 0)
            {
                try
                {
                    _fileUploadService.UploadImages(request.Files, TypeSaveFile.Branch, FileName);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "عکس آپلود نشد: " + ex.Message });
                }
            }

            var result = _branchCUDService.EditBranch(new RequestEditBranchDto
            {
                Id = request.Id,
                Title = request.Title,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                WorkHours = request.WorkHours,
                Holidays = request.Holidays,
                Longtitude = request.Longtitude,
                Latitude = request.Latitude,
                CityId = request.CityId,
                IsActive = request.IsActive,
                ImageNames = FileName,
            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveBranch(long Id)
        {
            var result = _branchCUDService.RemoveBranch(Id, CurrentUser.Id);

            if (result.IsSuccess && result.Data?.OldImages != null)
            {
                foreach (var url in result.Data.OldImages)
                {
                    try
                    {
                        _fileUploadService.DeleteImage(url, TypeSaveFile.Branch).GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }
                }
            }

            return Json(result);
        }

        [HttpDelete("{imageId}")]
        public JsonResult RemoveBranchImage(long imageId)
        {
            var result = _branchCUDService.RemoveBranchImage(imageId, CurrentUser.Id);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.OldImageToDelete))
            {
                try
                {
                    _fileUploadService.DeleteImage(result.Data.OldImageToDelete, TypeSaveFile.Branch).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult BranchList(RequestBranchListDto request)
        {
            var result = _branchService.GetBranchList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult BranchItem(long Id)
        {
            var result = _branchService.GetBranchItem(Id);
            return Json(result);
        }
    }
}