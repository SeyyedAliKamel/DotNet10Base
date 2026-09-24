using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using static Common.Helper;

namespace Application.Services.Products.Commands
{
    // ==================== Interface ====================
    public interface ISlugCUDService
    {
        ResultDto<SlugResultDto> AddSlug(RequestAddSlugDto request, long currentUserId);
        ResultDto<SlugResultDto> EditSlug(RequestEditSlugDto request, long currentUserId);
        ResultDto<SlugResultDto> RemoveSlug(long id, long currentUserId);
        ResultDto<bool> RemoveSlugsByReference(SlugTypeEnum type, long referenceId, long currentUserId);
    }

    // ==================== Implementation ====================
    public class SlugCUDService : ISlugCUDService
    {
        private readonly IDataBaseContext _context;

        public SlugCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto<SlugResultDto> AddSlug(RequestAddSlugDto request, long currentUserId)
        {
            var (transaction, ownsTransaction) = GetOrBeginTransaction();

            try
            {
                var normalizedTitle = NormalizeSlugTitle(request.Title);

                if (string.IsNullOrWhiteSpace(normalizedTitle))
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<SlugResultDto>
                    {
                        IsSuccess = false,
                        Message = "عنوان اسلاگ نمی‌تواند خالی باشد",
                        Status = 200,
                    };
                }

                var exists = _context.Slugs.Any(x => x.Title == normalizedTitle);

                if (exists)
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<SlugResultDto>
                    {
                        IsSuccess = false,
                        Message = "اسلاگ تکراری هست",
                        Status = 200,
                    };
                }

                if (!ReferenceExists(request.Type, request.ReferenceId))
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<SlugResultDto>
                    {
                        IsSuccess = false,
                        Message = "موردی که اسلاگ به آن اشاره دارد یافت نشد",
                        Status = 200,
                    };
                }

                var slug = new Slug
                {
                    Title = normalizedTitle,
                    Type = request.Type,
                    ReferenceId = request.ReferenceId
                };

                _context.SetUserId(currentUserId);
                _context.Slugs.Add(slug);
                _context.SaveChanges();

                CommitIfOwned(transaction, ownsTransaction);

                return new ResultDto<SlugResultDto>
                {
                    Data = MapToResult(slug),
                    IsSuccess = true,
                    Message = "اطلاعات اسلاگ با موفقیت ثبت شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                RollbackIfOwned(transaction, ownsTransaction);
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

                return new ResultDto<SlugResultDto>
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
            finally
            {
                DisposeIfOwned(transaction, ownsTransaction);
            }
        }

        // -------------------- Update --------------------
        public ResultDto<SlugResultDto> EditSlug(RequestEditSlugDto request, long currentUserId)
        {
            var (transaction, ownsTransaction) = GetOrBeginTransaction();

            try
            {
                var data = _context.Slugs.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<SlugResultDto>
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "اطلاعات اشتباه است "
                    };
                }

                var normalizedTitle = NormalizeSlugTitle(request.Title);

