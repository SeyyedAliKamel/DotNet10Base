using Common;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("product_offers")]

    public class ProductOffer : BaseEntity
    {
        public long ProductId { get; set; }
        //public Product? Product { get; set; }
        public string? OfferDescription { get; set; }
        public float OfferPercentage { get; set; }
        public OfferStatus OfferStatus { get; set; } = OfferStatus.Inactive;


        public DateTime? OfferStart { get; set; }
        public DateTime? OfferEnd { get; set; }
        public int? OfferLimit { get; set; }
    }
}
