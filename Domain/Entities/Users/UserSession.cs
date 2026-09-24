using Domain.Attributes;
using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;


namespace Domain.Entities.Users
{
    [Table("user_sessions")]

    public class UserSession : BaseEntity
    {
        public long UserId { set; get; }
        public User User { set; get; } = null!;
        [DataType("NVARCHAR"), MaxLength(50), Required]
        public string Platform { set; get; } = null!;
        [DataType("NVARCHAR"), MaxLength(50), Required]
        public string Browser { set; get; } = null!;
        [DataType("NVARCHAR"), MaxLength(20), Required, Ip]
        public string Ip { set; get; } = null!;
        //[DataType("NVARCHAR"), Index("TokenIndex", IsUnique = true), Length(20), Required]
        public string Token { set; get; } = null!;
        public DateTime LastActivity { set; get; } = DateTime.Now;

    }
}