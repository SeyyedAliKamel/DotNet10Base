using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Microsoft.Extensions.Configuration;
using static Application.Services.Contents.Queries.ContentCategoryService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.Services.Contents.Queries
{
    public interface IContentCategoryService
    {
        ResultDto<ResultContentCategoryItemDto> GetContentCategoryItem(long Id);
        ResultDto<List<ResultContentCategoryListDto>> GetContentCategoryList(RequestListDto request);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<ResultContentCategoryListDto>> GetContentCategoryListFrontEnd(RequestListDto request);
        ResultDto<ResultContentCategoryItemDto> GetContentCategoryItemFrontEnd(long Id);
    }


    public class ContentCategoryService : IContentCategoryService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public ContentCategoryService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ResultDto<ResultContentCategoryItemDto> GetContentCategoryItem(long Id)
        {
            var item = _context.ContentCategories.Where(p => p.Id == Id).Select(p => new ResultContentCategoryItemDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                Description = p.Description,
                Image = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.ContentCategory
                             && i.ImageRole == ImageRoleEnum.Single
                             && i.ReferenceId == p.Id)
                    .Select(i => new ResultImageDto
                    {
                        Id = i.Id,
                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ContentCategory/" + i.ImageUrl,
                    })
                    .ToList(),
                MetaTitle = p.MetaTitle,
                MetaDescription = p.MetaDescription,
                Slug = _context.Slugs
                        .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.ContentCategory)
                        .Select(s => s.Title)
                        .FirstOrDefault(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            }).FirstOrDefault();

            if (item == null)
            {
                return new ResultDto<ResultContentCategoryItemDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Message = "اطلاعات یافت نشد",
                    Status = 404
                };
            }

            item.ImageId = item.Image?.FirstOrDefault()?.Id;

            return new ResultDto<ResultContentCategoryItemDto>
            {
                Data = item,
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ResultContentCategoryItemDto> GetContentCategoryItemFrontEnd(long Id)
        {
            var item = _context.ContentCategories.Where(p => p.Id == Id && p.IsActive).Select(p => new ResultContentCategoryItemDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                Description = p.Description,
                Image = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.ContentCategory
                             && i.ImageRole == ImageRoleEnum.Single
                             && i.ReferenceId == p.Id)
                    .Select(i => new ResultImageDto
                    {
                        Id = i.Id,
                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ContentCategory/" + i.ImageUrl,
                    })
                    .ToList(),
                MetaTitle = p.MetaTitle,
                MetaDescription = p.MetaDescription,
                Slug = _context.Slugs
                        .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.ContentCategory)
                        .Select(s => s.Title)
                        .FirstOrDefault(),
            }).FirstOrDefault();

            if (item == null)
            {
                return new ResultDto<ResultContentCategoryItemDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Message = "اطلاعات یافت نشد",
                    Status = 404
                };
            }

            item.ImageId = item.Image?.FirstOrDefault()?.Id;

            return new ResultDto<ResultContentCategoryItemDto>
            {
                Data = item,
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        public ResultDto<List<ResultContentCategoryListDto>> GetContentCategoryList(RequestListDto request)
        {
            var ContentCategories = _context.ContentCategories.AsQueryable();

            ContentCategories = ContentCategories.ApplySortingAndFiltering(request.Sort, request.Filter);

            if (string.IsNullOrWhiteSpace(request.Sort))
            {
                ContentCategories = ContentCategories.OrderByDescending(s => s.CreatedAt);
            }

            var rowsCount = 0;
            var contentCategoryList = ContentCategories
                .ToPaged(request.Page, request.PageSize, out rowsCount)
                .Select(p => new ResultContentCategoryListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsActive = p.IsActive,
                    Description = p.Description,
                    Image = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.ContentCategory
                                 && i.ImageRole == ImageRoleEnum.Single
                                 && i.ReferenceId == p.Id)
                        .Select(i => new ResultImageDto
                        {
                            Id = i.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ContentCategory/" + i.ImageUrl,
                        })
                        .ToList(),
                    MetaTitle = p.MetaTitle,
                    MetaDescription = p.MetaDescription,
                    Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.ContentCategory)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                })
                .ToList();

            foreach (var category in contentCategoryList)
            {
                category.ImageId = category.Image?.FirstOrDefault()?.Id;
            }

            return new ResultDto<List<ResultContentCategoryListDto>>
            {
                Data = contentCategoryList,
                IsSuccess = true,
                Message = "اطلاعات با موفقیت ارسال گردید.",
                Status = 200,
                Result = new ResultListDto()
                {
                    Current = request.Page,
                    Count = rowsCount
                }
            };
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<ResultContentCategoryListDto>> GetContentCategoryListFrontEnd(RequestListDto request)
        {
            var ContentCategories = _context.ContentCategories.Where(p => p.IsActive).AsQueryable();

            ContentCategories = ContentCategories.ApplySortingAndFiltering(request.Sort, request.Filter);

            if (string.IsNullOrWhiteSpace(request.Sort))
            {
                ContentCategories = ContentCategories.OrderBy(s => s.Order);
            }

            var rowsCount = 0;
            var contentCategoryList = ContentCategories
                .ToPaged(request.Page, request.PageSize, out rowsCount)
                .Select(p => new ResultContentCategoryListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsActive = p.IsActive,
                    Description = p.Description,
                    Image = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.ContentCategory
                                 && i.ImageRole == ImageRoleEnum.Single
                                 && i.ReferenceId == p.Id)
                        .Select(i => new ResultImageDto
                        {
                            Id = i.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ContentCategory/" + i.ImageUrl,
                        })
                        .ToList(),
                    MetaTitle = p.MetaTitle,
                    MetaDescription = p.MetaDescription,
                    Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.ContentCategory)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                })
                .ToList();

            foreach (var category in contentCategoryList)
            {
                category.ImageId = category.Image?.FirstOrDefault()?.Id;
            }

            return new ResultDto<List<ResultContentCategoryListDto>>
            {
                Data = contentCategoryList,
                IsSuccess = true,
                Message = "اطلاعات با موفقیت ارسال گردید.",
                Status = 200,
                Result = new ResultListDto()
                {
                    Current = request.Page,
                    Count = rowsCount
                }
            };
        }

        public class ResultContentCategoryItemDto
        {
            public long Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public List<ResultImageDto>? Image { get; set; }
            public long? ImageId { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? Slug { get; set; }
            public bool IsActive { get; set; }
            public bool IsDeleted { get; internal set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class ResultContentCategoryListDto
        {
            public long Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public List<ResultImageDto>? Image { get; set; }
            public long? ImageId { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string Slug { get; set; }
            public bool IsActive { get; set; }
            public bool IsDeleted { get; internal set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }
    }

}