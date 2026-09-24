using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("product_product_categories")]

    public class ProductProductCategory : BaseEntity
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }

        public long CategoryId { get; set; }
        public ProductCategory Category { get; set; }
    }
}
