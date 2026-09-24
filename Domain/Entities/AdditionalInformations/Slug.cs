using Common;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("slugs")]
    public class Slug : BaseEntity
    {
        public string Title { get; set; }
        public SlugTypeEnum Type { get; set; }
        public long ReferenceId { get; set; }
    }
}
