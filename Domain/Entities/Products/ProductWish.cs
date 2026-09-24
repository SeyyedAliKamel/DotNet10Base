using Domain.Entities.Common;
using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("product_wishs")]

    public class ProductWish : BaseEntity
    {
        public long? ProductId { get; set; }
        public Product Product { get; set; }

        public long? UserId { get; set; }
        public User User { get; set; }
    }
}
