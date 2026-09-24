using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Search.Queries
{
    public interface ISearchService
    {
        ResultDto<List<SearchResultDto>> Search(SearchRequestDto request);
    }

    public class SearchService : ISearchService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public SearchService(IConfiguration configuration, IDataBaseContext context)
        {
            _context = context;
            _configuration = configuration;
        }

        public ResultDto<List<SearchResultDto>> Search(SearchRequestDto request)
        {
            try
            {
                string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
                var key = request.Key?.Trim();

                // -------------------- Base queries --------------------
                var productQuery = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .AsQueryable();

                var contentQuery = _context.Contents
                    .AsNoTracking()
                    .Where(c => c.IsActive && !c.IsDeleted && c.ReleaseDate <= DateTime.Now)
                    .AsQueryable();

                // -------------------- Manual key search --------------------
                if (!string.IsNullOrWhiteSpace(key))
                {
                    productQuery = productQuery.Where(p => p.Name.Contains(key));
                    contentQuery = contentQuery.Where(c => c.Name.Contains(key));
                }

                // -------------------- Sorting --------------------
                if (!string.IsNullOrWhiteSpace(request.Sort))
                {
                    productQuery = Filter.ApplySorting(productQuery, request.Sort);
                    contentQuery = Filter.ApplySorting(contentQuery, request.Sort);
                }
                else
                {
                    productQuery = productQuery.OrderByDescending(p => p.CreatedAt);
                    contentQuery = contentQuery.OrderByDescending(c => c.CreatedAt);
                }

                var productTotal = productQuery.Count();
                var contentTotal = contentQuery.Count();
                var rowsCount = productTotal + contentTotal;

                var offset = (page - 1) * pageSize;
                var results = new List<SearchResultDto>();

                if (offset < productTotal)
                {
                    var take = Math.Min(pageSize, productTotal - offset);

                    var products = productQuery
                        .Skip(offset)
                        .Take(take)
                        .Select(p => new SearchResultDto
                        {
                            Id = p.Id,
                            Type = SearchResultType.Product,
                            Name = p.Name,
                            Description = p.Description,
                            Price = p.Price,
                            ImageUrl = _context.Images
                                .Where(i => i.ImageType == ImageTypeEnum.Product
                                         && i.ImageRole == ImageRoleEnum.Primary
                                         && i.ReferenceId == p.Id)
                                .Select(i => i.ImageUrl)
                                .FirstOrDefault(),
                            Slug = _context.Slugs
                                .Where(s => s.ReferenceId == p.Id && s.Type == SlugTypeEnum.Product)
                                .Select(s => s.Title)
                                .FirstOrDefault(),
                        })
                        .ToList();

                    foreach (var p in products)
                    {
                        if (p.ImageUrl != null)
                            p.ImageUrl = staticPath + "/Resources/Product/" + p.ImageUrl;
                    }

                    results.AddRange(products);

                    var remaining = pageSize - results.Count;
                    if (remaining > 0)
                    {
                        var content = contentQuery
                            .Take(remaining)
                            .Select(c => new SearchResultDto
                            {
                                Id = c.Id,
                                Type = SearchResultType.Content,
                                Name = c.Name,
                                Description = c.Description,
                                ShortDescription = c.ShortDescription,
                                ImageUrl = _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.Content
                                             && i.ImageRole == ImageRoleEnum.Single
                                             && i.ReferenceId == c.Id)
                                    .Select(i => i.ImageUrl)
                                    .FirstOrDefault(),
                                Slug = _context.Slugs
                                    .Where(s => s.ReferenceId == c.Id && s.Type == SlugTypeEnum.Content)
                                    .Select(s => s.Title)
                                    .FirstOrDefault(),
                            })
                            .ToList();

                        foreach (var c in content)
                        {
                            if (c.ImageUrl != null)
                                c.ImageUrl = staticPath + "/Resources/Content/" + c.ImageUrl;
                        }

                        results.AddRange(content);
                    }
                }
                else
                {
                    var contentOffset = offset - productTotal;

                    var content = contentQuery
                        .Skip(contentOffset)
                        .Take(pageSize)
                        .Select(c => new SearchResultDto
                        {
                            Id = c.Id,
                            Type = SearchResultType.Content,
                            Name = c.Name,
                            Description = c.Description,
                            ShortDescription = c.ShortDescription,
                            ImageUrl = _context.Images
                                .Where(i => i.ImageType == ImageTypeEnum.Content
                                         && i.ImageRole == ImageRoleEnum.Single
                                         && i.ReferenceId == c.Id)
                                .Select(i => i.ImageUrl)
                                .FirstOrDefault(),
                            Slug = _context.Slugs
                                .Where(s => s.ReferenceId == c.Id && s.Type == SlugTypeEnum.Content)
                                .Select(s => s.Title)
                                .FirstOrDefault(),
                        })
                        .ToList();

                    foreach (var c in content)
                    {
                        if (c.ImageUrl != null)
                            c.ImageUrl = staticPath + "/Resources/Content/" + c.ImageUrl;
                    }

                    results.AddRange(content);
                }

                return new ResultDto<List<SearchResultDto>>
                {
                    Data = results,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "نتایج جستجو"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<SearchResultDto>>(ex);
            }
        }

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }

        private ResultDto<T> HandleException<T>(Exception ex)
        {
            try
            {
                DetachAll();

                _context.HandleExceptions.Add(new HandleException
                {
                    Message = ex.InnerException?.Message ?? ex.Message,
                    StackTrace = ex.StackTrace
                });

                _context.SaveChanges();
            }
            catch
            {
                // avoid secondary crash during logging
            }

            return new ResultDto<T>
            {
                IsSuccess = false,
                Status = 500,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد"
            };
        }
    }

    public enum SearchResultType
    {
        Product,
        Content
    }

    public class SearchResultDto
    {
        public long Id { get; set; }
        public SearchResultType Type { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public int? Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Slug { get; set; }
    }

    public class SearchRequestDto
    {
        public string? Key { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? Sort { get; set; }
    }
}