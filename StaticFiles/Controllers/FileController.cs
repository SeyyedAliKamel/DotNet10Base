using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace StaticFile.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private const string ValidApiKey = "bahram"; // TODO: move to Appsettings

        public FileController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // POST api/File/PostMultiImage?apiKey=...
        [HttpPost]
        public async Task<IActionResult> PostMultiImage([FromQuery] string apiKey)
        {
            if (apiKey != ValidApiKey)
                return Unauthorized("Invalid API key.");

            var files = Request.Form.Files;
            if (files.Count == 0)
                return BadRequest("No files were provided.");

            var folderName = Request.Form["Path"].ToString();
            if (string.IsNullOrWhiteSpace(folderName))
                return BadRequest("Path is required.");

            var fileNamesJson = Request.Form["FileName"].ToString();
            List<string>? fileNames;
            try
            {
                fileNames = JsonSerializer.Deserialize<List<string>>(fileNamesJson);
            }
            catch
            {
                return BadRequest("Invalid JSON format for FileName.");
            }

            if (fileNames is null || fileNames.Count != files.Count)
                return BadRequest("File count and file names count do not match.");

            var savePath = BuildSavePath(folderName);

            try
            {
                await SaveFilesAsync(files, savePath, fileNames);
                return Ok("Files uploaded successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/File/PostImage?apiKey=...
        [HttpPost]
        public async Task<IActionResult> PostImage([FromQuery] string apiKey)
        {
            if (apiKey != ValidApiKey)
                return Unauthorized("Invalid API key.");

            var files = Request.Form.Files;
            if (files.Count == 0)
                return BadRequest("No file uploaded.");

            var folderName = Request.Form["Path"].ToString();
            if (string.IsNullOrWhiteSpace(folderName))
                return BadRequest("Path is required.");

            var rawFileName = Request.Form["FileName"].ToString();
            var safeFileName = Path.GetFileName(rawFileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                return BadRequest("Invalid file name.");

            var savePath = BuildSavePath(folderName);
            Directory.CreateDirectory(savePath);

            try
            {
                var file = files[0];
                var filePath = Path.Combine(savePath, safeFileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var publicPath = Path.Combine("Resources", folderName, safeFileName)
                    .Replace("\\", "/");

                return Ok(publicPath);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE api/File/DeleteImage?apiKey=...
        [HttpDelete]
        public IActionResult DeleteImage([FromQuery] string apiKey, [FromQuery] string path)
        {
            if (apiKey != ValidApiKey)
                return Unauthorized("Invalid API key.");

            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Path is required.");

            try
            {
                var webRoot = _environment.WebRootPath;
                var fullPath = Path.GetFullPath(Path.Combine(webRoot, path.TrimStart('/')));
                var allowed = Path.GetFullPath(Path.Combine(webRoot, "Resources"));

                if (!fullPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                    return Unauthorized("Invalid path.");

                if (!System.IO.File.Exists(fullPath))
                    return Ok("File already absent."); // idempotent, not an error

                System.IO.File.Delete(fullPath);
                return Ok("File deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Builds and returns the absolute path to save files in, rooted at wwwroot/Resources.
        private string BuildSavePath(string folderName)
        {
            // Path.GetFullPath + EnsurePathIsUnder prevents directory traversal
            var webRoot = _environment.WebRootPath;
            var target = Path.GetFullPath(Path.Combine(webRoot, "Resources", folderName));
            var allowed = Path.GetFullPath(Path.Combine(webRoot, "Resources"));

            if (!target.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Invalid upload path.");

            return target;
        }

        /// <summary>
        /// Saves each uploaded file to <paramref name="savePath"/> using the corresponding name
        /// from <paramref name="fileNames"/>.
        /// </summary>
        private static async Task SaveFilesAsync(
            IFormFileCollection files,
            string savePath,
            List<string> fileNames)
        {
            Directory.CreateDirectory(savePath);

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (file.Length == 0) continue;

                var safeFileName = Path.GetFileName(fileNames[i]);
                var filePath = Path.Combine(savePath, safeFileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
        }
    }
}