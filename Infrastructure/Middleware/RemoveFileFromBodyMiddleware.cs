using Microsoft.AspNetCore.Http;

public class RemoveFileFromBodyMiddleware
{
    private readonly RequestDelegate _next;

    public RemoveFileFromBodyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsFileRequest(context.Request))
        {
            string requestBody = await GetRequestBody(context.Request);
            requestBody = RemoveFilesFromRequestBody(requestBody);
            ReplaceRequestBody(context.Request, requestBody);
        }

        await _next(context);
    }

    private bool IsFileRequest(HttpRequest request)
    {
        return request.Method == "POST" && request.ContentType?.StartsWith("multipart/form-data") == false;
    }

    private async Task<string> GetRequestBody(HttpRequest request)
    {
        using (StreamReader reader = new StreamReader(request.Body))
        {
            return await reader.ReadToEndAsync();
        }
    }

    private string RemoveFilesFromRequestBody(string requestBody)
    {
        // Remove file contents from the request body here.
        // Example implementation: 
        return requestBody.Replace("fileContents", "");

        throw new NotImplementedException("RemoveFilesFromRequestBody method not implemented");
    }

    private void ReplaceRequestBody(HttpRequest request, string requestBody)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(requestBody);
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
    }
}
