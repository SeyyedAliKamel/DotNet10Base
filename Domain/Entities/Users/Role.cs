using Domain.Attributes;
using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;
//using System.ComponentModel.DataAnnotations;


namespace Domain.Entities.Users
{
    [Table("roles")]

    public class Role : BaseEntity
    {
        [DataType("NVARCHAR"), Required, MaxLength(150)]
        public string Name { set; get; } = null!;
        public virtual ICollection<User>? Users { get; set; }

        public virtual ICollection<Permission>? Permissions { get; set; }
        //public virtual ICollection<UserRole>? UserRoles { get; set; }

        //public RoleType Type { init; get; } = RoleType.Driver;
        //public virtual ICollection<User> Users { get; set; } = new List<User>();
        //public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    }
}