using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("product_categories")]

    public class ProductCategory : BaseEntity
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public long? ParentId { get; set; }
        public ProductCategory? Parent { get; set; }
        public ICollection<ProductCategory>? Children { get; set; }

        public long? Order { get; set; }
    }
}
