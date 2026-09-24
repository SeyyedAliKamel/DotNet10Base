using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using static Common.Helper;

namespace Application.Services.AdditionalInformations.Commands
{
    // ==================== Interface ====================
    public interface IContactUsCUDService
    {
        ResultDto AddContactUs(RequestAddContactUsDto request, long currentUserId);
        ResultDto EditContactUs(RequestEditContactUsDto request, long currentUserId);
        ResultDto RemoveContactUs(long id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class ContactUsCUDService : IContactUsCUDService
    {
        private readonly IDataBaseContext _context;

        public ContactUsCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto AddContactUs(RequestAddContactUsDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var (isValid, normalized) = MobileValidator.Normalize(request.Phone);
                if (!isValid)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "شماره موبایل معتبر نیست"
                    };

                if (!string.IsNullOrWhiteSpace(request.Email) &&
                    !Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "ایمیل معتبر نیست"
                    };
                }

                var contactUs = new ContactUs
                {
                    Name = request.Name,
                    Phone = normalized,
                    Email = request.Email,
                    Description = request.Description,
                };

                _context.SetUserId(currentUserId);
                _context.ContactUses.Add(contactUs);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات تماس با موفقیت ثبت شد"
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
        public ResultDto EditContactUs(RequestEditContactUsDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.ContactUses.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "اطلاعات اشتباه است"
                    };

                var (isValid, normalized) = MobileValidator.Normalize(request.Phone);
                if (!isValid)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "شماره موبایل معتبر نیست"
                    };

                if (!string.IsNullOrWhiteSpace(request.Email) &&
                    !Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "ایمیل معتبر نیست"
                    };
                }

                data.Name = request.Name;
                data.Phone = normalized;
                data.Email = request.Email;
                data.Description = request.Description;

                _context.SetUserId(currentUserId);
                _context.ContactUses.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Status = 200,
                    Message = "اطلاعات تماس با موفقیت ویرایش شد"
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
        public ResultDto RemoveContactUs(long id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.ContactUses.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto
                    {
                        IsSuccess = false,
                        Message = "اطلاعات تماس یافت نشد",
                        Status = 200,
                    };

                _context.SetUserId(currentUserId);
                _context.ContactUses.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto
                {
                    IsSuccess = true,
                    Message = "اطلاعات تماس با موفقیت حذف شد",
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

    public class RequestAddContactUsDto
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        public string Phone { get; set; }

        public string? Email { get; set; }

        [Required(ErrorMessage = "توضیحات الزامی است.")]
        public string Description { get; set; }
    }

    public class RequestEditContactUsDto
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "نام الزامی است.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        public string Phone { get; set; }

        public string? Email { get; set; }

        [Required(ErrorMessage = "توضیحات الزامی است.")]
        public string Description { get; set; }
    }
}