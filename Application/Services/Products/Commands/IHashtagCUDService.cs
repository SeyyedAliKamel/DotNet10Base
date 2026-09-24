using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.HandleException;
using Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using static Common.Helper;

namespace Application.Services.Products.Commands
{
    // ==================== Interface ====================
    public interface IHashtagCUDService
    {
        ResultDto<HashtagItemDto> AddHashtag(HashtagRequestDto request, long currentUserId);
        ResultDto<HashtagItemDto> EditHashtag(int id, HashtagRequestDto request, long currentUserId);
        ResultDto<HashtagItemDto> RemoveHashtag(int id, long currentUserId);
    }

    // ==================== Implementation ====================
    public class HashtagCUDService : IHashtagCUDService
    {
        private readonly IDataBaseContext _context;

        public HashtagCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto<HashtagItemDto> AddHashtag(HashtagRequestDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var exists = _context.Hashtags.Any(x => x.Title == request.Title);

                if (exists)
                    return FailData<HashtagItemDto>("هشتگ تکراری است");

                var hashtag = new Hashtag
                {
                    Title = request.Title,
                    ShowMenu = request.ShowMenu,
                    IsActive = request.IsActive,
                };

                _context.SetUserId(currentUserId);
                _context.Hashtags.Add(hashtag);
                _context.SaveChanges();

                transaction.Commit();

                return OkData(MapToResult(hashtag), "هشتگ با موفقیت ایجاد گردید");
            }
            catch (Exception ex)
            {
                return HandleException<HashtagItemDto>(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Update --------------------
        public ResultDto<HashtagItemDto> EditHashtag(int id, HashtagRequestDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.Hashtags.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return FailData<HashtagItemDto>("هشتگ یافت نشد");

                var duplicate = _context.Hashtags.Any(x =>
                    x.Id != id &&
                    x.Title == request.Title);

                if (duplicate)
                    return FailData<HashtagItemDto>("هشتگ تکراری است");

                existing.Title = request.Title;
                existing.IsActive = request.IsActive;
                existing.ShowMenu = request.ShowMenu;

                _context.SetUserId(currentUserId);
                _context.Hashtags.Update(existing);
                _context.SaveChanges();

                transaction.Commit();

                return OkData(MapToResult(existing), "هشتگ با موفقیت به روز گردید");
            }
            catch (Exception ex)
            {
                return HandleException<HashtagItemDto>(transaction, ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Delete --------------------
        public ResultDto<HashtagItemDto> RemoveHashtag(int id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.Hashtags.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return FailData<HashtagItemDto>("هشتگ یافت نشد");

                var result = MapToResult(existing);

                _context.SetUserId(currentUserId);
                _context.Hashtags.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return OkData(result, "هشتگ با موفقیت حذف گردید");
            }
            catch (Exception ex)
            {
                return HandleException<HashtagItemDto>(transaction, ex, id.ToJson(), currentUserId);
            }
        }

        // ==================== Private Helpers ====================
        private static HashtagItemDto MapToResult(Hashtag hashtag) => new()
        {
            Id = hashtag.Id,
            Title = hashtag.Title,
            IsActive = hashtag.IsActive
        };

        private ResultDto<T> OkData<T>(T data, string message) =>
            new ResultDto<T>
            {
                Data = data,
                IsSuccess = true,
                Message = message,
                Status = 200
            };

        private ResultDto<T> FailData<T>(string message, int status = 400) =>
            new ResultDto<T>
            {
                Data = default,
                IsSuccess = false,
                Message = message,
                Status = status
            };

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
                entry.State = EntityState.Detached;
        }

        private ResultDto<T> HandleException<T>(
            IDbContextTransaction transaction,
            Exception ex,
            string requestParam,
            long? userId)
        {
            transaction.Rollback();

            DetachAll();

            _context.HandleExceptions.Add(new HandleException
            {
                Message = ex.InnerException?.Message ?? ex.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = userId,
            });

            _context.SaveChanges();

            return new ResultDto<T>
            {
                IsSuccess = false,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                Status = 500
            };
        }
    }

    // ==================== DTOs ====================

    public class HashtagRequestDto
    {
        [Required(ErrorMessage = "تیتر الزامی است.")]
        public string Title { get; set; }
        public bool ShowMenu { get; set; }
        public bool IsActive { get; set; }
    }

    public class HashtagItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public bool ShowMenu { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductHashtagDto
    {
        public long Id { get; set; }
        public long HashtagId { get; set; }
        public string Title { get; set; }
    }

    public class RequestAddProductHashtagDto
    {
        public long ProductId { get; set; }
        public long HashtagId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductHashtagResultDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public long HashtagId { get; set; }
    }
}