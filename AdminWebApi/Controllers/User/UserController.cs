using Application.Services.Users.Commands;
using Application.Services.Users.Queries;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController : BaseController
    {
        private readonly IUserCUDService _userCUDService;
        private readonly IUserService _userService;
        public UserController(
            IUserCUDService userCUDService,
            IUserService userService
        ) : base(userService)
        {
            _userCUDService = userCUDService;
            _userService = userService;
        }



        [HttpPost]
        public JsonResult AddUser(RequestAddUserDto request)
        {
            var result = _userCUDService.AddUser(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditUser(RequestEditUserDto request)
        {
            var result = _userCUDService.EditUser(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveUser(int id)
        {
            var result =  _userCUDService.RemoveUser(id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult UserList(RequestGetUserDto request)
        {
            var result = _userService.GetUserList(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult UserItem(long Id)
        {
            var result = _userService.GetUserItem(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult UserInfo()
        {

            var result = _userService.GetUserInfo(CurrentUser.Id);
            if (result.IsSuccess == false)
            {
                return Json(result);
            }

            var userPermission = _userService.GetUserRolePermissions(CurrentUser.Id, CurrentUser.Id);
            if (result.IsSuccess == true)
            {
                return Json(new
                {
                    userInfo = result,
                    userPermission = userPermission.Data
                });
            }
            else
            {
                return Json(new ResultDto { IsSuccess = false, Message = "Please provide all required fields." });
            }
        }

        [HttpPost]
        public JsonResult ChangePasswordByCurrentUser(RequestChangePasswordByCurrentUserDto request)
        {
            var result = _userCUDService.ChangePasswordByCurrentUser(new RequestChangePasswordByCurrentUserDto
            {
                Password = request.Password,
                RepeatPassword = request.RepeatPassword,

            }, CurrentUser.Id);

            return Json(result);
        }

        [HttpPost]
        public JsonResult ChangePasswordByUser(RequestChangePasswordByUserDto request)
        {
            var result = _userCUDService.ChangePasswordByUser(new RequestChangePasswordByUserDto
            {
                UserId = request.UserId,
                Password = request.Password,
                RepeatPassword = request.RepeatPassword,

            }, CurrentUser.Id);

            return Json(result);
        }

    }
}