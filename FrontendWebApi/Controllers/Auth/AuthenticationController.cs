using Application.Services.Users.Commands;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Models.ViewModels.Authentication;
using FrontendWebApi.Models.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using UAParser;

namespace EndPoint.WebService.Controllers
{
    [ApiController]
    [Route("api/[controller]/[Action]")]
    public class AuthenticationController : BaseController
    {
        private readonly IUserCUDService _userCUDService;
        private readonly IUserService _userService;

        public AuthenticationController(
            IUserCUDService userCUDService,
            IUserService userService
        ) : base(userService)
        {
            _userCUDService = userCUDService;
            _userService = userService;
        }

        [HttpPost]
        public JsonResult RegisterUser(RequestRegisterNewUserDto model)
        {
            var result = _userCUDService.RegisterNewUser(model);
            return Json(result);
        }

        [HttpPost]
        public JsonResult AcceptUserAuthCode(RequestAcceptUserAuthCodeDto model)
        {
            var result = _userCUDService.AcceptUserAuthCode(model);
            return Json(result);
        }

        [HttpPost]
        public JsonResult RequestOtp(LoginUserViewModel model)
        {
            var result = _userCUDService.GetOtpCode(model.Mobile, 0);
            return Json(result);
        }

        [HttpPost]
        public JsonResult LoginWithPassword(RequestLoginUserWithPasswordDto model)
        {
            var ua = Request.Headers["User-Agent"].ToString();
            var client = Parser.GetDefault().Parse(ua);
            var result = _userCUDService.LoginWithPassword(new LoginUserWithPasswordDto
            {
                Mobile = model.Mobile,
                Password = model.Password,
                requestDto = new RequestDto
                {
                    Browser = client?.UA.ToString() ?? "Other",
                    Ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString().Replace("::1", "127.0.0.1") ?? "",
                    Platform = client?.OS.ToString() ?? "Other",
                    Token = Helper.GetRandom(20)
                }
            }, CurrentUser.Id, CurrentUser.PublicToken);

            if (!result.IsSuccess)
                return Json(result);

            if (!IsLoginAllowed(result.Data))
                return DenyLogin(result.Data);

            return Json(result);
        }

        [HttpPost]
        public JsonResult LoginWithOtp(RequestCheckOtpCodeDto model)
        {
            var ua = Request.Headers["User-Agent"].ToString();
            var client = Parser.GetDefault().Parse(ua);
            model.requestDto = new RequestDto
            {
                Token = Helper.GetRandom(20),
                Browser = client?.UA.ToString() ?? "Other",
                Ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString().Replace("::1", "127.0.0.1") ?? "",
                Platform = client?.OS.ToString() ?? "Other",
            };

            var result = _userCUDService.CheckOtpCode(model, 0, CurrentUser.PublicToken);

            if (!result.IsSuccess)
                return Json(result);

            //if (!IsLoginAllowed(result.Data))
            //    return DenyLogin(result.Data);

            return Json(result);
        }

        [HttpPost]
        public JsonResult Logout()
        {
            var result = _userCUDService.Logout(CurrentUser.Token, CurrentUser.Id);
            return Json(result);
        }

        // Helper
        private static bool IsLoginAllowed(ResultUserLoginDto data)
        {
            var roles = data.Roles ?? new List<RoleItemDto>();

            return roles.Any(r =>
                !string.Equals(r.Name, "Coffeeshop", StringComparison.OrdinalIgnoreCase));
        }

        private JsonResult DenyLogin(ResultUserLoginDto data)
        {
            _userCUDService.Logout(data.Token, data.UserId);
            _userCUDService.LogoutUser(data.UserId, data.UserId);

            return Json(new ResultDto
            {
                IsSuccess = false,
                Message = "شما مجاز به لاگین در پنل ادمین نیستید."
            });
        }
    }
}