using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Cookies
{
    public interface ICookieService
    {
        string GetCookie(string cookieName);
        string GetCookie1(string cookieName);
    }
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetCookie(string cookieName)
        {
            return _httpContextAccessor.HttpContext.Request.Headers[cookieName];
        }
        public string GetCookie1(string cookieName)
        {
            return _httpContextAccessor.HttpContext.Request.Cookies[cookieName];
        }
    }

   

}
