using Domain.Entities.AdditionalInformations;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Users
{
    [Table("user_addresses")]

    public class UserAddress : BaseEntity
    {
        public int UserId { get; set; }
        public string Address { get; set; }
        public string Plaque { get; set; }
        public string RecipientFullName { get; set; }
        public long CityId { get; set; }
        public City? City { get; set; }
        public long StateId { get; set; }
        public State? State { get; set; }
        public string Mobile { get; set; }
        public string PostalCode  { get; set; }
    }
}
