using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Products
{
    [Table("hashtags")]
    public class Hashtag : BaseEntity
    {
        public string Title { get; set; }
        public bool ShowMenu { get; set; } = false;
    }
}
