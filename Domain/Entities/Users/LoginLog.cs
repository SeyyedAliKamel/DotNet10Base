using Common;
using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Users
{
    [Table("login_logs")]

    public class LoginLog : BaseEntity
    {
        public long UserId { get; set; }
        public LoginLogStatus Status { get; set; }
    }
}