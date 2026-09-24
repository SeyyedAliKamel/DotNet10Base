using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace AdminWebApi.Models.ViewModels.Branch
{
    public class EditBranchViewModel
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WorkHours { get; set; }
        public string? Holidays { get; set; }
        public decimal? Longtitude { get; set; }
        public decimal? Latitude { get; set; }
        public long? CityId { get; set; }
        public bool IsActive { get; set; }

        public List<IFormFile>? Files { get; set; }
    }
}