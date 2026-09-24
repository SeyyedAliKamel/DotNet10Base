namespace AdminWebApi.Models.ViewModels.Additional
{
    public class AddImageViewModel
    {
        public IFormFile Image { get; set; }
        public string? Alt { get; set; }
    }
}
