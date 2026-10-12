using Application.Services.AdditionalInformations.Commands;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Users.Queries;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;
namespace AdminWebApi.Controllers.AdditionalInformations
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ContactUsController : BaseController
    {
        private readonly IContactUsCUDService _contactUsCUDService;
        private readonly IContactUsService _contactUsService;
        public ContactUsController(
            IContactUsCUDService contactUsCUDService,
            IContactUsService contactUsService,
            IUserService userService
        ) : base(userService)
        {
            _contactUsCUDService = contactUsCUDService;
            _contactUsService = contactUsService;
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveContactUs(long Id)
        {
            var result = _contactUsCUDService.RemoveContactUs(Id, CurrentUser.Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ContactUsList(RequestListDto request)
        {
            var result = _contactUsService.GetContactUsList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult ContactUsItem(long Id)
        {
            var result = _contactUsService.GetContactUsItem(Id);
            return Json(result);
        }
    }
}