using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("cities")]
    public class City : BaseEntity
    {
        public string Name { get; set; }
        public long StateId { get; set; }
        public State? State { get; set; }
    }
}
