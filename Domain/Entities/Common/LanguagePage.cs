using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Common
{
    [Table("language_pages")]

    public class LanguagePage : BaseEntity
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Locate { get; set; }
        public string Direction { get; set; }

    }
}
