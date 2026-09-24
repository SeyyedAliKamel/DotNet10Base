using Common;
using Domain.Entities.Common;
using PaypalServerSdk.Standard.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Shippings
{
    [Table("shippings")]
    public class Shipping : BaseEntity
    {
        public string Name { get; set; } 
        public string? Description { get; set; }
        public string? CityIds { get; set; }
        public int? MinPrice { get; set; }
        public string Conditions { get; set; }
    }
}
