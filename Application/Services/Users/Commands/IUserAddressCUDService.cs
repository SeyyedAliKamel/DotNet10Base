using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;

namespace Application.Services.UserAddresses.Commands
{
    // ==================== Interface ====================
    public interface IUserAddressCUDService
    {
        ResultDto<UserAddressResultDto> AddUserAddress(UserAddressRequestDto request, long currentUserId);
        ResultDto<UserAddressResultDto> EditUserAddress(long id, UserAddressRequestDto request, long currentUserId);
        ResultDto RemoveUserAddress(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class UserAddressCUDService : IUserAddressCUDService
    {
        private readonly IDataBaseContext _context;

        public UserAddressCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto<UserAddressResultDto> AddUserAddress(UserAddressRequestDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var userExists = _context.Users.Any(u => u.Id == currentUserId);
                if (!userExists)
                    return FailData<UserAddressResultDto>("کاربر یافت نشد");

                var city = _context.Cities.FirstOrDefault(c => c.Id == request.CityId);
                if (city is null)
                    return FailData<UserAddressResultDto>("شهر یافت نشد");

                var address = new UserAddress
                {
                    UserId = (int)currentUserId,
                    Address = request.Address,
                    Plaque = request.Plaque,
                    RecipientFullName = request.RecipientFullName,
                    CityId = request.CityId,
                    StateId = city.StateId,
                    Mobile = request.Mobile,
                    PostalCode = request.PostalCode
                };

                _context.SetUserId(currentUserId);
                _context.UserAddresses.Add(address);
                _context.SaveChanges();
                transaction.Commit();

                var stateName = _context.States.Where(s => s.Id == city.StateId).Select(s => s.Name).FirstOrDefault();
                return OkData(MapToResult(address, city.Name, stateName), "آدرس با موفقیت ایجاد گردید");
            }
            catch (Exception ex)
            {
                return HandleException<UserAddressResultDto>(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Update --------------------
        public ResultDto<UserAddressResultDto> EditUserAddress(long id, UserAddressRequestDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var existing = _context.UserAddresses.FirstOrDefault(a => a.Id == id);
                if (existing is null)
                    return FailData<UserAddressResultDto>("آدرس یافت نشد");

                if (existing.UserId != currentUserId)
                    return FailData<UserAddressResultDto>("دسترسی غیرمجاز", 403);

                var city = _context.Cities.FirstOrDefault(c => c.Id == request.CityId);
                if (city is null)
                    return FailData<UserAddressResultDto>("شهر یافت نشد");

                var isLocked = IsAddressLockedByBasket(existing.Id);
                UserAddress target;

                if (isLocked)
                {
                    existing.IsActive = false;
                    _context.UserAddresses.Update(existing);

                    target = new UserAddress
                    {
                        UserId = (int)currentUserId,
                        Address = request.Address,
                        Plaque = request.Plaque,
                        RecipientFullName = request.RecipientFullName,
                        CityId = request.CityId,
                        StateId = city.StateId,
                        Mobile = request.Mobile,
                        PostalCode = request.PostalCode,
                        IsActive = true
                    };
                    _context.UserAddresses.Add(target);
                }
                else
                {
                    existing.Address = request.Address;
                    existing.Plaque = request.Plaque;
                    existing.RecipientFullName = request.RecipientFullName;
                    existing.CityId = request.CityId;
                    existing.StateId = city.StateId;
                    existing.Mobile = request.Mobile;
                    existing.PostalCode = request.PostalCode;

                    _context.UserAddresses.Update(existing);
                    target = existing;
                }

                _context.SetUserId(currentUserId);
                _context.SaveChanges();
                transaction.Commit();

                var stateName = _context.States.Where(s => s.Id == city.StateId).Select(s => s.Name).FirstOrDefault();
                return OkData(MapToResult(target, city.Name, stateName), "آدرس با موفقیت به روز گردید");
            }
            catch (Exception ex)
            {
                return HandleException<UserAddressResultDto>(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Delete --------------------
        public ResultDto RemoveUserAddress(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var existing = _context.UserAddresses.FirstOrDefault(a => a.Id == id);
                if (existing is null)
                    return Fail("آدرس یافت نشد");

                if (existing.UserId != currentUserId)
                    return Fail("دسترسی غیرمجاز", 403);

                if (IsAddressLockedByBasket(existing.Id))
                {
                    existing.IsActive = false;
                    _context.UserAddresses.Update(existing);
                }
                else
                {
                    _context.UserAddresses.Remove(existing);
                }

                _context.SaveChanges();
                transaction.Commit();

                return Ok("آدرس با موفقیت حذف گردید");
            }
            catch (Exception ex)
            {
                return HandleException(transaction, ex, id.ToJson(), currentUserId);
            }
        }

        // ==================== Private Helpers ====================
        private static UserAddressResultDto MapToResult(UserAddress address, string cityName = null, string stateName = null) => new()
        {
            Id = address.Id,
            UserId = address.UserId,
            Address = address.Address,
            Plaque = address.Plaque,
            RecipientFullName = address.RecipientFullName,
            CityId = address.CityId,
            CityName = cityName,
            StateId = address.StateId,
            StateName = stateName,
            Mobile = address.Mobile,
            PostalCode = address.PostalCode
        };

        private ResultDto Ok(string message) =>
            new ResultDto { IsSuccess = true, Message = message, Status = 200 };

        private ResultDto Fail(string message, int status = 400) =>
            new ResultDto { IsSuccess = false, Message = message, Status = status };

        private ResultDto<T> OkData<T>(T data, string message) =>
            new ResultDto<T> { Data = data, IsSuccess = true, Message = message, Status = 200 };

        private ResultDto<T> FailData<T>(string message, int status = 400) =>
            new ResultDto<T> { Data = default, IsSuccess = false, Message = message, Status = status };

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
                entry.State = EntityState.Detached;
        }

        private ResultDto HandleException(IDbContextTransaction transaction, Exception ex, string requestParam, long? userId)
        {
            transaction.Rollback();
            DetachAll();
            _context.HandleExceptions.Add(new HandleException
            {
                Message = ex.InnerException?.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = userId,
            });
            _context.SaveChanges();
            return new ResultDto { IsSuccess = false, Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد", Status = 500 };
        }

        private ResultDto<T> HandleException<T>(IDbContextTransaction transaction, Exception ex, string requestParam, long? userId)
        {
            transaction.Rollback();
            DetachAll();
            _context.HandleExceptions.Add(new HandleException
            {
                Message = ex.InnerException?.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = userId,
            });
            _context.SaveChanges();
            return new ResultDto<T> { IsSuccess = false, Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد", Status = 500 };
        }

        private bool IsAddressLockedByBasket(long addressId)
        {
            return _context.Baskets.Any(b =>
                b.UserAddressId == addressId &&
                (b.BasketStatus == BasketStatus.Finalized || b.BasketStatus == BasketStatus.Sent));
        }
    }

    // ==================== DTOs ====================
    public class UserAddressRequestDto
    {
        public string Address { get; set; }
        public string Plaque { get; set; }
        public string RecipientFullName { get; set; }
        public long CityId { get; set; }
        public string Mobile { get; set; }
        public string PostalCode { get; set; }
    }

    public class UserAddressResultDto
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
    }
}