using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("settings")]
    public class Setting : BaseEntity
    {
        public string Title { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
