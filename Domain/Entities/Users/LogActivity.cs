using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Users
{
    [Table("log_activities")]

    public class LogActivity : BaseEntity
    {
        public string Guid { get; set; }
        public long? UserId { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string Page { get; set; }
        public string Property { get; set; }
        public string Browser { get; set; }
        public string Ip { get; set; }
        public string Platform { get; set; }
    }
}
