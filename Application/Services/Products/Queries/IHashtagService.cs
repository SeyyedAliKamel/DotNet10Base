using Application.Interfaces.Contexts;
using Application.Services.Products.Commands;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Products.Queries
{
    // ==================== Interface ====================
    public interface IHashtagService
    {
        ResultDto<List<HashtagDto>> GetHashtagList(HashtagListRequestDto request);
        ResultDto<HashtagItemDto> GetHashtagItem(long id);

        // -------- Frontend-facing (active only) --------
        ResultDto<List<HashtagDto>> GetHashtagListFrontEnd(HashtagListRequestDto request);
        ResultDto<HashtagItemDto> GetHashtagItemFrontEnd(long id);
    }

    // ==================== Implementation ====================
    public class HashtagService : IHashtagService
    {
        private readonly IDataBaseContext _context;

        public HashtagService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<HashtagDto>> GetHashtagList(HashtagListRequestDto request)
        {
            try
            {
                var query = _context.Hashtags
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
                    .Select(x => new HashtagDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        ShowMenu = x.ShowMenu,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<HashtagDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست هشتگ ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<HashtagDto>>(ex);
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<HashtagDto>> GetHashtagListFrontEnd(HashtagListRequestDto request)
        {
            try
            {
                var query = _context.Hashtags
                    .AsNoTracking()
                    .Where(x => x.IsActive)
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
                    .Select(x => new HashtagDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                    })
                    .ToList();

                return new ResultDto<List<HashtagDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست هشتگ ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<HashtagDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<HashtagItemDto> GetHashtagItem(long id)
        {
            try
            {
                var hashtag = _context.Hashtags
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new HashtagItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        ShowMenu = x.ShowMenu,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (hashtag == null)
                {
                    return new ResultDto<HashtagItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "هشتگ یافت نشد"
                    };
                }

                return new ResultDto<HashtagItemDto>
                {
                    Data = hashtag,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات هشتگ ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<HashtagItemDto>(ex);
            }
        }

        // -------------------- GetItemFrontend --------------------
        public ResultDto<HashtagItemDto> GetHashtagItemFrontEnd(long id)
        {
            try
            {
                var hashtag = _context.Hashtags
                    .AsNoTracking()
                    .Where(x => x.Id == id && x.IsActive)
                    .Select(x => new HashtagItemDto
                    {
                        Id = x.Id,
                        Title = x.Title
                    })
                    .FirstOrDefault();

                if (hashtag == null)
                {
                    return new ResultDto<HashtagItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "هشتگ یافت نشد"
                    };
                }

                return new ResultDto<HashtagItemDto>
                {
                    Data = hashtag,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات هشتگ ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<HashtagItemDto>(ex);
            }
        }

        // ==================== Exception Handler ====================
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
                // avoid throwing from logger
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
    public class HashtagDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public bool ShowMenu { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class HashtagListRequestDto
    {
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}