using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("states")]
    public class State : BaseEntity
    {
        public string Name { get; set; }
        public ICollection<City>? Cities { get; set; }
    }
}
