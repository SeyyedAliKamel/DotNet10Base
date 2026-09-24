using Domain.Entities.Common;
using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("product_reviews")]

    public class ProductReview : BaseEntity
    {
        public long UserId { get; set; }
        public User User { get; set; }

        public string? Email { get; set; }
        public string Description { get; set; }

        public int Rate { get; set; }


        public int ProductId { get; set; }
        public Product Product { get; set; }

    }
}
