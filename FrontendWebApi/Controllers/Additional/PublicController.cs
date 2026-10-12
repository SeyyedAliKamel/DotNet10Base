using Application.ConnectedServices;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Branches.Queries;
using Application.Services.Contents.Queries;
using Application.Services.Payments.Queries;
using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Application.Services.Search.Queries;
using Application.Services.Users.Queries;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using EndPoint.WebService.Controllers;
using FrontendWebApi.Models.ViewModels.Common;
using FrontendWebApi.Models.ViewModels.Product;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static Application.Services.Branches.Queries.BranchService;
namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PublicController : BaseController
    {
        private readonly IProductService _productService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IBrandService _brandService;
        private readonly IContentService _contentService;
        private readonly IContentCategoryService _contentCategoryService;
        private readonly IBannerService _bannerService;
        private readonly IBranchService _branchService;
        private readonly ISlugService _slugService;
        private readonly IReviewService _reviewService;
        private readonly IPaymentService _paymentService;
        private readonly IContactUsCUDService _contactUsCUDService;
        private readonly IReviewCUDService _reviewCUDService;
        private readonly ISearchService _searchService;
        private readonly IGeoService _geoService;
        public PublicController(
            IProductService productService,
            IProductCategoryService productCategoryService,
            IBrandService brandService,
            IContentService contentService,
            IContentCategoryService contentCategoryService,
            IBannerService bannerService,
            IBranchService branchService,
            ISlugService slugService,
            IGeoService geoService,
            IReviewService reviewService,
            IPaymentService paymentService,
            IContactUsCUDService contactUsCUDSerivce,
            IReviewCUDService reviewCUDService,
            ISearchService searchService,
            IUserService userService
        ) : base(userService)
        {
            _productService = productService;
            _productCategoryService = productCategoryService;
            _brandService = brandService;
            _contentService = contentService;
            _contentCategoryService = contentCategoryService;
            _bannerService = bannerService;
            _branchService = branchService;
            _slugService = slugService;
            _geoService = geoService;
            _reviewService = reviewService;
            _paymentService = paymentService;
            _contactUsCUDService = contactUsCUDSerivce;
            _reviewCUDService = reviewCUDService;
            _searchService = searchService;
        }



        // Product
        [HttpPost]
        public JsonResult ProductListSlider(ProductListByCategoryRequestDto request)
        {
            var result = _productService.GetProductListByCategory(request);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ProductList(ProductListRequestDto request)
        {
            var result = _productService.GetProductListFrontEnd(request);
            return Json(result);
        }

        [HttpGet("{slug}")]
        public JsonResult ProductItem(string slug)
        {
            var slugResult = _slugService.GetSlug(slug);

            if (!slugResult.IsSuccess || slugResult.Data == null)
            {
                return Json(slugResult);
            }

            var result = _productService.GetProductItemFrontEnd(
                slugResult.Data.ReferenceId,
                CurrentUser.Id,
                CurrentUser.PublicToken
            );

            return Json(result);
        }

        [HttpPost]
        public JsonResult ProductCategoryList(RequestListDto request)
        {
            var result = _productCategoryService.GetProductCategoryListFrontEnd(new ProductCategoryListRequestDto
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Sort = request.Sort,
                Filter = request.Filter
            });
            return Json(result);
        }

        [HttpGet("{slug}")]
        public JsonResult ProductCategoryItem(string slug)
        {
            var slugResult = _slugService.GetSlug(slug);

            if (!slugResult.IsSuccess || slugResult.Data == null)
            {
                return Json(slugResult);
            }

            var result = _productCategoryService.GetProductCategoryItemFrontEnd(
                slugResult.Data.ReferenceId
            );

            return Json(result);
        }

        [HttpPost]
        public JsonResult BrandList(RequestListDto request)
        {
            var result = _brandService.GetBrandListFrontEnd(request);
            return Json(result);
        }


        // Content
        [HttpPost]
        public JsonResult ContentList(RequestListFrontendDto request)
        {
            var result = _contentService.GetContentListFrontEnd(request);
            return Json(result);
        }

        [HttpGet("{slug}")]
        public JsonResult ContentItem(string slug)
        {
            var slugResult = _slugService.GetSlug(slug);

            if (!slugResult.IsSuccess || slugResult.Data == null)
            {
                return Json(slugResult);
            }

            var result = _contentService.GetContentItemFrontEnd(
                slugResult.Data.ReferenceId
            );

            return Json(result);
        }

        [HttpPost]
        public JsonResult ContentCategoryList(RequestListDto request)
        {
            var result = _contentCategoryService.GetContentCategoryListFrontEnd(request);
            return Json(result);
        }

        [HttpGet("{slug}")]
        public JsonResult ContentCategoryItem(string slug)
        {
            var slugResult = _slugService.GetSlug(slug);

            if (!slugResult.IsSuccess || slugResult.Data == null)
            {
                return Json(slugResult);
            }

            var result = _contentCategoryService.GetContentCategoryItemFrontEnd(
                slugResult.Data.ReferenceId
            );

            return Json(result);
        }



        // Banner
        [HttpPost]
        public JsonResult BannerList([FromForm] RequestBannerByPositionDto requestBannerByPositionDto)
        {
            var result = _bannerService.GetBannerListByPosition(requestBannerByPositionDto);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult BannerItem(long Id)
        {
            var result = _bannerService.GetBannerItemFrontEnd(Id);
            return Json(result);
        }



        // Geo
        [HttpPost]
        public JsonResult StateList(RequestListDto request)
        {
            var result = _geoService.GetStateListFrontEnd(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult StateItem(long Id)
        {
            var result = _geoService.GetStateItemFrontEnd(Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult CityList(RequestListDto request)
        {
            var result = _geoService.GetCityListFrontEnd(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult CityItem(long Id)
        {
            var result = _geoService.GetCityItemFrontEnd(Id);
            return Json(result);
        }

        [HttpGet]
        public JsonResult AllStatesWithCities()
        {
            var result = _geoService.GetAllGeoFrontEnd();
            return Json(result);
        }



        // Branch
        [HttpPost]
        public JsonResult BranchList(RequestBranchListDto request)
        {
            var result = _branchService.GetBranchListFrontEnd(request);
            return Json(result);
        }

        [HttpGet("{Id}")]
        public JsonResult BranchItem(long Id)
        {
            var result = _branchService.GetBranchItemFrontEnd(Id);
            return Json(result);
        }

        [HttpPost]
        public JsonResult BranchNear(long? CityId, decimal? Long, decimal? Lati)
        {
            var result = _branchService.GetBranchNearMeList(CityId, Long, Lati);
            return Json(result);
        }

        // Review
        [HttpPost]
        public JsonResult Reviews(RequestReviewListByTypeDto request) 
        {
            var result = _reviewService.GetReviewListByTypeFrontEnd(request);
            return Json(result);
        }


        [HttpGet("{Slug}")]
        public JsonResult SlugCheck(string Slug)
        {
            var result = _slugService.GetSlug(Slug);
            return Json(result);
        }

        // ContactUs
        [HttpPost]
        public JsonResult ContactUs([FromForm] RequestAddContactUsDto request)
        {
            var result = _contactUsCUDService.AddContactUs(request, CurrentUser.Id);
            return Json(result);
        }

        // Search
        [HttpPost]
        public JsonResult Search(SearchRequestDto request)
        {
            var result = _searchService.Search(request);
            return Json(result);
        }

        // Review
        [HttpPost]
        public JsonResult AddReview(RequestAddReviewFrontEndDto request)
        {
            var command = new RequestAddReviewDto
            {
                ReferenceId = request.ReferenceId,
                Description = request.Description,
                Type = request.Type,
                IsActive = false,
                FullName = request.FullName
            };

            var result = _reviewCUDService.AddReview(command, CurrentUser.Id);
            return Json(result);
        }
    }
}