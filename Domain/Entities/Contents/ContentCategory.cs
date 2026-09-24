using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Contents
{
    [Table("content_categories")]

    public class ContentCategory : BaseEntity
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public long? Order { get; set; }
    }
}
