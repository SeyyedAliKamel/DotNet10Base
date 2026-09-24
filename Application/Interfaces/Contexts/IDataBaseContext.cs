using Domain.Entities.AdditionalInformations;
using Domain.Entities.Baskets;
using Domain.Entities.Branches;
using Domain.Entities.Contents;
using Domain.Entities.HandleException;
using Domain.Entities.Payments;
using Domain.Entities.Products;
using Domain.Entities.Scores;
using Domain.Entities.Shippings;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Interfaces.Contexts
{
    public interface IDataBaseContext
    {
        DbSet<User> Users { get; set; }
        DbSet<UserAddress> UserAddresses { get; set; }
        DbSet<UserSession> UserSessions { get; set; }
        DbSet<HandleException> HandleExceptions { get; set; }
        DbSet<Score> Scores { get; set; }
        DbSet<LogActivity> LogActivities { get; set; }
        DbSet<LoginLog> LoginLogs { get; set; }
        DbSet<Role> Roles { get; set; }
        DbSet<Permission> Permissions { get; set; }
        DbSet<PermissionCategory> PermissionCategories { get; set; }
        DbSet<Slug> Slugs { get; set; }

        DbSet<Hashtag> Hashtags { get; set; }
        DbSet<Brand> Brands { get; set; }
        DbSet<Product> Products { get; set; }
        DbSet<ProductCategory> ProductCategories { get; set; }
        DbSet<ProductHashtag> ProductHashtags { get; set; }
        DbSet<ProductOffer> ProductOffers { get; set; }
        DbSet<ProductRelation> ProductRelations { get; set; }
        DbSet<ProductWish> ProductWishes { get; set; }
        DbSet<ProductProductCategory> ProductProductCategories { get; set; }

        DbSet<Content> Contents { get; set; }
        DbSet<ContentCategory> ContentCategories { get; set; }
        DbSet<ContentRelation> ContentRelations { get; set; }

        DatabaseFacade Database { get; }
        ChangeTracker ChangeTracker { get; }
        DbSet<Banner> Banners { get; set; }

        DbSet<Basket> Baskets { get; set; }
        DbSet<BasketItem> BasketItems { get; set; }
        DbSet<BasketPayment> BasketPayments { get; set; }
        DbSet<BasketStatusLog> BasketStatusLogs { get; set; }
        DbSet<Shipping> Shippings { get; set; }

        DbSet<City> Cities { get; set; }
        DbSet<State> States { get; set; } 
        DbSet<Menu> Menus { get; set; }
        DbSet<Branch> Branches { get; set; }
        DbSet<Image> Images { get; set; }
        DbSet<Review> Reviews { get; set; }
        DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        DbSet<ContactUs> ContactUses { get; set; }
        DbSet<Setting> Settings { get; set; }



        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        void SetUserId(long userId);
    }
}