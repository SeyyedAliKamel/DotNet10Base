using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("product_hashtags")]

    public class ProductHashtag : BaseEntity
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }


        public long HashtagId { get; set; }
        public Hashtag Hashtag { get; set; }
    }
}
