using Application.Services.Users;
using Application.Services.Users.Commands;
using Application.Services.Users.Queries;
using Common;
using EndPoint.WebService.Models.ViewModels.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace EndPoint.WebService.Controllers
{
    public class BaseController : Controller
    {
        private readonly IUserService _userService;

        public BaseController(IUserService userService)
        {
            _userService = userService;
        }

        protected CurrentUser? CurrentUser = new CurrentUser();
        protected string Controller = null!;
        protected string Action = null!;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            Request.Cookies.TryGetValue("PublicToken", out var publicToken);
            CurrentUser.PublicToken = publicToken;

            Action = ControllerContext.ActionDescriptor.ActionName;
            Controller = ControllerContext.ActionDescriptor.ControllerName;

            var rawHeader = Request.Headers[HeaderNames.Authorization].ToString();
            var token = rawHeader.Replace("Bearer", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (!string.IsNullOrEmpty(token))
            {
                var data = _userService.GetCurrentUser(new RequestCurrentUserDto
                {
                    Token = token
                }, CurrentUser.Id);

                if (data.Data != null)
                {
                    CurrentUser.FirstName = data.Data.FirstName;
                    CurrentUser.LastName = data.Data.LastName;
                    CurrentUser.Id = data.Data.Id;
                    CurrentUser.Mobile = data.Data.Mobile;
                    CurrentUser.Token = token;
                }
            }

            if (Controller != "Authentication" && Controller != "Common" && Controller != "Public" && Controller != "Basket")
            {
                if (CurrentUser.Id == 0)
                {
                    Response.StatusCode = 401;
                    filterContext.Result = new JsonResult(new JsonResultViewModel
                    {
                        Result = "Not Logged In",
                        Status = ResultStatus.Unauthorized,
                    });
                    return;
                }
            }

            if (Controller != "Public" && filterContext.CheckModel() is { } checkResult)
            {
                filterContext.Result = new JsonResult(new JsonResultViewModel
                {
                    Status = ResultStatus.Failed,
                    Result = checkResult
                });
            }
        }
    }
}