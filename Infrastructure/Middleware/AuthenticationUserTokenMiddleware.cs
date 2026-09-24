using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class AuthenticationUserTokenMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationUserTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        //if (!context.Request.Headers.ContainsKey("UserToken"))
        //{
        //    //var cookieOptions = new CookieOptions
        //    //{
        //    //    Expires = DateTimeOffset.Now.AddDays(7), // Set the expiration time for the cookie
        //    //    Domain = "localhost", // Set the domain for the cookie
        //    //    Path = "/", // Set the path for the cookie
        //    //    Secure = false, // Set the secure flag for the cookie
        //    //    HttpOnly = true, // Set the HttpOnly flag for the cookie
        //    //    SameSite = SameSiteMode.Lax,
        //    //    IsEssential = true,
        //    //    MaxAge = TimeSpan.MaxValue

        //    //};
        //    // ایجاد کوکی برای شناسایی کاربر جدید
        //    string userToken = Guid.NewGuid().ToString();

        //    var data = context.Response.Cookies["UserToken"]
        //    if ( == null)
        //        context.Response.Cookies.Append("UserToken", userToken);
           
        //}

        await _next(context);
    }
}
