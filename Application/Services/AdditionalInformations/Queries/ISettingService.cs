using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdditionalInformations.Queries
{
    // ==================== Interface ====================
    public interface ISettingService
    {
        ResultDto<List<SettingDto>> GetSettingList(RequestSettingListDto request);
        ResultDto<SettingItemDto> GetSettingItem(long id);
    }

    // ==================== Implementation ====================
    public class SettingService : ISettingService
    {
        private readonly IDataBaseContext _context;

        public SettingService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetListFiltered --------------------
        public ResultDto<List<SettingDto>> GetSettingList(RequestSettingListDto request)
        {
            try
            {
                var query = _context.Settings.AsNoTracking().AsQueryable();

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
                    .Select(p => new SettingDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Key = p.Key,
                        Value = p.Value,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<SettingDto>>
                {
                    Data = list,
                    Result = new ResultListDto { Current = page, Count = rowsCount },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست تنظیمات"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<SettingDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<SettingItemDto> GetSettingItem(long id)
        {
            try
            {
                var setting = _context.Settings
                    .AsNoTracking()
                    .Where(s => s.Id == id)
                    .Select(p => new SettingItemDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Key = p.Key,
                        Value = p.Value,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (setting == null)
                {
                    return new ResultDto<SettingItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "تنظیمات یافت نشد"
                    };
                }

                return new ResultDto<SettingItemDto>
                {
                    Data = setting,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات تنظیمات ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<SettingItemDto>(ex);
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
    public class SettingDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SettingItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RequestSettingListDto
    {
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}