using Application.Services.Baskets.Queries;
using Application.Services.Scores.Commands;
using Application.Services.Scores.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Controllers;
using EndPoint.WebService.Models.ViewModels.Base;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ScoreController : BaseController
    {
        private readonly IScoreCUDService _scoreCUDService;
        private readonly IScoreService _scoreService;
        private readonly IBasketService _basketService;

        public ScoreController(
            IScoreCUDService scoreCUDService,
            IScoreService scoreService,
            IBasketService basketService,
            IUserService userService
        ) : base(userService)
        {
            _scoreCUDService = scoreCUDService;
            _scoreService = scoreService;
            _basketService = basketService;
        }

        [HttpPost]
        public JsonResult ScoreList(RequestListDto request)
        {
            var result = _scoreService.GetScoreList(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpGet]
        public JsonResult ScoreTotal()
        {
            var result = _scoreService.GetScoreTotal(CurrentUser.Id);
            return Json(result);
        }

        [HttpGet]
        public JsonResult ScoreDailyCheck()
        {
            var result = _scoreService.CheckLoginScores(CurrentUser.Id);
            return Json(result);
        }
    }
}