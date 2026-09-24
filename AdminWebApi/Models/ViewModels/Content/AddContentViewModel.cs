using Domain.Entities.Contents;

namespace AdminWebApi.Models.ViewModels.Content
{
    public class AddContentViewModel
    {
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string SlugTitle { get; set; }
        public string? ReleaseDate { get; set; }
        public bool IsActive { get; set; }

        public long ContentCategoryId { get; set; }
        public string? ContentRelationIds { get; set; } = "[]";
        public IFormFile Files { get; set; }
        public string? ImageDescription { get; set; }
    }
}
