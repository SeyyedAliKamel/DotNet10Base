using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Contents
{
    [Table("contents")]

    public class Content : BaseEntity
    {
        public string? Name { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public long ContentCategoryId { get; set; }
        public ContentCategory ContentCategory { get; set; }
        public DateTime ReleaseDate { get; set; }

        public long? Order { get; set; }
    }
}
