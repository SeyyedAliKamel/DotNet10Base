//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Infrastructure.IdentityConfigs
//{
//    public static class IdentityConfig
//    {
//        public static IServiceCollection AddIdentitySrvice(this IServiceCollection services ,IConfiguration configuration)
//        {

//            var connectionString = configuration.GetConnectionString("Context");

//           // builder.Services.AddDbContext<DataBaseContext>(options => options.UseSqlServer(connectionString));
//            services.AddDbContext<IdentityDataBaseContext>(options => options.UseSqlServer(connectionString));

//            services.AddIdentity<User, IdentityRole>()
//                .AddEntityFrameworkStores<IdentityDataBaseContext>()
//                .AddDefaultTokenProviders()
//                .AddRoles<IdentityRole>()
//                .AddErrorDescriber<IdentityErrorDescriber>();

//            services.Configure<IdentityOptions>(options =>
//            {
//                options.Password.RequireDigit = false;
//                options.Password.RequiredLength = 8;
//                options.Password.RequireLowercase = false;
//                options.Password.RequireNonAlphanumeric = false;
//                options.Password.RequireUppercase = false;
//                options.Password.RequiredUniqueChars = 1;
//                options.Password.RequireNonAlphanumeric = false;
//                options.User.RequireUniqueEmail = true;
//                options.Lockout.MaxFailedAccessAttempts = 5;
//                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
//            });
//            return services;
//        } 
//    }
//}
  