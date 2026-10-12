using Application.Services.Users.Commands;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using EndPoint.WebService.Models.ViewModels.Authentication;
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

        // ==================== Actions ====================

        [HttpPost]
        public JsonResult LoginUser(LoginUserViewModel model)
        {
            var result = _userCUDService.LoginWithPassword(new LoginUserWithPasswordDto
            {
                Mobile = model.Mobile,
                Password = model.Password,
                requestDto = BuildRequestDto()
            }, CurrentUser.Id, "");

            if (!result.IsSuccess)
                return Json(result);

            if (!IsLoginAllowed(result.Data))
                return DenyLogin(result.Data);

            return LoginSuccess(result);
        }

        [HttpPost]
        public JsonResult LoginUserWithMobile(LoginUserViewModel model)
        {
            var result = _userCUDService.GetOtpCode(model.Mobile, 0);
            return Json(result);
        }

        [HttpPost]
        public JsonResult CheckOtpCode(RequestCheckOtpCodeDto model)
        {
            model.requestDto = BuildRequestDto();

            var result = _userCUDService.CheckOtpCode(model, 0, "");

            if (!result.IsSuccess || result.Data == null)
                return Json(new ResultDto { IsSuccess = false, Message = result.Message ?? "OTP validation failed" });

            if (!IsLoginAllowed(result.Data))
                return DenyLogin(result.Data);

            return LoginSuccess(result);
        }

        [HttpPost]
        public JsonResult Logout()
        {
            var result = _userCUDService.Logout(CurrentUser.Token, CurrentUser.Id);
            return Json(result);
        }

        // ==================== Private helpers ====================

        // Builds the session info (browser / IP / platform / new token) from the current request
        private RequestDto BuildRequestDto()
        {
            var ua = Request.Headers["User-Agent"].ToString();
            var client = Parser.GetDefault().Parse(ua);

            return new RequestDto
            {
                Browser = client?.UA.ToString() ?? "Other",
                Ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString().Replace("::1", "127.0.0.1") ?? "",
                Platform = client?.OS.ToString() ?? "Other",
                Token = Helper.GetRandom(20)
            };
        }

        // Allowed only if the user has at least one role and none of them is Coffeeshop
        //private static bool IsLoginAllowed(ResultUserLoginDto data)
        //{
        //    var roles = data.Roles ?? new List<RoleItemDto>();

        //    var hasCoffeeshopRole = roles.Any(r =>
        //        string.Equals(r.Name, "Coffeeshop", StringComparison.OrdinalIgnoreCase));

        //    return roles.Count > 0 && !hasCoffeeshopRole;
        //}

        private static bool IsLoginAllowed(ResultUserLoginDto data)
        {
            return true;
        }

        // The service already created the session, so revoke it and return the rejection
        private JsonResult DenyLogin(ResultUserLoginDto data)
        {
            _userCUDService.Logout(data.Token, data.UserId);

            return Json(new ResultDto
            {
                IsSuccess = false,
                Message = "You are not allowed to log in."
            });
        }

        // Shared success response: login info + the user's permissions
        private JsonResult LoginSuccess(ResultDto<ResultUserLoginDto> result)
        {
            var userPermission = _userService.GetUserRolePermissions(result.Data.UserId, CurrentUser?.Id ?? 0);

            return Json(new
            {
                userInfo = result,
                userPermission = userPermission.Data
            });
        }
    }
}