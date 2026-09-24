using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Products.Queries
{
    // ==================== Interface ====================
    public interface ISlugService
    {
        ResultDto<List<ResultSlugListDto>> GetSlugList(RequestListDto request);
        ResultDto<ResultSlugItemDto> GetSlugItem(long id);
        ResultDto<ResultSlugItemDto> GetSlug(string title);
    }

    // ==================== Implementation ====================
    public class SlugService : ISlugService
    {
        private readonly IDataBaseContext _context;

        public SlugService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<ResultSlugListDto>> GetSlugList(RequestListDto request)
        {
            try
            {
                var query = _context.Slugs
                    .AsNoTracking()
                    .AsQueryable();

                int rowsCount = 0;

                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToPaged(page, pageSize, out rowsCount)
                    .Select(x => new ResultSlugListDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Type = x.Type,
                        ReferenceId = x.ReferenceId,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ResultSlugListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست اسلاگ ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultSlugListDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ResultSlugItemDto> GetSlugItem(long id)
        {
            try
            {
                var slug = _context.Slugs
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ResultSlugItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Type = x.Type,
                        ReferenceId = x.ReferenceId,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (slug == null)
                {
                    return new ResultDto<ResultSlugItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "اسلاگ یافت نشد"
                    };
                }

                if (!ReferenceExists(slug.Type, slug.ReferenceId))
                {
                    return new ResultDto<ResultSlugItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "موردی که اسلاگ به آن اشاره دارد یافت نشد"
                    };
                }

                return new ResultDto<ResultSlugItemDto>
                {
                    Data = slug,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات اسلاگ ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultSlugItemDto>(ex);
            }
        }

        // -------------------- GetSlugByTitle --------------------
        public ResultDto<ResultSlugItemDto> GetSlug(string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return new ResultDto<ResultSlugItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 400,
                        Message = "عنوان اسلاگ نمی‌تواند خالی باشد"
                    };
                }

                var slug = _context.Slugs
                    .AsNoTracking()
                    .Where(x => x.Title == title)
                    .Select(x => new ResultSlugItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Type = x.Type,
                        ReferenceId = x.ReferenceId
                    })
                    .FirstOrDefault();

                if (slug == null)
                {
                    return new ResultDto<ResultSlugItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "اسلاگ یافت نشد"
                    };
                }

                if (!ReferenceExists(slug.Type, slug.ReferenceId))
                {
                    return new ResultDto<ResultSlugItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "موردی که اسلاگ به آن اشاره دارد یافت نشد"
                    };
                }

                return new ResultDto<ResultSlugItemDto>
                {
                    Data = slug,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات اسلاگ ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultSlugItemDto>(ex);
            }
        }

        // ==================== Helpers ====================
        private bool ReferenceExists(SlugTypeEnum type, long referenceId)
        {
            return type switch
            {
                SlugTypeEnum.Product => _context.Products.Any(x => x.Id == referenceId),
                SlugTypeEnum.Content => _context.Contents.Any(x => x.Id == referenceId),
                SlugTypeEnum.ProductCategory => _context.ProductCategories.Any(x => x.Id == referenceId),
                SlugTypeEnum.ContentCategory => _context.ContentCategories.Any(x => x.Id == referenceId),
                _ => false
            };
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

    // ==================== DTOs ====================
    public class ResultSlugListDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public SlugTypeEnum Type { get; set; }
        public long ReferenceId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultSlugItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public SlugTypeEnum Type { get; set; }
        public long ReferenceId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}