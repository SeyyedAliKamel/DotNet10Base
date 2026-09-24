using ClosedXML.Excel;
using Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;


namespace Infrastructure.ExternalApi.FileServer
{
    public interface IFileUploadService
    {
        Task<List<string>> UploadImages(List<IFormFile> files, TypeSaveFile typeSaveFile, List<string> fileName);
        Task<string> UploadImage(List<IFormFile> files, TypeSaveFile typeSaveFile, string fileName);
        Task<List<ResultUserExcelDto>> UploadUserExcelFile(List<IFormFile> files, TypeSaveFile typeSaveFile, string fileName);
        Task<bool> DeleteImage(string imagePath);
    }
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        public FileUploadService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<List<string>> UploadImages(List<IFormFile> files, TypeSaveFile typeSaveFile, List<string> fileName)
        {
            string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

            var options = new RestClientOptions(staticPath)
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
            };
            var client = new RestClient(options);
            var request = new RestRequest("/api/File/PostMultiImage?apikey=bahram", Method.Post);

            request.AddParameter("Path", GetPathFile(typeSaveFile));
            request.AddParameter("FileName", JsonConvert.SerializeObject(fileName));
            foreach (var item in files)
            {
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    await item.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                request.AddFile(item.FileName, bytes, item.FileName, item.ContentType);
            }

            RestResponse response = client.Execute(request);

            if (!response.IsSuccessful || response.Content is null)
            {
                throw new Exception(
                    $"Upload failed. Status: {response.StatusCode}, " +
                    $"Error: {response.ErrorMessage}, " +
                    $"Exception: {response.ErrorException?.Message}");
            }

            UploadDto upload = JsonConvert.DeserializeObject<UploadDto>(response.Content);
            return upload.FileNameAddress;
        }

        public async Task<string> UploadImage(List<IFormFile> files, TypeSaveFile typeSaveFile, string fileName)
        {
            string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];
            var file = files[0];

            var options = new RestClientOptions(staticPath)
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
            };
            var client = new RestClient(options);
            var request = new RestRequest("/api/File/PostImage?apikey=bahram", Method.Post);

            request.AddParameter("Path", GetPathFile(typeSaveFile));
            request.AddParameter("FileName", fileName);
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                ms.Position = 0;
                bytes = ms.ToArray();
            }
            request.AddFile(file.FileName, bytes, file.FileName, file.ContentType);

            RestResponse response = client.Execute(request);

            if (!response.IsSuccessful || response.Content is null)
            {
                throw new Exception(
                    $"Upload failed. Status: {response.StatusCode}, " +
                    $"Error: {response.ErrorMessage}, " +
                    $"Exception: {response.ErrorException?.Message}");
            }

            return response.Content;
        }

        public async Task<bool> DeleteImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return true; // nothing to delete

            string staticPath = _configuration.GetSection("AppSettings")["StaticPath"];

            var options = new RestClientOptions(staticPath)
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
            };
            var client = new RestClient(options);
            var request = new RestRequest("/api/File/DeleteImage?apikey=bahram", Method.Delete);
            request.AddParameter("path", imagePath);

            RestResponse response = await client.ExecuteAsync(request);

            return response.IsSuccessful;
        }

        public async Task<List<ResultUserExcelDto>> UploadUserExcelFile(List<IFormFile> files, TypeSaveFile typeSaveFile, string fileName)
        {
            var userList = new List<ResultUserExcelDto>();
            var formFile = files[0];
            using (var stream = new MemoryStream())
            {
                // Save the uploaded file to the memory stream
                await formFile.CopyToAsync(stream);
                stream.Position = 0;

                // Use ClosedXML to read the Excel file from the stream
                using (var workbook = new XLWorkbook(stream))
                {
                    IXLWorksheet worksheet = workbook.Worksheet(1); // Get the first worksheet



                    int rowCount = worksheet.RowsUsed().Count();
                    int columnCount = worksheet.ColumnsUsed().Count();

                    ExcelItem[,] excelData = new ExcelItem[rowCount, columnCount];

                    for (int row = 2; row <= rowCount; row++)
                    {
                        for (int column = 1; column <= columnCount; column++)
                        {
                            IXLCell cell = worksheet.Cell(row, column);

                            string cellValue = cell.IsEmpty() ? null : cell.Value.ToString();

                            excelData[row - 1, column - 1] = new ExcelItem
                            {
                                CellValue = cellValue
                            };
                        }
                        var item = new ResultUserExcelDto
                        {
                            FirstName = excelData[row - 1, 0].CellValue,
                            LastName = excelData[row - 1, 1].CellValue,
                            Mobile = excelData[row - 1, 2].CellValue,
                        };
                        userList.Add(item);
                    }



                }
            }


            return userList;
        }
        private async Task ProcessStream(Stream stream)
        {



            // Save the uploaded file to the memory stream


            // Use ClosedXML to read the Excel file from the stream
            using (var workbook = new XLWorkbook(stream))
            {
                IXLWorksheet worksheet = workbook.Worksheet(1); // Get the first worksheet

                foreach (IXLRow row in worksheet.RowsUsed())
                {
                    foreach (IXLCell cell in row.Cells())
                    {
                        string cellValue = cell.Value.ToString();
                        // Perform the desired operations with the cell value
                    }
                }
            }




        }
        private string GetPathFile(TypeSaveFile typeSaveFile)
        {
            switch (typeSaveFile)
            {
                case TypeSaveFile.Product:
                    return "Product";
                    break;
                case TypeSaveFile.ProductCategory:
                    return "ProductCategory";
                    break;
                case TypeSaveFile.BannerSlider:
                    return "BannerSlider";
                    break;
                case TypeSaveFile.Content:
                    return "Content";
                    break;
                case TypeSaveFile.FrequentQuestionCategory:
                    return "FrequentQuestion";
                    break;
                case TypeSaveFile.FrequentQuestion:
                    return "FrequentQuestion";
                case TypeSaveFile.ContentCategory:
                    return "ContentCategory";
                    break;
                
                case TypeSaveFile.Public:
                    return "Public";
                    break;
                case TypeSaveFile.Banner:
                    return "Banner";
                    break;
                case TypeSaveFile.ProductSlider:
                    return "ProductSlider";
                    break;
                case TypeSaveFile.Slider:
                    return "Slider";
                    break;
                case TypeSaveFile.PaymentType:
                    return "PaymentType";
                    break;
                case TypeSaveFile.Branch:
                    return "Branch";
                    break;
                case TypeSaveFile.Cheque:
                    return "Cheque";
                    break;

                default: return "Other";
            }
        }
    }
    public class UploadDto
    {
        public bool? Status { get; set; }
        public List<string> FileNameAddress { get; set; }
    }
    public class ResultUserExcelDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }

    }
    public class ExcelItem
    {
        public string CellValue { get; set; }
    }
}
