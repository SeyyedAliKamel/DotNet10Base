using Common;
using Domain.Entities.Common;
using Domain.Entities.Shippings;
using Domain.Entities.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Baskets
{
    [Table("baskets")]
    public class Basket : BaseEntity
    {
        public long? UserId { get; set; }
        public User? User { get; set; }
        public string? Token { get; set; }
        public long? UserAddressId { get; set; }
        public UserAddress? UserAddress { get; set; }
        public int? Weight { get; set; }

        public string? AddressData { get; set; }

        public DateTime? PaymentDate { get; set; } = null;
        public DateTime? SendTime { get; set; } = null;
        public ICollection<BasketPayment>? BasketPayments { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public BasketStatus? BasketStatus { get; set; }
        public PaymentType PaymentType { get; set; }
        public ICollection<BasketItem>? BasketItems { get; set; }
        public ICollection<BasketStatusLog>? BasketStatusLogs { get; set; }

        public long? ShippingId { get; set; }
        public Shipping? Shipping { get; set; }
        public long ShippingCost { get; set; }
        public string? ShippingDescription { get; set; }

        public bool SeenByAdmin { get; set; } = false;
    }
}
