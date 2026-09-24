using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using static Common.Helper;

namespace Application.Services.AdditionalInformations.Commands
{
    // ==================== Interface ====================
    public interface IGeoCUDService
    {
        // -------- State --------
        ResultDto AddState(RequestAddStateDto request, long currentUserId);
        ResultDto EditState(RequestEditStateDto request, long currentUserId);
        ResultDto RemoveState(long id, long currentUserId);

        // -------- City --------
        ResultDto AddCity(RequestAddCityDto request, long currentUserId);
        ResultDto EditCity(RequestEditCityDto request, long currentUserId);
        ResultDto RemoveCity(long id, long currentUserId);

        // -------- Status --------
        ResultDto UpdateGeoStatus(RequestToggleGeoStatusDto request, long currentUserId);
    }

    // ==================== Implementation ====================
    public class GeoCUDService : IGeoCUDService
    {
        private readonly IDataBaseContext _context;

        public GeoCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Add State --------------------
        public ResultDto AddState(RequestAddStateDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var duplicate = _context.States.Any(x => x.Name == request.Name);
                if (duplicate)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "استانی با این نام قبلا ثبت شده است"
                    };
                }

                var state = new State
                {
                    Name = request.Name,
                    IsActive = request.IsActive,
                };

                _context.SetUserId(currentUserId);
                _context.States.Add(state);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات استان با موفقیت ثبت شد"
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

        // -------------------- Edit State --------------------
        public ResultDto EditState(RequestEditStateDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.States.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "استان یافت نشد"
                    };

                var duplicate = _context.States.Any(x => x.Name == request.Name && x.Id != request.Id);
                if (duplicate)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "استانی با این نام قبلا ثبت شده است"
                    };
                }

                data.Name = request.Name;
                data.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.States.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "استان با موفقیت ویرایش شد"
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

        // -------------------- Remove State --------------------
        public ResultDto RemoveState(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.States.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "استان یافت نشد",
                        Status = 200,
                    };

                var hasCities = _context.Cities.Any(x => x.StateId == id);
                if (hasCities)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "ابتدا شهرهای این استان را حذف کنید",
                        Status = 200,
                    };
                }

                _context.SetUserId(currentUserId);
                _context.States.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "استان با موفقیت حذف شد",
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

        // -------------------- Add City --------------------
        public ResultDto AddCity(RequestAddCityDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var state = _context.States.FirstOrDefault(x => x.Id == request.StateId);
                if (state is null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "استان یافت نشد"
                    };
                }

                var duplicate = _context.Cities.Any(x => x.Name == request.Name && x.StateId == request.StateId);
                if (duplicate)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "شهری با این نام در این استان قبلا ثبت شده است"
                    };
                }

                var city = new City
                {
                    Name = request.Name,
                    StateId = request.StateId,
                    IsActive = request.IsActive,
                };

                _context.SetUserId(currentUserId);
                _context.Cities.Add(city);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات شهر با موفقیت ثبت شد"
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

        // -------------------- Edit City --------------------
        public ResultDto EditCity(RequestEditCityDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.Cities.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "شهر یافت نشد"
                    };

                var state = _context.States.FirstOrDefault(x => x.Id == request.StateId);
                if (state is null)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "استان یافت نشد"
                    };
                }

                var duplicate = _context.Cities.Any(x =>
                    x.Name == request.Name &&
                    x.StateId == request.StateId &&
                    x.Id != request.Id);
                if (duplicate)
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "شهری با این نام در این استان قبلا ثبت شده است"
                    };
                }

                data.Name = request.Name;
                data.StateId = request.StateId;
                data.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.Cities.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "شهر با موفقیت ویرایش شد"
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

        // -------------------- Remove City --------------------
        public ResultDto RemoveCity(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.Cities.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "شهر یافت نشد",
                        Status = 200,
                    };

                _context.SetUserId(currentUserId);
                _context.Cities.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "شهر با موفقیت حذف شد",
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

        // -------------------- Toggle Active/Inactive --------------------
        public ResultDto UpdateGeoStatus(RequestToggleGeoStatusDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                if (request.Type == GeoTypeEnum.State)
                {
                    var state = _context.States
                        .Include(x => x.Cities)
                        .FirstOrDefault(x => x.Id == request.Id);

                    if (state is null)
                        return new ResultDto
                        {
                            IsSuccess = false,
                            Status = 200,
                            Message = "استان یافت نشد"
                        };

                    state.IsActive = request.IsActive;

                    // cascade the same status down to all cities of this state
                    if (state.Cities != null)
                    {
                        foreach (var city in state.Cities)
                        {
                            city.IsActive = request.IsActive;
                        }
                    }

                    _context.SetUserId(currentUserId);
                    _context.States.Update(state);
                }
                else if (request.Type == GeoTypeEnum.City)
                {
                    var city = _context.Cities.FirstOrDefault(x => x.Id == request.Id);

                    if (city is null)
                        return new ResultDto
                        {
                            IsSuccess = false,
                            Status = 200,
                            Message = "شهر یافت نشد"
                        };

                    city.IsActive = request.IsActive;

                    _context.SetUserId(currentUserId);
                    _context.Cities.Update(city);
                }
                else
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "نوع نامعتبر است"
                    };
                }

                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = request.IsActive ? "با موفقیت فعال شد" : "با موفقیت غیرفعال شد"
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
    }

    // ==================== DTOs ====================

    public class RequestAddStateDto
    {
        [Required(ErrorMessage = "نام استان الزامی است.")]
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestEditStateDto
    {
        public long Id { get; set; }
        [Required(ErrorMessage = "نام استان الزامی است.")]
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestAddCityDto
    {
        [Required(ErrorMessage = "نام شهر الزامی است.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "استان الزامی است.")]
        public long StateId { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestEditCityDto
    {
        public long Id { get; set; }
        [Required(ErrorMessage = "نام شهر الزامی است.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "استان الزامی است.")]
        public long StateId { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestToggleGeoStatusDto
    {
        public long Id { get; set; }
        public GeoTypeEnum Type { get; set; }
        public bool IsActive { get; set; }
    }

    public enum GeoTypeEnum
    {
        State = 1,
        City = 2
    }
}