                if (string.IsNullOrWhiteSpace(normalizedTitle))
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<SlugResultDto>
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "عنوان اسلاگ نمی‌تواند خالی باشد"
                    };
                }

                var duplicate = _context.Slugs.Any(x =>
                    x.Id != request.Id &&
                    x.Title == normalizedTitle);

                if (duplicate)
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<SlugResultDto>
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "اسلاگ تکراری است"
                    };
                }

                //if (!ReferenceExists(request.Type, request.ReferenceId))
                //{
                //    CommitIfOwned(transaction, ownsTransaction);
                //    return new ResultDto<SlugResultDto>
                //    {
                //        IsSuccess = false,
                //        Status = 200,
                //        Message = "موردی که اسلاگ به آن اشاره دارد یافت نشد"
                //    };
                //}

                data.Title = normalizedTitle;

                _context.SetUserId(currentUserId);
                _context.Slugs.Update(data);
                _context.SaveChanges();

                CommitIfOwned(transaction, ownsTransaction);

                return new ResultDto<SlugResultDto>
                {
                    Data = MapToResult(data),
                    IsSuccess = true,
                    Status = 200,
                    Message = "اسلاگ با موفقیت ثبت شد"
                };
            }
            catch (Exception ex)
            {
                RollbackIfOwned(transaction, ownsTransaction);
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

                return new ResultDto<SlugResultDto>
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
            finally
            {
                DisposeIfOwned(transaction, ownsTransaction);
            }
        }

        // -------------------- Remove --------------------
        public ResultDto<SlugResultDto> RemoveSlug(long id, long currentUserId)
        {
            var (transaction, ownsTransaction) = GetOrBeginTransaction();

            try
            {
                var existing = _context.Slugs.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<SlugResultDto>
                    {
                        IsSuccess = false,
                        Message = "اسلاگ یافت نشد",
                        Status = 200,
                    };
                }

                var result = MapToResult(existing);

                _context.SetUserId(currentUserId);
                _context.Slugs.Remove(existing);
                _context.SaveChanges();

                CommitIfOwned(transaction, ownsTransaction);

                return new ResultDto<SlugResultDto>
                {
                    Data = result,
                    IsSuccess = true,
                    Message = "اسلاگ با موفقیت حذف شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                RollbackIfOwned(transaction, ownsTransaction);
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

                return new ResultDto<SlugResultDto>
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
            finally
            {
                DisposeIfOwned(transaction, ownsTransaction);
            }
        }

        // -------------------- Remove by Reference --------------------
        public ResultDto<bool> RemoveSlugsByReference(SlugTypeEnum type, long referenceId, long currentUserId)
        {
            var (transaction, ownsTransaction) = GetOrBeginTransaction();

            try
            {
                var slugs = _context.Slugs
                    .Where(x => x.Type == type && x.ReferenceId == referenceId)
                    .ToList();

                if (!slugs.Any())
                {
                    CommitIfOwned(transaction, ownsTransaction);
                    return new ResultDto<bool>
                    {
                        Data = true,
                        IsSuccess = true,
                        Message = "اسلاگی برای حذف یافت نشد",
                        Status = 200,
                    };
                }

                _context.Slugs.RemoveRange(slugs);
                _context.SaveChanges();

                CommitIfOwned(transaction, ownsTransaction);

                return new ResultDto<bool>
                {
                    Data = true,
                    IsSuccess = true,
                    Message = "اسلاگ‌های مرتبط با موفقیت حذف شدند",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                RollbackIfOwned(transaction, ownsTransaction);
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                var item = new HandleException
                {
                    Message = ex.InnerException?.Message.ToString(),
                    RequestParameter = new { Type = type, ReferenceId = referenceId }.ToJson(),
                    StackTrace = ex.StackTrace,
                    UserId = currentUserId,
                };
                _context.HandleExceptions.Add(item);
                _context.SaveChanges();

                return new ResultDto<bool>
                {
                    IsSuccess = false,
                    Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                    Status = 400,
                };
            }
            finally
            {
                DisposeIfOwned(transaction, ownsTransaction);
            }
        }

        // ==================== Transaction Helpers ====================

        /// <summary>
        /// Returns the ambient transaction if the context already has one open (e.g. this
        /// method was called from within another service's unit of work), otherwise begins
        /// and returns a new one that this method owns and is responsible for committing,
        /// rolling back, and disposing.
        /// </summary>
        private (IDbContextTransaction transaction, bool ownsTransaction) GetOrBeginTransaction()
        {
            var current = _context.Database.CurrentTransaction;
            return current != null
                ? (current, false)
                : (_context.Database.BeginTransaction(), true);
        }

        private static void CommitIfOwned(IDbContextTransaction transaction, bool ownsTransaction)
        {
            if (ownsTransaction)
                transaction.Commit();
        }

        private static void RollbackIfOwned(IDbContextTransaction transaction, bool ownsTransaction)
        {
            if (ownsTransaction)
                transaction.Rollback();
        }

        private static void DisposeIfOwned(IDbContextTransaction transaction, bool ownsTransaction)
        {
            if (ownsTransaction)
                transaction.Dispose();
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

        private static SlugResultDto MapToResult(Slug slug) => new()
        {
            Id = slug.Id,
            Title = slug.Title,
            Type = slug.Type,
            ReferenceId = slug.ReferenceId
        };
    }

    // ==================== DTOs ====================
    public class RequestAddSlugDto
    {
        [Required(ErrorMessage = "تیتر الزامی است.")]
        public string Title { get; set; }
        public SlugTypeEnum Type { get; set; }
        public long ReferenceId { get; set; }
    }

    public class RequestEditSlugDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
    }

    public class SlugResultDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public SlugTypeEnum Type { get; set; }
        public long ReferenceId { get; set; }
    }
}