using Domain.Entities.Common;
using Domain.Entities.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Contents
{
    [Table("content_relations")]

    public class ContentRelation : BaseEntity
    {
        public long ContentId { get; set; }
        public Content Content { get; set; }
        public long RelatedContentId { get; set; }
        public Content RelatedContent { get; set; }
    }
}
