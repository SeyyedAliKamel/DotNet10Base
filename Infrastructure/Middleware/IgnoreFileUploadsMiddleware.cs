using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class IgnoreFileUploadsMiddleware
{
    private readonly RequestDelegate _next;

    public IgnoreFileUploadsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.HasFormContentType)
        {
            var contentType = context.Request.ContentType.ToLower();
            if (contentType.StartsWith("multipart/form-data"))
            {
                // Do nothing and return immediately
                context.Response.StatusCode = 404;
            
                return;
            }
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }
}
