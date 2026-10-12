using Application.Services.Baskets.Commands;
using Application.Services.Baskets.Queries;
using Application.Services.UserAddresses.Commands;
using Application.Services.UserAddresses.Queries;
using Application.Services.Users.Commands;
using Application.Services.Users.Queries;
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
        private readonly IBasketCUDService _basketCUDService;
        private readonly IBasketService _basketService;
        private readonly IUserAddressCUDService _userAddressCUDService;
        private readonly IUserAddressService _userAddressService;

        public UserController(
            IUserCUDService userCUDService,
            IUserService userService,
            IBasketCUDService basketCUDService,
            IBasketService basketService,
            IUserAddressCUDService userAddressCUDService,
            IUserAddressService userAddressService
        ) : base(userService)
        {
            _userCUDService = userCUDService;
            _userService = userService;
            _basketCUDService = basketCUDService;
            _basketService = basketService;
            _userAddressCUDService = userAddressCUDService;
            _userAddressService = userAddressService;
        }



        [HttpGet]
        public JsonResult GetUserInfo()
        {
            var result = _userService.GetUserInfo(CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult EditUserPassword(RequestChangePasswordByCurrentUserDto request)
        {
            var result = _userCUDService.ChangePasswordByCurrentUser(request, CurrentUser.Id);
            return Json(result);
        }


        [HttpPost]
        public JsonResult UpdateUserInfo(RequestUpdateUserInfoDto model)
        {
            var result = _userCUDService.UpdateUserInfo(model, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult AddUserAddress(UserAddressRequestDto request)
        {
            var result = _userAddressCUDService.AddUserAddress(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost("{Id}")]
        public JsonResult EditUserAddress(long Id, UserAddressRequestDto request)
        {
            var result = _userAddressCUDService.EditUserAddress(Id, request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveUserAddress(long Id)
        {
            var result = _userAddressCUDService.RemoveUserAddress(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult UserAddressList(RequestUserAddressDto request)
        {
            var result = _userAddressService.GetUserAddressListFrontEnd(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ChangeInvoiceAddress(long Id)
        {
            var result = _basketCUDService.ChangeInvoiceAddress(Id, CurrentUser.Id);
            return Json(result);
        }


        [HttpGet]
        public JsonResult GetCurrentBasketAddressId()
        {
            var result = _basketService.GetCurrentBasketAddressId(CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult BasketPayment(RequestBasketPeymentDto request)
        {
            var result = _basketCUDService.BasketPayment(request, CurrentUser.Id).Result;
            return Json(result);
        }

        [HttpGet]
        public JsonResult RefferedUsers()
        {
            var result = _userService.GetReferredUsers(CurrentUser.Id);
            return Json(result);
        }
    }
}