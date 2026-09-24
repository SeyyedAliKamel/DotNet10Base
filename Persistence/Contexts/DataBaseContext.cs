using Application.Interfaces.Contexts;
using Application.Services.Roles.Queries;
using Domain.Attributes;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Baskets;
using Domain.Entities.Branches;
using Domain.Entities.Campaigns;
using Domain.Entities.Common;
using Domain.Entities.Contents;
using Domain.Entities.Discounts;
using Domain.Entities.HandleException;
using Domain.Entities.Lotteries;
using Domain.Entities.Lottery;
using Domain.Entities.Payments;
using Domain.Entities.Products;
using Domain.Entities.Scores;
using Domain.Entities.Shippings;
using Domain.Entities.Surveys;
using Domain.Entities.Users;
using Domain.Entities.Wallets;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PaypalServerSdk.Standard.Models;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection;

namespace Persistence.Contexts
{
    public class DataBaseContext : DbContext, IDataBaseContext
    {
        private long? _currentUserId;

        public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<HandleException> HandleExceptions { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<ScoreGift> ScoreGifts { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<LogActivity> LogActivities { get; set; }
        public DbSet<LoginLog> LoginLogs { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<PermissionCategory> PermissionCategories { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<Slug> Slugs { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductHashtag> ProductHashtags { get; set; }
        public DbSet<ProductOffer> ProductOffers { get; set; }
        public DbSet<ProductRelation> ProductRelations { get; set; }
        public DbSet<ProductWish> ProductWishes { get; set; }
        public DbSet<ProductProductCategory> ProductProductCategories { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<ContentCategory> ContentCategories { get; set; }
        public DbSet<ContentRelation> ContentRelations { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<Shipping> Shippings { get; set; }
        public DbSet<BasketItem> BasketItems { get; set; }
        public DbSet<BasketPayment> BasketPayments { get; set; }
        public DbSet<BasketStatusLog> BasketStatusLogs { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<ManageScore> ManageScores { get; set; }
        public DbSet<ContactUs> ContactUses { get; set; }
        public DbSet<BasketItemImage> BasketItemImages { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<LotteryDate> LotteryDates { get; set; }
        public DbSet<LotteryResult> LotteryResults { get; set; }
        public DbSet<SurveyAnswer> SurveyAnswers { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<SurveyQuestionOption> SurveyQuestionOptions { get; set; }
        public DbSet<SurveySubmission> SurveySubmissions { get; set; }
        public DbSet<ChequeImage> ChequeImages { get; set; }



        private void ApplyQueryFilter(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Role>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Permission>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<UserSession>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<PermissionCategory>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<UserAddress>().HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<Content>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<ContentCategory>().HasQueryFilter(p => !p.IsDeleted);

        }
        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder
               .Entity<User>()
               .HasMany(p => p.Roles)
               .WithMany(p => p.Users).UsingEntity(p => p.ToTable("role_users"));
            modelBuilder
              .Entity<Role>()
              .HasMany(p => p.Permissions)
              .WithMany(p => p.Roles).UsingEntity(p => p.ToTable("permission_roles")); ;
        }
        public void SetUserId(long userId)
        {
            _currentUserId = userId;
        }

        public override int SaveChanges()
        {
            AuditEntities();
            return base.SaveChanges();
        }

        private void AuditEntities()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedById = _currentUserId;
                        entry.Entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedById = _currentUserId;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = now;
                        entry.Entity.DeletedById = _currentUserId;
                        break;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(BuildIsDeletedFilter(entityType.ClrType));
                }
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var hasAuditableAttr = entityType.ClrType
                    .GetCustomAttribute<AuditableAttribute>() != null;

                if (hasAuditableAttr)
                {

                }
            }

            SeedData(modelBuilder);
            ApplyQueryFilter(modelBuilder);
        }

        private static System.Linq.Expressions.LambdaExpression BuildIsDeletedFilter(Type type)
        {
            var param = System.Linq.Expressions.Expression.Parameter(type, "x");
            var property = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.IsDeleted));
            var body = System.Linq.Expressions.Expression.Not(property);
            return System.Linq.Expressions.Expression.Lambda(body, param);
        }
    }
}