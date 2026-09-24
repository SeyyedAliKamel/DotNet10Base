namespace AdminWebApi.Models.ViewModels.Product
{
    public class EditProductCategoryViewModel
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public int? ParentId { get; set; }
        public string? SlugTitle { get; set; }
        public bool IsActive { get; set; }

        public IFormFile? File { get; set; }
        public string? ImageDescription { get; set; }
    }
}
