using Common;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("menus")]
    public class Menu : BaseEntity
    {
        public string Title { get; set; }
        public string? Url { get; set; }
        public string Path { get; set; }
        public long? ParentId { get; set; }
        public Menu? Parent { get; set; }
        public MenuTypeEnum Type { get; set; } = MenuTypeEnum.Internal;
    }
}
