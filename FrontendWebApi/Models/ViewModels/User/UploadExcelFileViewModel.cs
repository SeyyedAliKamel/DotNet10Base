namespace AdminWebApi.Models.ViewModels.User
{
        public class UploadExcelFileViewModel
        {
            public List<IFormFile>? Files { get; set; }
            public long userCategoryId { get; set; }
        }
    }

