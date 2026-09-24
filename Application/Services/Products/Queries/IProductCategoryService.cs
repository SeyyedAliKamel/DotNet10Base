using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services.Products.Queries
{
    // ==================== Interface ====================
    public interface IProductCategoryService
    {
        ResultDto<List<ProductCategoryDto>> GetProductCategoryList(ProductCategoryListRequestDto request);
        ResultDto<ProductCategoryItemDto> GetProductCategoryItem(long id);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<ProductCategoryDto>> GetProductCategoryListFrontEnd(ProductCategoryListRequestDto request);
        ResultDto<ProductCategoryItemDto> GetProductCategoryItemFrontEnd(long id);
    }

    // ==================== Implementation ====================
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;

        public ProductCategoryService(IDataBaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<ProductCategoryDto>> GetProductCategoryList(ProductCategoryListRequestDto request)
        {
            try
            {
                var query = _context.ProductCategories
                    .AsNoTracking()
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderByDescending(s => s.CreatedAt);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(c => new ProductCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        MetaTitle = c.MetaTitle,
                        MetaDescription = c.MetaDescription,
                        Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.ProductCategory
                                     && i.ImageRole == ImageRoleEnum.Single
                                     && i.ReferenceId == c.Id)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ProductCategory/" + i.ImageUrl,
                            })
                            .ToList(),
                        IsActive = c.IsActive,
                        ParentId = c.ParentId,
                        ParentName = c.Parent != null ? c.Parent.Name : null,
                        ChildrenCount = c.Children != null ? c.Children.Count : 0,
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == c.Id && s.Type == SlugTypeEnum.ProductCategory)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                    })
                    .ToList();

                foreach (var category in list)
                {
                    category.ImageId = category.Image?.FirstOrDefault()?.Id;
                }

                return new ResultDto<List<ProductCategoryDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست دسته بندی محصولات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductCategoryDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<ProductCategoryDto>> GetProductCategoryListFrontEnd(ProductCategoryListRequestDto request)
        {
            try
            {
                var query = _context.ProductCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .AsQueryable();

                query = query.ApplySortingAndFiltering(request.Sort, request.Filter);

                if (string.IsNullOrWhiteSpace(request.Sort))
                {
                    query = query.OrderBy(s => s.Order);
                }

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(c => new ProductCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        MetaTitle = c.MetaTitle,
                        MetaDescription = c.MetaDescription,
                        Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.ProductCategory
                                     && i.ImageRole == ImageRoleEnum.Single
                                     && i.ReferenceId == c.Id)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ProductCategory/" + i.ImageUrl,
                            })
                            .ToList(),
                        IsActive = c.IsActive,
                        ParentId = c.ParentId,
                        ParentName = c.Parent != null ? c.Parent.Name : null,
                        ChildrenCount = c.Children != null ? c.Children.Count(ch => ch.IsActive) : 0,
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == c.Id && s.Type == SlugTypeEnum.ProductCategory)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                    })
                    .ToList();

                foreach (var category in list)
                {
                    category.ImageId = category.Image?.FirstOrDefault()?.Id;
                }

                return new ResultDto<List<ProductCategoryDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست دسته بندی محصولات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ProductCategoryDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ProductCategoryItemDto> GetProductCategoryItem(long id)
        {
            try
            {
                var category = _context.ProductCategories
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new ProductCategoryItemDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        MetaTitle = c.MetaTitle,
                        MetaDescription = c.MetaDescription,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.ProductCategory
                                     && i.ImageRole == ImageRoleEnum.Single
                                     && i.ReferenceId == c.Id)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ProductCategory/" + i.ImageUrl,
                            })
                            .ToList(),
                        IsActive = c.IsActive,
                        ParentId = c.ParentId,
                        ParentName = c.Parent != null ? c.Parent.Name : null,
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == c.Id && s.Type == SlugTypeEnum.ProductCategory)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        Children = c.Children != null
                            ? c.Children.Select(ch => new ProductCategoryDto
                            {
                                Id = ch.Id,
                                Name = ch.Name,
                                Image = _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.ProductCategory
                                             && i.ImageRole == ImageRoleEnum.Single
                                             && i.ReferenceId == ch.Id)
                                    .Select(i => new ResultImageDto
                                    {
                                        Id = i.Id,
                                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ProductCategory/" + i.ImageUrl,
                                    })
                                    .ToList(),
                                IsActive = ch.IsActive,
                                ParentId = ch.ParentId,
                                ParentName = c.Name,
                                ChildrenCount = ch.Children != null ? ch.Children.Count : 0,
                                Slug = _context.Slugs
                                    .Where(s => s.ReferenceId == ch.Id && s.Type == SlugTypeEnum.ProductCategory)
                                    .Select(s => s.Title)
                                    .FirstOrDefault(),
                                CreatedAt = c.CreatedAt,
                                UpdatedAt = c.UpdatedAt,
                            }).ToList()
                            : new List<ProductCategoryDto>()
                    })
                    .FirstOrDefault();

                if (category == null)
                {
                    return new ResultDto<ProductCategoryItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "دسته بندی یافت نشد"
                    };
                }

                category.ImageId = category.Image?.FirstOrDefault()?.Id;
                foreach (var child in category.Children)
                {
                    child.ImageId = child.Image?.FirstOrDefault()?.Id;
                }

                return new ResultDto<ProductCategoryItemDto>
                {
                    Data = category,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات دسته بندی ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ProductCategoryItemDto>(ex);
            }
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<ProductCategoryItemDto> GetProductCategoryItemFrontEnd(long id)
        {
            try
            {
                var category = _context.ProductCategories
                    .AsNoTracking()
                    .Where(c => c.Id == id && c.IsActive)
                    .Select(c => new ProductCategoryItemDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        MetaTitle = c.MetaTitle,
                        MetaDescription = c.MetaDescription,
                        Image = _context.Images
                            .Where(i => i.ImageType == ImageTypeEnum.ProductCategory
                                     && i.ImageRole == ImageRoleEnum.Single
                                     && i.ReferenceId == c.Id)
                            .Select(i => new ResultImageDto
                            {
                                Id = i.Id,
                                Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ProductCategory/" + i.ImageUrl,
                            })
                            .ToList(),
                        IsActive = c.IsActive,
                        ParentId = c.ParentId,
                        ParentName = c.Parent != null ? c.Parent.Name : null,
                        Slug = _context.Slugs
                            .Where(s => s.ReferenceId == c.Id && s.Type == SlugTypeEnum.ProductCategory)
                            .Select(s => s.Title)
                            .FirstOrDefault(),
                        Children = c.Children != null
                            ? c.Children.Where(ch => ch.IsActive).Select(ch => new ProductCategoryDto
                            {
                                Id = ch.Id,
                                Name = ch.Name,
                                Image = _context.Images
                                    .Where(i => i.ImageType == ImageTypeEnum.ProductCategory
                                             && i.ImageRole == ImageRoleEnum.Single
                                             && i.ReferenceId == ch.Id)
                                    .Select(i => new ResultImageDto
                                    {
                                        Id = i.Id,
                                        Preview = _configuration.GetSection("AppSettings")["StaticPath"] + "/Resources/ProductCategory/" + i.ImageUrl,
                                    })
                                    .ToList(),
                                IsActive = ch.IsActive,
                                ParentId = ch.ParentId,
                                ParentName = c.Name,
                                ChildrenCount = ch.Children != null ? ch.Children.Count(g => g.IsActive) : 0,
                                Slug = _context.Slugs
                                    .Where(s => s.ReferenceId == ch.Id && s.Type == SlugTypeEnum.ProductCategory)
                                    .Select(s => s.Title)
                                    .FirstOrDefault(),
                            }).ToList()
                            : new List<ProductCategoryDto>()
                    })
                    .FirstOrDefault();

                if (category == null)
                {
                    return new ResultDto<ProductCategoryItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "دسته بندی یافت نشد"
                    };
                }

                category.ImageId = category.Image?.FirstOrDefault()?.Id;
                foreach (var child in category.Children)
                {
                    child.ImageId = child.Image?.FirstOrDefault()?.Id;
                }

                return new ResultDto<ProductCategoryItemDto>
                {
                    Data = category,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات دسته بندی ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ProductCategoryItemDto>(ex);
            }
        }

        // ==================== Helpers ====================
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

    // ==================== DTOs ====================
    public class ProductCategoryDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public List<ResultImageDto>? Image { get; set; }
        public long? ImageId { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
        public long? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int ChildrenCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductCategoryItemDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public List<ResultImageDto>? Image { get; set; }
        public long? ImageId { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
        public long? ParentId { get; set; }
        public string? ParentName { get; set; }
        public List<ProductCategoryDto> Children { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductCategoryListRequestDto
    {
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}