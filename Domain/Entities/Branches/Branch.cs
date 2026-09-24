using Domain.Entities.AdditionalInformations;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NetTopologySuite.Geometries;

namespace Domain.Entities.Branches
{
    [Table("branches")]

    public class Branch : BaseEntity
    {
        public string? Title { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WorkHours { get; set; }
        public string? Holidays { get; set; }
        public string? Image { get; set; }
        public decimal? Longtitude { get; set; }
        public decimal? Latitude { get; set; }
        public Point? Location { get; set; }
        public long? CityId { get; set; }
        public City? City { get; set; }
        public long? StateId { get; set; }
        public State? State { get; set; }
        public long? ManagerUserId { get; set; }
    }
}
