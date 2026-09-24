
using Microsoft.AspNetCore.Mvc;


namespace AdminWebApi.Models.ViewModels.Public
{
    public class UploadImageViewModel
    {
       public IFormFile upload { get; set; }
       public string ckCsrfToken { get; set; }


    }
   
}
