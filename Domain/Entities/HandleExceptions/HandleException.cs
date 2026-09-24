using Domain.Entities.Common;
using Domain.Entities.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.HandleException
{
    [Table("handle_exceptions")]

    public class HandleException : BaseEntity
    {
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
        public string? RequestParameter { get; set; }
        public long? UserId { get; set; }
        public User? User { get; set; }
    }
}
