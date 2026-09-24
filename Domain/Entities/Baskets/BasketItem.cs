using Domain.Entities.Common;
using Domain.Entities.Products;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Baskets
{
    [Table("basket_items")]

    public class BasketItem : BaseEntity
    {

        public long BasketId { get; set; }
        public Basket Basket { get; set; }
        public long ProductId { get; set; }
        public Product Product { get; set; }
        public int Count { get; set; }
        public bool IsChangePrice { get; set; } = false;
        public long Price { get; set; }
        public long Discount { get; set; }
        public int? DiscountType { get; set; }
        public bool IsGrounded { get; set; } = false;
    }
}
