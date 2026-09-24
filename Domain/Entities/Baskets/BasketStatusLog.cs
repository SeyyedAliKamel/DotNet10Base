using Common;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Baskets
{
    [Table("basket_status_logs")]
    public class BasketStatusLog : BaseEntity
    {
        public long BasketId { get; set; }
        public Basket? Basket { get; set; }
        public BasketStatus BasketStatus { get; set; }
        // long CreatedById : BaseEntity
        // DateTime CreatedAt : BaseEntity
    }
}
