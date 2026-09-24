using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Contents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using static Application.Services.Contents.Queries.ContentService;

namespace Application.Services.Contents.Queries
{
    public interface IContentService
    {
        ResultDto<ResultContentItemDto> GetContentItem(long Id);
        ResultDto<List<ResultContentListDto>> GetContentList(RequestListDto request);
        ResultDto<List<ResultContentListDto>> Get4LastContent();

        // -------- Frontend-facing (active & non-deleted only) --------
        ResultDto<ResultContentItemDto> GetContentItemFrontEnd(long Id);
        ResultDto<List<ResultContentListDto>> GetContentListFrontEnd(RequestListFrontendDto request,long categoryId);
    }

    public class ContentService : IContentService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        private const double WordsPerMinute = 200;

        public ContentService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ResultDto<ResultContentItemDto> GetContentItem(long Id)
        {
            var item = _context.Contents.Where(p => p.Id == Id).Select(p => new ResultContentItemDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                Description = p.Description,
                ShortDescription = p.ShortDescription,
                MetaTitle = p.MetaTitle,
                MetaDescription = p.MetaDescription,
                Image = _context.Images
                    .Where(i => i.ImageType == ImageTypeEnum.Content
                             && i.ImageRole == ImageRoleEnum.Single
                             && i.ReferenceId == p.Id)
                    .Select(i => new ResultImageDto
                    {
                        Id = i.Id,
                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Content/" + i.ImageUrl,
                    })
                    .ToList(),
                ContentCategory = _context.ContentCategories
                    .IgnoreQueryFilters()
                    .Where(cc => cc.Id == p.ContentCategoryId)
                    .FirstOrDefault(),
                ContentCategoryId = (long)p.ContentCategoryId,
                CreatedBy = p.CreatedBy == null ? null : p.CreatedBy.FirstName + " " + p.CreatedBy.LastName,
                Relations = _context.ContentRelations
                    .Where(r => r.ContentId == p.Id)
                    .Select(r => new ContentRelationItemDto
                    {
                        Id = r.Id,
                        ContentId = r.ContentId,
                        RelatedContentId = r.RelatedContentId,
                        RelatedContentName = r.RelatedContent.Name,
                        RelatedContentSlug = _context.Slugs
                                                .Where(s => s.ReferenceId == r.RelatedContent.Id && s.Type == SlugTypeEnum.Content)
                                                .Select(s => s.Title)
                                                .FirstOrDefault(),
                    })
                    .ToList(),
                Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Content)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                ReleaseDate = p.ReleaseDate,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            }).FirstOrDefault();

            if (item == null)
            {
                return new ResultDto<ResultContentItemDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Message = "اطلاعات یافت نشد",
                    Status = 404
                };
            }

            item.ReadTime = CalculateReadTime(item.Description, item.ShortDescription);
            item.ImageId = item.Image?.FirstOrDefault()?.Id;

            return new ResultDto<ResultContentItemDto>
            {
                Data = item,
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ResultContentItemDto> GetContentItemFrontEnd(long Id)
        {
            var item = _context.Contents
                .Where(p => p.Id == Id && p.IsActive && !p.IsDeleted && p.ReleaseDate <= DateTime.Now)
                .Select(p => new ResultContentItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsActive = p.IsActive,
                    Description = p.Description,
                    ShortDescription = p.ShortDescription,
                    MetaTitle = p.MetaTitle,
                    Image = _context.Images
                        .Where(i => i.ImageType == ImageTypeEnum.Content
                                 && i.ImageRole == ImageRoleEnum.Single
                                 && i.ReferenceId == p.Id)
                        .Select(i => new ResultImageDto
                        {
                            Id = i.Id,
                            Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Content/" + i.ImageUrl,
                        })
                        .ToList(),
                    ContentCategory = _context.ContentCategories
                        .IgnoreQueryFilters()
                        .Where(cc => cc.Id == p.ContentCategoryId)
                        .FirstOrDefault(),
                    ContentCategoryId = (long)p.ContentCategoryId,
                    MetaDescription = p.MetaDescription,
                    CreatedBy = p.CreatedBy == null ? null : p.CreatedBy.FirstName + " " + p.CreatedBy.LastName,
                    Relations = _context.ContentRelations
                        .Where(r => r.ContentId == p.Id)
                        .Select(r => new ContentRelationItemDto
                        {
                            Id = r.Id,
                            ContentId = r.ContentId,
                            RelatedContentId = r.RelatedContentId,
                            RelatedContentName = r.RelatedContent.Name,
                            RelatedContentSlug = _context.Slugs
                                                    .Where(s => s.ReferenceId == r.RelatedContent.Id && s.Type == SlugTypeEnum.Content)
                                                    .Select(s => s.Title)
                                                    .FirstOrDefault(),
                        })
                        .ToList(),
                    Slug = _context.Slugs
                                .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Content)
                                .Select(s => s.Title)
                                .FirstOrDefault(),
                    ReleaseDate = p.ReleaseDate,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                }).FirstOrDefault();

            if (item == null)
            {
                return new ResultDto<ResultContentItemDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Message = "اطلاعات یافت نشد",
                    Status = 404
                };
            }

            item.ReadTime = CalculateReadTime(item.Description, item.ShortDescription);
            item.ImageId = item.Image?.FirstOrDefault()?.Id;

            return new ResultDto<ResultContentItemDto>
            {
                Data = item,
                IsSuccess = true,
                Message = "اطلاعات ارسال شد",
                Status = 200
            };
        }

        public ResultDto<List<ResultContentListDto>> GetContentList(RequestListDto request)
        {
            try
            {
                var Contents = _context.Contents.AsQueryable();

                Contents = Contents.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    Contents = Contents.OrderByDescending(s => s.CreatedAt);
                }

                var rowsCount = 0;
                var contentList = Contents
                    .ToPaged(request.Page, request.PageSize, out rowsCount)
                    .Select(p => new ResultContentListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ShortDescription = p.ShortDescription,
                        Description = p.Description,
                        ContentCategoryId = (long)p.ContentCategoryId,
                        ContentCategory = _context.ContentCategories
                            .IgnoreQueryFilters()
                            .Where(cc => cc.Id == p.ContentCategoryId)
                            .Select(cc => cc.Name)
                            .FirstOrDefault(),
                        MetaTitle = p.MetaTitle,
                        MetaDescription = p.MetaDescription,
                        ReleaseDate = p.ReleaseDate,
                        IsActive = p.IsActive,
                        CreatedBy = p.CreatedBy == null ? null : p.CreatedBy.FirstName + " " + p.CreatedBy.LastName,
                        Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Content
                                     && i.ImageRole == ImageRoleEnum.Single
                                     && i.ReferenceId == p.Id)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Content/" + i.ImageUrl,
                            })
                            .ToList(),
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Content)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                foreach (var content in contentList)
                {
                    content.ReadTime = CalculateReadTime(content.Description, content.ShortDescription);
                    content.ImageId = content.Image?.FirstOrDefault()?.Id;
                }

                return new ResultDto<List<ResultContentListDto>>
                {
                    Data = contentList,
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ارسال گردید.",
                    Status = 200,
                    Result = new ResultListDto()
                    {
                        Current = request.Page,
                        Count = rowsCount
                    },
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<List<ResultContentListDto>>
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 201
                };
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<ResultContentListDto>> GetContentListFrontEnd(RequestListFrontendDto request, long categoryId)
        {
            try
            {
                var Contents = _context.Contents
                    .Where(p => p.IsActive && !p.IsDeleted && p.ReleaseDate <= DateTime.Now)
                    .AsQueryable();
                if(categoryId!=0)
                {
                    Contents = Contents.Where(p=>p.ContentCategoryId== categoryId);

                }
                Contents = Contents.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    Contents = Contents.OrderByDescending(p => p.ReleaseDate);
                }

                var rowsCount = 0;
                var contentList = Contents
                    .ToPaged(request.Page, request.PageSize, out rowsCount)
                   .Select(p => new ResultContentListDto
                   {
                       Id = p.Id,
                       Name = p.Name,
                       ShortDescription = p.ShortDescription,
                       Description = p.Description,
                       ContentCategoryId = (long)p.ContentCategoryId,
                       ContentCategory = _context.ContentCategories
                            .IgnoreQueryFilters()
                            .Where(cc => cc.Id == p.ContentCategoryId)
                            .Select(cc => cc.Name)
                            .FirstOrDefault(),
                       MetaTitle = p.MetaTitle,
                       MetaDescription = p.MetaDescription,
                       ReleaseDate = p.ReleaseDate,
                       IsActive = p.IsActive,
                       CreatedBy = p.CreatedBy == null ? null : p.CreatedBy.FirstName + " " + p.CreatedBy.LastName,
                       Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Content
                                     && i.ImageRole == ImageRoleEnum.Single
                                     && i.ReferenceId == p.Id)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Content/" + i.ImageUrl,
                            })
                            .ToList(),
                       Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Content)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                       CreatedAt = p.CreatedAt,
                       UpdatedAt = p.UpdatedAt,
                   })
                    .ToList();

                foreach (var content in contentList)
                {
                    content.ReadTime = CalculateReadTime(content.Description, content.ShortDescription);
                    content.ImageId = content.Image?.FirstOrDefault()?.Id;
                }

                return new ResultDto<List<ResultContentListDto>>
                {
                    Data = contentList,
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ارسال گردید.",
                    Status = 200,
                    Result = new ResultListDto()
                    {
                        Current = request.Page,
                        Count = rowsCount
                    },
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<List<ResultContentListDto>>
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 201
                };
            }
        }

        public ResultDto<List<ResultContentListDto>> Get4LastContent()
        {
            try
            {
                var contentList = _context.Contents
                   .Where(p => p.IsActive && !p.IsDeleted && p.ReleaseDate <= DateTime.Now)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new ResultContentListDto
                   {
                       Id = p.Id,
                       Name = p.Name,
                       ShortDescription = p.ShortDescription,
                       Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.Content
                                     && i.ImageRole == ImageRoleEnum.Single
                                     && i.ReferenceId == p.Id)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/Content/" + i.ImageUrl,
                            })
                            .ToList(),
                       MetaTitle = p.MetaTitle,
                       MetaDescription = p.MetaDescription,
                       ContentCategoryId = (long)p.ContentCategoryId,
                       ContentCategory = _context.ContentCategories
                            .IgnoreQueryFilters()
                            .Where(cc => cc.Id == p.ContentCategoryId)
                            .Select(cc => cc.Name)
                            .FirstOrDefault(),
                       IsActive = p.IsActive,
                       Slug = _context.Slugs
                            .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Content)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                   })
                    .Take(3)
                    .ToList();

                foreach (var content in contentList)
                {
                    content.ReadTime = CalculateReadTime(content.Description, content.ShortDescription);
                    content.ImageId = content.Image?.FirstOrDefault()?.Id;
                }

                return new ResultDto<List<ResultContentListDto>>
                {
                    Data = contentList,
                    IsSuccess = true,
                    Message = "اطلاعات با موفقیت ارسال گردید.",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                return new ResultDto<List<ResultContentListDto>>
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 201
                };
            }
        }

        // ==================== Helpers ====================

        /// <summary>
        /// Estimates reading time in whole minutes (minimum 1) from the given text,
        /// stripping basic HTML markup before counting words. Falls back to
        /// <paramref name="fallbackText"/> if <paramref name="primaryText"/> is empty.
        /// </summary>
        private static int CalculateReadTime(string? primaryText, string? fallbackText = null)
        {
            var text = !string.IsNullOrWhiteSpace(primaryText) ? primaryText : fallbackText;

            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var plainText = Regex.Replace(text, "<.*?>", " ");

            var wordCount = plainText
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Length;

            if (wordCount == 0)
                return 0;

            var minutes = (int)Math.Ceiling(wordCount / WordsPerMinute);
            return Math.Max(minutes, 1);
        }

        public class ResultContentItemDto
        {
            public string Name { get; set; }
            public long Id { get; set; }
            public string ShortDescription { get; set; }
            public string Description { get; set; }
            public List<ResultImageDto>? Image { get; set; }
            public long? ImageId { get; set; }
            public long ContentCategoryId { get; set; }
            public ContentCategory ContentCategory { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string Slug { get; set; }
            public string CreatedBy { get; set; }
            public DateTime ReleaseDate { get; set; }
            public bool IsActive { get; set; }
            public int ReadTime { get; set; }
            public List<ContentRelationItemDto> Relations { get; set; } = new();
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class ResultContentListDto
        {
            public string Name { get; set; }
            public long Id { get; set; }
            public string ShortDescription { get; set; }
            public string Description { get; set; }
            public List<ResultImageDto>? Image { get; set; }
            public long? ImageId { get; set; }
            public long ContentCategoryId { get; set; }
            public string ContentCategory { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string Slug { get; set; }
            public string CreatedBy { get; set; }
            public DateTime ReleaseDate { get; set; }
            public bool IsActive { get; set; }
            public bool IsDeleted { get; set; }
            public int ReadTime { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class ContentRelationItemDto
        {
            public long Id { get; set; }
            public long ContentId { get; set; }
            public long RelatedContentId { get; set; }
            public string RelatedContentName { get; set; }
            public string RelatedContentSlug { get; set; }
        }
    }
}