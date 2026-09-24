namespace AdminWebApi.Models.ViewModels.Additional
{
    public class EditImageViewModel
    {
        public long Id { get; set; }
        public IFormFile? Image { get; set; }
        public string? Alt { get; set; }
    }
}
