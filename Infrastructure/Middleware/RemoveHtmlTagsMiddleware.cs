using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class RemoveHtmlTagsMiddleware
{
    private readonly RequestDelegate _next;

    public RemoveHtmlTagsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            // get the request body
            var requestBody = await GetRequestBody(context.Request);

            // remove HTML tags using regular expressions
            var cleanRequestBody = RemoveHtmlTags(requestBody);

            // replace the request body with the cleaned version
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(cleanRequestBody));
            context.Request.ContentLength = context.Request.Body.Length;
        }

        await _next(context);
    }

    private async Task<string> GetRequestBody(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private string RemoveHtmlTags(string input)
    {
        // define a regular expression pattern that matches HTML tags
        string pattern = @"<[^>]*>";

        // remove HTML tags using regular expressions
        return Regex.Replace(input, pattern, string.Empty);
    }
}
