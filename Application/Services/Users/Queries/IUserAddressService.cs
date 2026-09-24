using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.UserAddresses.Queries
{
    // ==================== Interface ====================
    public interface IUserAddressService
    {
        ResultDto<List<UserAddressItemDto>> GetUserAddressList(RequestUserAddressDto request, long currentUserId);
        ResultDto<UserAddressItemDto> GetUserAddressItem(long id, long currentUserId);
        ResultDto<List<UserAddressItemDto>> GetUserAddressListFrontEnd(RequestUserAddressDto request, long currentUserId);
        ResultDto<UserAddressItemDto> GetUserAddressItemFrontEnd(long id, long currentUserId);

    }

    // ==================== Implementation ====================
    public class UserAddressService : IUserAddressService
    {
        private readonly IDataBaseContext _context;

        public UserAddressService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- GetList --------------------
        public ResultDto<List<UserAddressItemDto>> GetUserAddressList(RequestUserAddressDto request, long currentUserId)
        {
            try
            {
                var query =
                    from a in _context.UserAddresses.AsNoTracking()
                    join c in _context.Cities.AsNoTracking() on a.CityId equals c.Id
                    join s in _context.States.AsNoTracking() on c.StateId equals s.Id
                    where a.UserId == currentUserId
                    select new UserAddressItemDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        Address = a.Address,
                        Plaque = a.Plaque,
                        RecipientFullName = a.RecipientFullName,
                        CityId = a.CityId,
                        CityName = c.Name,
                        StateId = c.StateId,
                        StateName = s.Name,
                        Mobile = a.Mobile,
                        PostalCode = a.PostalCode,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt,
                    };

                int rowsCount = 0;
                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .OrderByDescending(x => x.Id)
                    .ToPaged(page, pageSize, out rowsCount)
                    .ToList();

                return new ResultDto<List<UserAddressItemDto>>
                {
                    Data = list,
                    Result = new ResultListDto { Current = page, Count = rowsCount },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست آدرس های کاربر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<UserAddressItemDto>>(ex);
            }
        }

        // -------------------- GetItem --------------------
        public ResultDto<UserAddressItemDto> GetUserAddressItem(long id, long currentUserId)
        {
            try
            {
                var address =
                    (from a in _context.UserAddresses.AsNoTracking()
                     join c in _context.Cities.AsNoTracking() on a.CityId equals c.Id
                     join s in _context.States.AsNoTracking() on c.StateId equals s.Id
                     where a.Id == id && a.UserId == currentUserId
                     select new UserAddressItemDto
                     {
                         Id = a.Id,
                         UserId = a.UserId,
                         Address = a.Address,
                         Plaque = a.Plaque,
                         RecipientFullName = a.RecipientFullName,
                         CityId = a.CityId,
                         CityName = c.Name,
                         StateId = c.StateId,
                         StateName = s.Name,
                         Mobile = a.Mobile,
                         PostalCode = a.PostalCode,
                         CreatedAt = a.CreatedAt,
                         UpdatedAt = a.UpdatedAt,
                     }).FirstOrDefault();

                if (address == null)
                {
                    return new ResultDto<UserAddressItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "آدرس یافت نشد"
                    };
                }

                return new ResultDto<UserAddressItemDto>
                {
                    Data = address,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات آدرس ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<UserAddressItemDto>(ex);
            }
        }

        // -------------------- GetListFrontEnd --------------------
        public ResultDto<List<UserAddressItemDto>> GetUserAddressListFrontEnd(RequestUserAddressDto request, long currentUserId)
        {
            try
            {
                var query =
                    from a in _context.UserAddresses.AsNoTracking()
                    join c in _context.Cities.AsNoTracking() on a.CityId equals c.Id
                    join s in _context.States.AsNoTracking() on c.StateId equals s.Id
                    where a.UserId == currentUserId && a.IsActive
                    select new UserAddressItemDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        Address = a.Address,
                        Plaque = a.Plaque,
                        RecipientFullName = a.RecipientFullName,
                        CityId = a.CityId,
                        CityName = c.Name,
                        StateId = c.StateId,
                        StateName = s.Name,
                        Mobile = a.Mobile,
                        PostalCode = a.PostalCode
                    };

                int rowsCount = 0;
                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

                var list = query
                    .OrderByDescending(x => x.Id)
                    .ToPaged(page, pageSize, out rowsCount)
                    .ToList();

                return new ResultDto<List<UserAddressItemDto>>
                {
                    Data = list,
                    Result = new ResultListDto { Current = page, Count = rowsCount },
                    IsSuccess = true,
                    Status = 200,
                    Message = "لیست آدرس های کاربر ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<List<UserAddressItemDto>>(ex);
            }
        }

        // -------------------- GetItemFrontEnd --------------------
        public ResultDto<UserAddressItemDto> GetUserAddressItemFrontEnd(long id, long currentUserId)
        {
            try
            {
                var address =
                    (from a in _context.UserAddresses.AsNoTracking()
                     join c in _context.Cities.AsNoTracking() on a.CityId equals c.Id
                     join s in _context.States.AsNoTracking() on c.StateId equals s.Id
                     where a.Id == id && a.UserId == currentUserId && a.IsActive
                     select new UserAddressItemDto
                     {
                         Id = a.Id,
                         UserId = a.UserId,
                         Address = a.Address,
                         Plaque = a.Plaque,
                         RecipientFullName = a.RecipientFullName,
                         CityId = a.CityId,
                         CityName = c.Name,
                         StateId = c.StateId,
                         StateName = s.Name,
                         Mobile = a.Mobile,
                         PostalCode = a.PostalCode
                     }).FirstOrDefault();

                if (address == null)
                {
                    return new ResultDto<UserAddressItemDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "آدرس یافت نشد"
                    };
                }

                return new ResultDto<UserAddressItemDto>
                {
                    Data = address,
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات آدرس ارسال گردید"
                };
            }
            catch (Exception ex)
            {
                return HandleException<UserAddressItemDto>(ex);
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
                // silent fail safe
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
    public class UserAddressItemDto
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Address { get; set; }
        public string Plaque { get; set; }
        public string RecipientFullName { get; set; }
        public long CityId { get; set; }
        public string? CityName { get; set; }
        public long StateId { get; set; }
        public string? StateName { get; set; }
        public string Mobile { get; set; }
        public string PostalCode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RequestUserAddressDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}