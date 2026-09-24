using Application.Interfaces.Contexts;
using Application.Services.Shippings.Queries;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Shippings;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using static Application.Services.Shippings.Queries.ShippingService;

namespace Application.Services.Shippings.Commands
{
    // ==================== Interface ====================
    public interface IShippingCUDService
    {
        ResultDto AddShipping(RequestAddShippingDto request, long currentUserId);
        ResultDto EditShipping(RequestEditShippingDto request, long currentUserId);
        ResultDto RemoveShipping(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ShippingCUDService : IShippingCUDService
    {
        private readonly IDataBaseContext _context;

        public ShippingCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto AddShipping(RequestAddShippingDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var name = request.Name?.Trim();

                if (request.MinPrice is < 0)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "حداقل قیمت نمی‌تواند منفی باشد"
                    };

                if (!ShippingCityIds.Validate(request.CityIds, out var cityError))
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = cityError
                    };

                if (!ShippingConditions.Validate(request.Conditions, out var conditionsError))
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = conditionsError
                    };

                var duplicate = _context.Shippings.Any(x => x.Name == name);

                if (duplicate)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "این نام روش ارسال قبلاً ثبت شده است"
                    };

                var shipping = new Shipping
                {
                    Name = name!,
                    Description = request.Description,
                    CityIds = ShippingCityIds.Serialize(request.CityIds),
                    MinPrice = request.MinPrice,
                    Conditions = ShippingConditions.Serialize(request.Conditions),
                };

                _context.SetUserId(currentUserId);
                _context.Shippings.Add(shipping);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "روش ارسال با موفقیت ثبت شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = request.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);

                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
        }

        // -------------------- Update --------------------
        public ResultDto EditShipping(RequestEditShippingDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.Shippings.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "اطلاعات اشتباه است"
                    };

                var name = request.Name?.Trim();

                if (request.MinPrice is < 0)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "حداقل قیمت نمی‌تواند منفی باشد"
                    };

                if (!ShippingCityIds.Validate(request.CityIds, out var cityError))
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = cityError
                    };

                if (!ShippingConditions.Validate(request.Conditions, out var conditionsError))
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = conditionsError
                    };

                var duplicate = _context.Shippings.Any(x => x.Id != request.Id && x.Name == name);

                if (duplicate)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "این نام روش ارسال قبلاً ثبت شده است"
                    };

                data.Name = name!;
                data.Description = request.Description;
                data.CityIds = ShippingCityIds.Serialize(request.CityIds);
                data.MinPrice = request.MinPrice;
                data.Conditions = ShippingConditions.Serialize(request.Conditions);

                _context.SetUserId(currentUserId);
                _context.Shippings.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "روش ارسال با موفقیت ویرایش شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = request.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);

                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
        }

        // -------------------- Delete --------------------
        public ResultDto RemoveShipping(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.Shippings.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "روش ارسال یافت نشد",
                        Status = 200,
                    };

                // Baskets reference their shipping, so don't delete one that is in use
                var inUse = _context.Baskets.Any(b => b.Shipping != null && b.Shipping.Id == id);

                if (inUse)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "این روش ارسال در سبدهای خرید استفاده شده و قابل حذف نیست",
                        Status = 200,
                    };

                _context.SetUserId(currentUserId);
                _context.Shippings.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "روش ارسال با موفقیت حذف شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = id.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);

                _context.SaveChanges();

                return new ResultDto
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
        }
    }

    // ==================== DTOs ====================

    public class RequestAddShippingDto
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        public string Name { get; set; }

        public string? Description { get; set; }

        public List<long>? CityIds { get; set; }

        public int? MinPrice { get; set; }

        [Required(ErrorMessage = "شرایط قیمت‌گذاری الزامی است.")]
        public List<WeightPriceTierDto> Conditions { get; set; } = new();
    }

    public class RequestEditShippingDto
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "نام الزامی است.")]
        public string Name { get; set; }

        public string? Description { get; set; }

        public List<long>? CityIds { get; set; }

        public int? MinPrice { get; set; }

        [Required(ErrorMessage = "شرایط قیمت‌گذاری الزامی است.")]
        public List<WeightPriceTierDto> Conditions { get; set; } = new();
    }
}