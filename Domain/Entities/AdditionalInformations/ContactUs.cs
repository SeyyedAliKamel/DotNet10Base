using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("contact_uses")]
    public class ContactUs : BaseEntity
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string Description { get; set; }
    }
}
