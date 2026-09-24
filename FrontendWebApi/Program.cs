using Application.ConnectedServices;
using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.AdditionalInformations.Queries;
using Application.Services.Baskets.Commands;
using Application.Services.Baskets.Queries;
using Application.Services.Branches.Commands;
using Application.Services.Branches.Queries;
using Application.Services.Campaigns.Commands;
using Application.Services.Campaigns.Queries;
using Application.Services.Contents.Commands;
using Application.Services.Contents.Queries;
using Application.Services.Discounts.Commands;
using Application.Services.Discounts.Queries;
using Application.Services.Images.Queries;
using Application.Services.Lottery.Commands;
using Application.Services.Lottery.Queries;
using Application.Services.Payments.Queries;
using Application.Services.PermissionCategories.Commands;
using Application.Services.PermissionCategories.Queries;
using Application.Services.Permissions.Commands;
using Application.Services.Permissions.Queries;
using Application.Services.Products.Commands;
using Application.Services.Products.Queries;
using Application.Services.Roles.Commands;
using Application.Services.Roles.Queries;
using Application.Services.Scores.Commands;
using Application.Services.Scores.Queries;
using Application.Services.Search.Queries;
using Application.Services.SeedData;
using Application.Services.Shippings.Commands;
using Application.Services.Shippings.Queries;
using Application.Services.Surveys.Commands;
using Application.Services.Surveys.Queries;
using Application.Services.UserAddresses.Commands;
using Application.Services.UserAddresses.Queries;
using Application.Services.Users.Commands;
using Application.Services.Users.Queries;
using Application.Services.Wallets.Commands;
using Application.Services.Wallets.Queries;
using Infrastructure.ExternalApi.FileServer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Persistence.Contexts;



var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DataBaseContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        x => x.UseNetTopologySuite()
    ));

// DB
builder.Services.AddScoped<IDataBaseContext>(sp => sp.GetRequiredService<DataBaseContext>());

// Seed Services
builder.Services.AddScoped<ISeedDataService, SeedDataService>();

// User Services
builder.Services.AddScoped<IUserCUDService, UserCUDService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserAddressCUDService, UserAddressCUDService>();
builder.Services.AddScoped<IUserAddressService, UserAddressService>();

// Wallet Services
builder.Services.AddScoped<IWalletCUDService, WalletCUDService>();
builder.Services.AddScoped<IWalletService, WalletService>();

// Score Services
builder.Services.AddScoped<IScoreCUDService, ScoreCUDService>();
builder.Services.AddScoped<IScoreService, ScoreService>();

// ManageScore Services
builder.Services.AddScoped<IManageScoreCUDService, ManageScoreCUDService>();
builder.Services.AddScoped<IManageScoreService, ManageScoreService>();

//Role
builder.Services.AddScoped<IRoleCUDService, RoleCUDService>();
builder.Services.AddScoped<IRoleService, RoleService>();

//Permission
builder.Services.AddScoped<IPermissionCUDService, PermissionCUDService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

//PermissionCategory
builder.Services.AddScoped<IPermissionCategoryCUDService, PermissionCategoryCUDService>();
builder.Services.AddScoped<IPermissionCategoryService, PermissionCategoryService>();

//Content
builder.Services.AddScoped<IContentCUDService, ContentCUDService>();
builder.Services.AddScoped<IContentCategoryCUDService, ContentCategoryCUDService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentCategoryService, ContentCategoryService>();

//Product
builder.Services.AddScoped<IProductCUDService, ProductCUDService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBrandCUDService, BrandCUDService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IHashtagCUDService, HashtagCUDService>();
builder.Services.AddScoped<IHashtagService, HashtagService>();
builder.Services.AddScoped<IProductOfferCUDService, ProductOfferCUDService>();
builder.Services.AddScoped<IProductOfferService, ProductOfferService>();
builder.Services.AddScoped<IProductCategoryCUDService, ProductCategoryCUDService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();

//Basket
builder.Services.AddScoped<IBasketCUDService, BasketCUDService>();
builder.Services.AddScoped<IBasketService, BasketService>();
builder.Services.AddScoped<IShippingCUDService, ShippingCUDService>();
builder.Services.AddScoped<IShippingService, ShippingService>();

//Banner
builder.Services.AddScoped<IBannerCUDService, BannerCUDService>();
builder.Services.AddScoped<IBannerService, BannerService>();

//Menu
builder.Services.AddScoped<IMenuCUDService, MenuCUDService>();
builder.Services.AddScoped<IMenuService, MenuService>();

