using Common;
using System.Text.Json.Nodes;

namespace AdminWebApi.Models.ViewModels.Product
{
    public class EditProductViewModel
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public int Price { get; set; }
        public int? WholePrice { get; set; }
        public int Quantity { get; set; }
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }
        public float Rating { get; set; }
        public bool? IsGrounded { get; set; }
        public int? Weight { get; set; }
        public string? SlugTitle { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public bool? IsActive { get; set; }
        public string? Attribute { get; set; }
        public long? BrandId { get; set; }
        public string? HashtagIds { get; set; }
        public string? RelatedProductIds { get; set; }
        public string? CategoryIds { get; set; }

        public List<IFormFile>? Files { get; set; } = new();
    }
}