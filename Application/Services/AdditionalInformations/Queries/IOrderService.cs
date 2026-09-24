using Application.Interfaces.Contexts;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.HandleException;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Application.Services.Orders.Commands
{
    // -------------------- Service --------------------

    public interface IOrderService
    {
        ResultDto<bool> EditOrder(RequestEditOrderDto request, long currentUserId);
        ResultDto<bool> FillNulls(long currentUserId);
        ResultDto<bool> ResetOrder(OrderTypeEnum type, long currentUserId);
    }

    public class OrderService : IOrderService
    {
        private readonly IDataBaseContext _context;

        public OrderService(IDataBaseContext context)
        {
            _context = context;
        }

        public ResultDto<bool> EditOrder(RequestEditOrderDto request, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                if (request.OrderDesired.HasValue)
                {
                    Dispatch(request.Type,
                        () => MoveToPosition(_context.Products, request.ItemId, request.OrderDesired.Value),
                        () => MoveToPosition(_context.ProductCategories, request.ItemId, request.OrderDesired.Value),
                        () => MoveToPosition(_context.Contents, request.ItemId, request.OrderDesired.Value),
                        () => MoveToPosition(_context.ContentCategories, request.ItemId, request.OrderDesired.Value),
                        () => MoveToPosition(_context.Brands, request.ItemId, request.OrderDesired.Value));
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.Action))
                    {
                        return new ResultDto<bool>
                        {
                            IsSuccess = false,
                            Message = "نوع عملیات (افزایش/کاهش) مشخص نشده است",
                            Status = 200
                        };
                    }

                    var action = request.Action.Trim().ToLowerInvariant();

                    if (action != "increase" && action != "decrease")
                    {
                        return new ResultDto<bool>
                        {
                            IsSuccess = false,
                            Message = "مقدار Action باید increase یا decrease باشد",
                            Status = 200
                        };
                    }

                    Dispatch(request.Type,
                        () => Swap(_context.Products, request.ItemId, action),
                        () => Swap(_context.ProductCategories, request.ItemId, action),
                        () => Swap(_context.Contents, request.ItemId, action),
                        () => Swap(_context.ContentCategories, request.ItemId, action),
                        () => Swap(_context.Brands, request.ItemId, action));
                }

                _context.SaveChanges();
                transaction.Commit();

                return new ResultDto<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "ترتیب با موفقیت به‌روزرسانی شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<bool>(ex, request.ToJson(), currentUserId);
            }
        }

        public ResultDto<bool> FillNulls(long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                FillNullsFor(_context.Products);
                FillNullsFor(_context.ProductCategories);
                FillNullsFor(_context.Contents);
                FillNullsFor(_context.ContentCategories);
                FillNullsFor(_context.Brands);

                transaction.Commit();

                return new ResultDto<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "مقادیر خالی ترتیب برای همه جداول پر شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<bool>(ex, null, currentUserId);
            }
        }

        public ResultDto<bool> ResetOrder(OrderTypeEnum type, long currentUserId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                Dispatch(type,
                    () => ResetOrderFor(_context.Products),
                    () => ResetOrderFor(_context.ProductCategories),
                    () => ResetOrderFor(_context.Contents),
                    () => ResetOrderFor(_context.ContentCategories),
                    () => ResetOrderFor(_context.Brands));

                transaction.Commit();

                return new ResultDto<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "ترتیب بازنشانی شد",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return HandleException<bool>(ex, type.ToString(), currentUserId);
            }
        }

        // -------------------- Dispatch helper --------------------

        private void Dispatch(OrderTypeEnum type, Action product, Action productCategory, Action content, Action contentCategory, Action brand)
        {
            switch (type)
            {
                case OrderTypeEnum.Product: product(); break;
                case OrderTypeEnum.ProductCategory: productCategory(); break;
                case OrderTypeEnum.Content: content(); break;
                case OrderTypeEnum.ContentCategory: contentCategory(); break;
                case OrderTypeEnum.Brand: brand(); break;
            }
        }

        // -------------------- Generic helpers (no interface, no cast — EF.Property by name) --------------------

        // action is "increase" or "decrease" (already validated/lowercased by caller).
        // "increase" => swap with the next HIGHER order (moves later in the list).
        // "decrease" => swap with the next LOWER order (moves earlier in the list).
        private void Swap<TEntity>(DbSet<TEntity> set, long itemId, string action)
            where TEntity : class
        {
            var target = set.FirstOrDefault(x => EF.Property<long>(x, "Id") == itemId);
            if (target == null)
                throw new Exception("آیتم مورد نظر یافت نشد");

            var targetOrder = GetOrder(target);
            if (targetOrder == null)
                throw new Exception("ترتیب این مورد خالی است");

            var neighbor = action == "increase"
                ? set.Where(x => EF.Property<long?>(x, "Order") > targetOrder)
                     .OrderBy(x => EF.Property<long?>(x, "Order")).FirstOrDefault()
                : set.Where(x => EF.Property<long?>(x, "Order") < targetOrder)
                     .OrderByDescending(x => EF.Property<long?>(x, "Order")).FirstOrDefault();

            if (neighbor == null)
                return;

            var neighborOrder = GetOrder(neighbor);
            if (neighborOrder == null)
                throw new Exception("ترتیب این مورد خالی است");

            SetOrder(target, neighborOrder);
            SetOrder(neighbor, targetOrder);
        }

        private void MoveToPosition<TEntity>(DbSet<TEntity> set, long itemId, long desiredOrder)
            where TEntity : class
        {
            var target = set.FirstOrDefault(x => EF.Property<long>(x, "Id") == itemId);
            if (target == null)
                throw new Exception("آیتم مورد نظر یافت نشد");

            var oldOrder = GetOrder(target) ?? GetId(target);

            if (desiredOrder == oldOrder)
                return;

            if (desiredOrder > oldOrder)
            {
                set.Where(x => EF.Property<long>(x, "Id") != itemId
                             && EF.Property<long?>(x, "Order") > oldOrder
                             && EF.Property<long?>(x, "Order") <= desiredOrder)
                   .ExecuteUpdate(x => x.SetProperty(p => EF.Property<long?>(p, "Order"), p => EF.Property<long?>(p, "Order") - 1));
            }
            else
            {
                set.Where(x => EF.Property<long>(x, "Id") != itemId
                             && EF.Property<long?>(x, "Order") >= desiredOrder
                             && EF.Property<long?>(x, "Order") < oldOrder)
                   .ExecuteUpdate(x => x.SetProperty(p => EF.Property<long?>(p, "Order"), p => EF.Property<long?>(p, "Order") + 1));
            }

            SetOrder(target, desiredOrder);
        }

        private void FillNullsFor<TEntity>(DbSet<TEntity> set) where TEntity : class
        {
            set.Where(x => EF.Property<long?>(x, "Order") == null)
               .ExecuteUpdate(x => x.SetProperty(p => EF.Property<long?>(p, "Order"), p => EF.Property<long>(p, "Id")));
        }

        private void ResetOrderFor<TEntity>(DbSet<TEntity> set) where TEntity : class
        {
            set.ExecuteUpdate(x => x.SetProperty(p => EF.Property<long?>(p, "Order"), p => EF.Property<long>(p, "Id")));
        }

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

        // Looks up the entity's tracked EntityEntry via ChangeTracker (since IDataBaseContext
        // doesn't expose .Entry() directly). Works because target/neighbor entities were
        // fetched via a tracked query (no AsNoTracking), so EF is already watching them.
        private Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity)
            where TEntity : class
        {
            var entry = _context.ChangeTracker.Entries<TEntity>()
                .FirstOrDefault(e => ReferenceEquals(e.Entity, entity));

            if (entry == null)
                throw new Exception($"موجودیت از نوع {typeof(TEntity).Name} تحت ردیابی نیست");

            return entry;
        }

        private long? GetOrder<TEntity>(TEntity entity) where TEntity : class
            => (long?)GetEntry(entity).Property("Order").CurrentValue;

        private long GetId<TEntity>(TEntity entity) where TEntity : class
            => (long)GetEntry(entity).Property("Id").CurrentValue!;

        private void SetOrder<TEntity>(TEntity entity, long? value) where TEntity : class
        {
            GetEntry(entity).Property("Order").CurrentValue = value;
        }
    }

    public class RequestEditOrderDto
    {
        public string? Action { get; set; }
        public long ItemId { get; set; }
        public long? OrderDesired { get; set; }
        public OrderTypeEnum Type { get; set; }
    }

}