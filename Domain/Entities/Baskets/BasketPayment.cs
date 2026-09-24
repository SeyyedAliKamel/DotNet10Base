using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Baskets
{
    [Table("basket_payments")]

    public class BasketPayment : BaseEntity
    {

        public long BasketId { get; set; }
        public Basket Basket { get; set; }

        public string Status { get; set; }
        public string Authority { get; set; }
        public int Price { get; set; }
        public string VerificationStatus { get; set; }
        public string? Ref { get; set; }
        public long UserId { get; set; }
    }
}