//Branch
builder.Services.AddScoped<IBranchCUDService, BranchCUDService>();
builder.Services.AddScoped<IBranchService, BranchService>();

//Campaign
builder.Services.AddScoped<ICampaignCUDService, CampaignCUDService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();

//Slug
builder.Services.AddScoped<ISlugCUDService, SlugCUDService>();
builder.Services.AddScoped<ISlugService, SlugService>();

//Geo
builder.Services.AddScoped<IGeoCUDService, GeoCUDService>();
builder.Services.AddScoped<IGeoService, GeoService>();

//Image
builder.Services.AddScoped<IImageCUDService, ImageCUDService>();
builder.Services.AddScoped<IImageService, ImageService>();

//Review
builder.Services.AddScoped<IReviewCUDService, ReviewCUDService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

//Discount
builder.Services.AddScoped<IDiscountCUDService, DiscountCUDService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();

//ContactUs
builder.Services.AddScoped<IContactUsCUDService, ContactUsCUDService>();
builder.Services.AddScoped<IContactUsService, ContactUsService>();

//Setting
builder.Services.AddScoped<ISettingCUDService, SettingCUDService>();
builder.Services.AddScoped<ISettingService, SettingService>();

//Lottery
builder.Services.AddScoped<ILotteryCUDService, LotteryCUDService>();
builder.Services.AddScoped<ILotteryService, LotteryService>();

//Survey
builder.Services.AddScoped<ISurveyCUDService, SurveyCUDService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();

builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISadadGatewayClient, SadadGatewayClient>();
builder.Services.AddScoped<ISepGatewayClient, SepGatewayClient>();





builder.Services.AddCors();

builder.Services.AddControllers()
    .AddMvcOptions(options =>
    {
        var provider = options.ModelBindingMessageProvider;

        provider.SetValueMustNotBeNullAccessor(_ =>
            "این فیلد الزامی است.");

        provider.SetMissingBindRequiredValueAccessor(name =>
            $"مقدار '{name}' الزامی است.");

        provider.SetAttemptedValueIsInvalidAccessor((value, name) =>
            $"مقدار '{value}' برای '{name}' معتبر نیست.");

        provider.SetValueIsInvalidAccessor(value =>
            $"مقدار '{value}' معتبر نیست.");

        provider.SetMissingKeyOrValueAccessor(() =>
            "کلید یا مقدار ارسال نشده است.");

        provider.SetUnknownValueIsInvalidAccessor(name =>
            $"مقدار '{name}' معتبر نیست.");
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(kvp => new
            {
                Field = kvp.Key,
                Messages = kvp.Value.Errors.Select(e => e.ErrorMessage)
            });

        return new BadRequestObjectResult(new
        {
            Success = false,
            Message = "اطلاعات ارسالی معتبر نیست.",
            Errors = errors
        });
    };
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ssb",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

if (Environment.UserName == "SAK")
{
    builder.WebHost.UseKestrel();
    builder.WebHost.UseContentRoot(Directory.GetCurrentDirectory());
    builder.WebHost.UseUrls("https://192.168.12.173:5001;http://192.168.12.173:5000");
    builder.WebHost.UseIISIntegration();
}
else if (Environment.UserName == "bahram")
{
    builder.WebHost.UseKestrel();
    builder.WebHost.UseContentRoot(Directory.GetCurrentDirectory());
    builder.WebHost.UseUrls("https://192.168.12.147:5001;http://192.168.12.147:5000");
    builder.WebHost.UseIISIntegration();
}

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//else
//{
//    //app.UseSwagger();
//    //app.UseSwaggerUI();
//    app.UseExceptionHandler("/");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseExceptionHandler("/");
    app.UseHsts();
}


using (var scope = app.Services.CreateScope())
{
    var seedDataService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
    seedDataService.Ensure();
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Kestrel is only reachable through the local Nginx reverse proxy, so trust it
    // regardless of its address; otherwise X-Forwarded-Proto from Nginx is ignored,
    // UseHttpsRedirection() below thinks the request is plain HTTP and issues a
    // redirect, and the browser fails the preflight for LoginWithOtp with a CORS error.
    KnownNetworks = { },
    KnownProxies = { }
});



app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("x-filename")
    .WithExposedHeaders("x-error")
    .SetIsOriginAllowed(origin => true) // allow any origin
    .AllowCredentials());

app.Use(async (context, next) =>
{
    if (!context.Request.Cookies.ContainsKey("PublicToken"))
    {
        context.Response.Cookies.Append(
            "PublicToken",
            Guid.NewGuid().ToString("N"), // 32-character token
            new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false, // JS can still read it like your original code
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });
    }

    await next();
});

//app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();