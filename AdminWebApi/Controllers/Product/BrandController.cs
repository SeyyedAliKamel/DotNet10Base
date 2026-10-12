using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Application.Services.Users.Queries;
using Common.Dto;
using EndPoint.WebService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AdminWebApi.Controllers.Product
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class BrandController : BaseController
    {
        private readonly IBrandCUDService _brandCUDService;
        private readonly IBrandService _brandService;

        public BrandController(
            IBrandCUDService brandCUDService,
            IBrandService brandService,
            IUserService userService
        ) : base(userService)
        {
            _brandCUDService = brandCUDService;
            _brandService = brandService;
        }



        [HttpPost]
        public JsonResult AddBrand(RequestAddBrandDto request)
        {
            var result = _brandCUDService.AddBrand(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpPut]
        public JsonResult EditBrand( RequestEditBrandDto request)
        {
            var result = _brandCUDService.EditBrand(request, CurrentUser.Id);
            return Json(result);
        }

        [HttpDelete("{Id}")]
        public JsonResult RemoveBrand(int id)
        {
            var result = _brandCUDService.RemoveBrand(id, CurrentUser.Id);
            return Json(result);
        }



        [HttpPost]
        public JsonResult BrandList(RequestListDto request)
        {
            var result =  _brandService.GetBrandList(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult BrandItem(int Id)
        {
            var result = _brandService.GetBrandItem(Id);
            return Json(result);
        }
    }
}