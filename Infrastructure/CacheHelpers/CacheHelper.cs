using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.CacheHelpers
{
    public static class CacheHelper
    {
        public static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromDays(1);    
        public static string GenerateHomePageCacheKey()
        {
            return "HomePage";
        }
    }
}
 