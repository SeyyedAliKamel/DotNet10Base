using Application.Services.AdditionalInformations.Queries;
using Application.Services.Products.Commands;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.AdditionalInformations;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.AdditionalInformation
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
        public JsonResult AddOrUpdateReview(RequestAddOrUpdateReviewFrontEndDto request)
        {
            var result = _reviewCUDService.AddOrUpdateReviewFrontEnd(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditReview(RequestEditReviewFrontEndDto request)
        {
            var result = _reviewCUDService.EditReviewFrontEnd(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{id}")]
        public JsonResult RemoveReview(long id)
        {
            var result = _reviewCUDService.RemoveReviewFrontEnd(id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult UserReviews(RequestListDto request)
        {
            var result = _reviewService.GetReviewListFrontEnd(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult UserReviewBasket(long basketDetailId)
        {
            var result = _reviewService.GetReviewUserProduct(basketDetailId, CurrentUser.Id);
            return Json(result);
        }
    }
}