using Application.Services.Payments.Queries;
using Application.Services.Users.Queries;
using EndPoint.WebService.Controllers;
using EndPoint.WebService.Models.ViewModels.Base;
using Microsoft.AspNetCore.Mvc;

namespace FrontendWebApi.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _paymentService;

        public PaymentController
            (IUserService userService,
            IPaymentService paymentService) : base(userService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public JsonResult InitiateSep()
        {
            var result = _paymentService.InitiatePaymentSep(CurrentUser.Id, null);
            return Json(result);
        }
    }
}