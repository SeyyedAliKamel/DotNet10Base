using Application.Services.Scores.Commands;
using Application.Services.Scores.Queries;
using Application.Services.Users.Queries;
using Microsoft.AspNetCore.Mvc;
using EndPoint.WebService.Controllers;
using Common.Dto;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ScoreController : BaseController
    {
        private readonly IScoreCUDService _scoreCUDService;
        private readonly IScoreService _scoreService;

        public ScoreController(
            IScoreCUDService scoreCUDService,
            IScoreService scoreService,
            IUserService userService
        ) : base(userService)
        {
            _scoreCUDService = scoreCUDService;
            _scoreService = scoreService;
        }



        [HttpPost]
        public JsonResult AddScore(RequestAddScoreDto request)
        {
            var result = _scoreCUDService.AddScore(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditScore( RequestEditScoreDto request)
        {
            var result = _scoreCUDService.EditScore( request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveScore(int Id)
        {
            var result = _scoreCUDService.RemoveScore(Id, CurrentUser.Id);
            return Json(result);
        }



        [HttpPost]
        public JsonResult ScoreList(RequestListDto request)
        {
            var result = _scoreService.GetScoreList(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ScoreItem(int Id)
        {
            var result =  _scoreService.GetScoreItem(Id);
            return Json(result);
        }
    }
}