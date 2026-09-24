using Common;
using Domain.Entities.Common;
using Domain.Entities.Products;
using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("reviews")]

    public class Review : BaseEntity
    {
        public long? UserId { get; set; }
        public User? User { get; set; }
        public string FullName { get; set; }
        public string? Mobile { get; set; }
        public string Description { get; set; }

        public int Rate { get; set; }


        public long ReferenceId { get; set; }
        public long? BasketId { get; set; }
        public ReviewTypeEnum Type { get; set; }
    }
}
