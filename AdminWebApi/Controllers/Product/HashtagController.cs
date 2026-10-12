using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Application.Services.Users.Queries;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers.Product
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HashtagController : BaseController
    {
        private readonly IHashtagCUDService _hashtagCUDService;
        private readonly IHashtagService _hashtagService;

        public HashtagController(
            IHashtagCUDService hashtagCUDService,
            IHashtagService hashtagService,
            IUserService userService
        ) : base(userService)
        {
            _hashtagCUDService = hashtagCUDService;
            _hashtagService = hashtagService;
        }


        
        [HttpPost]
        public JsonResult AddHashtag(HashtagRequestDto request)
        {
            var result = _hashtagCUDService.AddHashtag(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditHashtag(int id, HashtagRequestDto request)
        {
            var result = _hashtagCUDService.EditHashtag(id, request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveHashtag(int id)
        {
            var result = _hashtagCUDService.RemoveHashtag(id, CurrentUser.Id);
            return Json(result);
        }


        [HttpPost]
        public JsonResult HashtagList(HashtagListRequestDto request)
        {
            var result = _hashtagService.GetHashtagList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult HashtagItem(int id)
        {
            var result = _hashtagService.GetHashtagItem(id);
            return Json(result);
        }
    }
}