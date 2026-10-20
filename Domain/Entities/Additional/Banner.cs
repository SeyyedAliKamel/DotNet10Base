using Common;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("banners")]
    public class Banner : BaseEntity
    {
        public string Title { get; set; }
        public string AdminTitle { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public BannerPositionEnum Position { get; set; } = BannerPositionEnum.Slider;
    }
}
