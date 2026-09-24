using Common;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.Json.Nodes;

namespace Domain.Entities.Products
{
    [Table("products")]
    public class Product : BaseEntity
    {
        public long? ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? LongDescription { get; set; }

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public int Price { get; set; }
        public int? Weight { get; set; } = 0;

        public int Quantity { get; set; } = 0;
        public int? MinBuyLimit { get; set; }
        public int? MaxBuyLimit { get; set; }

        public float Rating { get; set; } = 0;

        public ICollection<ProductProductCategory>? ProductCategories { get; set; }
        public ICollection<ProductHashtag>? Hashtags { get; set; }
        public Brand? Brand { get; set; }
        public long? BrandId { get; set; }

        public long? Order { get; set; }
        
    }
}
