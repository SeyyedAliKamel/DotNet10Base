using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
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
    public interface IBrandCUDService
    {
        ResultDto<ResultBrandItemDto> AddBrand(RequestAddBrandDto request, long currentUserId);
        ResultDto<ResultBrandItemDto> EditBrand(RequestEditBrandDto request, long currentUserId);
        ResultDto<ResultBrandItemDto> RemoveBrand(int id, long currentUserId);

        ResultDto<bool> ResetOrder();
    }

    // ==================== Implementation ====================
    public class BrandCUDService : IBrandCUDService
    {
        private readonly IDataBaseContext _context;

        public BrandCUDService(IDataBaseContext context)
        {
            _context = context;
        }

        // -------------------- Create --------------------
        public ResultDto<ResultBrandItemDto> AddBrand(RequestAddBrandDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var exists = _context.Brands.Any(x => x.Title == request.Title);

                if (exists)
                    return new ResultDto<ResultBrandItemDto>
                    {
                        IsSuccess = false,
                        Message = "برند تکراری هست",
                        Status = 200,
                    };

                var brand = new Brand
                {
                    Title = request.Title,
                    IsActive = request.IsActive,
                };

                _context.SetUserId(currentUserId);
                _context.Brands.Add(brand);
                _context.SaveChanges();

                brand.Order = brand.Id;
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto<ResultBrandItemDto>
                {
                    Data = MapToResult(brand),
                    IsSuccess = true,
                    Message = "اطلاعات برند با موفقیت ثبت شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ResultBrandItemDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Update --------------------
        public ResultDto<ResultBrandItemDto> EditBrand(RequestEditBrandDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var data = _context.Brands.FirstOrDefault(x => x.Id == request.Id);

                if (data is null)
                    return new ResultDto<ResultBrandItemDto>
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "اطلاعات اشتباه است "
                    };

                var duplicate = _context.Brands.Any(x =>
                    x.Id != request.Id &&
                    x.Title == request.Title);

                if (duplicate)
                    return new ResultDto<ResultBrandItemDto>
                    {
                        IsSuccess = false,
                        Status = 200,
                        Message = "برند تکراری است"
                    };

                data.Title = request.Title;
                data.IsActive = request.IsActive;

                _context.SetUserId(currentUserId);
                _context.Brands.Update(data);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto<ResultBrandItemDto>
                {
                    Data = MapToResult(data),
                    IsSuccess = true,
                    Status = 200,
                    Message = "برند با موفقیت ثبت شد"
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ResultBrandItemDto>(ex, request.ToJson(), currentUserId);
            }
        }

        // -------------------- Remove --------------------
        public ResultDto<ResultBrandItemDto> RemoveBrand(int id, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var existing = _context.Brands.FirstOrDefault(x => x.Id == id);

                if (existing is null)
                    return new ResultDto<ResultBrandItemDto>
                    {
                        IsSuccess = false,
                        Message = "برند یافت نشد",
                        Status = 200,
                    };

                var affectedProducts = _context.Products
                    .Where(p => p.BrandId == existing.Id)
                    .ToList();

                foreach (var product in affectedProducts)
                {
                    product.BrandId = null;
                }

                var result = MapToResult(existing);

                _context.SetUserId(currentUserId);
                _context.Brands.Remove(existing);
                _context.SaveChanges();

                transaction.Commit();

                return new ResultDto<ResultBrandItemDto>
                {
                    Data = result,
                    IsSuccess = true,
                    Message = "برند با موفقیت حذف شد",
                    Status = 200,
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<ResultBrandItemDto>(ex, id.ToJson(), currentUserId);
            }
        }

        public ResultDto<bool> ResetOrder()
        {
            _context.Brands
                .Where(x => x.Order == null)
                .ExecuteUpdate(x => x.SetProperty(b => b.Order, b => b.Id));

            return new ResultDto<bool> { IsSuccess = true, Data = true, Status = 200 };
        }

        // ==================== Helpers ====================
        private static ResultBrandItemDto MapToResult(Brand brand) => new()
        {
            Id = brand.Id,
            Title = brand.Title,
            IsActive = brand.IsActive
        };

        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }

        private ResultDto<T> HandleException<T>(Exception ex, string requestParam, long currentUserId)
        {
            DetachAll();

            var item = new HandleException
            {
                Message = ex.InnerException?.Message ?? ex.Message,
                RequestParameter = requestParam,
                StackTrace = ex.StackTrace,
                UserId = currentUserId,
            };
            _context.HandleExceptions.Add(item);
            _context.SaveChanges();

            return new ResultDto<T>
            {
                IsSuccess = false,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد",
                Status = 400,
            };
        }
    }

    // ==================== DTOs ====================

    public class RequestAddBrandDto
    {
        [Required(ErrorMessage = "تیتر الزامی است.")]
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }
    public class RequestEditBrandDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }
    public class ResultBrandItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductBrandDto
    {
        public long ProductId { get; set; }
        public long BrandId { get; set; }
        public string Title { get; set; }
    }

    public class RequestAddProductBrandDto
    {
        public long ProductId { get; set; }
        public long BrandId { get; set; }
    }

    public class ProductBrandResultDto
    {
        public long ProductId { get; set; }
        public long BrandId { get; set; }
    }
}