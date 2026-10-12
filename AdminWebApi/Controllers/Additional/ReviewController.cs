using AdminWebApi.Models.ViewModels.Additional;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Products.Commands;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace AdminWebApi.Controllers.AdditionalInformation
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ReviewController : BaseController
    {
        private readonly IReviewCUDService _reviewCUDService;
        private readonly IReviewService _reviewService;

        public ReviewController(
            IReviewCUDService reviewCUDService,
            IReviewService reviewService,
            IUserService userService
        ) : base(userService)
        {
            _reviewCUDService = reviewCUDService;
            _reviewService = reviewService;
        }

        [HttpPost]
        public JsonResult AddReview(RequestAddReviewDto request)
        {
            var result = _reviewCUDService.AddReview(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditReview(RequestEditReviewDto request)
        {
            var result = _reviewCUDService.EditReview(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveReview(long id)
        {
            var result = _reviewCUDService.RemoveReview(id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ReviewList(RequestListDto request)
        {
            var result = _reviewService.GetReviewList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ReviewItem(long Id)
        {
            var result = _reviewService.GetReviewItem(Id);
            return Json(result);
        }
    }
}