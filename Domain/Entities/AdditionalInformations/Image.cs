using Common;
using Common.Dto;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.AdditionalInformations
{
    [Table("images")]
    public class Image : BaseEntity
    {
        
        public string ImageUrl { get; set; }
        public string? ImageDescription { get; set; }
        public long? ReferenceId { get; set; }
        public ImageTypeEnum ImageType { get; set; }
        public ImageRoleEnum ImageRole { get; set; }
    }
}
