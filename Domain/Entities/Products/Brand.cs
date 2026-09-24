using Domain.Entities.Common;
using PaypalServerSdk.Standard.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("brands")]

    public class Brand : BaseEntity
    {
        public string Title { get; set; }
        public ICollection<Product>? Products { get; set; }

        public long? Order { get; set; }
    }
}
