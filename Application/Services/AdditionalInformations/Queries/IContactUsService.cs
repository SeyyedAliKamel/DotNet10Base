using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdditionalInformations.Queries
{
    // ==================== Interface ====================
    public interface IContactUsService
    {
        ResultDto<List<ResultContactUsListDto>> GetContactUsList(RequestListDto request);
        ResultDto<ResultContactUsItemDto> GetContactUsItem(long id);
    }

    // ==================== Implementation ====================
    public class ContactUsService : IContactUsService
    {
        private readonly IDataBaseContext _context;

        public ContactUsService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<ResultContactUsListDto>> GetContactUsList(RequestListDto request)
        {
            try
            {
                var query = _context.ContactUses
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
                    .Select(x => new ResultContactUsListDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Phone = x.Phone,
                        Email = x.Email,
                        Description = x.Description,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToList();

                return new ResultDto<List<ResultContactUsListDto>>
                {
                    Data = list,
                    Result = new ResultListDto
                    {
                        Current = page,
                        Count = rowsCount
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست تماس‌ها"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<ResultContactUsListDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<ResultContactUsItemDto> GetContactUsItem(long id)
        {
            try
            {
                var contactUs = _context.ContactUses
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ResultContactUsItemDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Phone = x.Phone,
                        Email = x.Email,
                        Description = x.Description,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .FirstOrDefault();

                if (contactUs == null)
                {
                    return new ResultDto<ResultContactUsItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "اطلاعات تماس یافت نشد"
                    };
                }

                return new ResultDto<ResultContactUsItemDto>
                {
                    Data = contactUs,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات تماس ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ResultContactUsItemDto>(ex);
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
    public class ResultContactUsListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ResultContactUsItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}