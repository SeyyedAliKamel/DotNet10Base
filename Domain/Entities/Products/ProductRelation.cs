using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("product_relations")]

    public class ProductRelation : BaseEntity
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }
        public long RelatedProductId { get; set; }
        public Product RelatedProduct { get; set; }
    }
}
