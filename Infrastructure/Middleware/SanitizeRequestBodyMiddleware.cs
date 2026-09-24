using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyApp.Infrastructure.Middleware
{
    public class SanitizeRequestBodyMiddleware
    {
        private readonly RequestDelegate _next;

        public SanitizeRequestBodyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method == "POST" || context.Request.Method == HttpMethods.Put)
            {
                var body = await GetRequestBodyAsync(context.Request.Body);
                var newBody = SanitizeRequestBody(body);
                var byteArray = Encoding.UTF8.GetBytes(newBody);
                context.Request.Body = new MemoryStream(byteArray);
            }

            await _next(context);
        }

        private string SanitizeRequestBody(string input)
        {
            // Replace potentially harmful code with safe equivalents
            return input.Replace("<script", string.Empty)
                        .Replace("script>", string.Empty)
                        .Replace(" on", string.Empty)
                        .Replace("<iframe", string.Empty)
                        .Replace("iframe>", string.Empty)
                        .Replace("<object", string.Empty)
                        .Replace("object>", string.Empty);
            // add more replacements as needed
        }

        private async Task<string> GetRequestBodyAsync(Stream body)
        {
            using var reader = new StreamReader(body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            body.Seek(0, SeekOrigin.Begin);
            return requestBody;
        }
    }
}