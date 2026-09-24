using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Users
{
    [Table("permissions")]
    public class Permission : BaseEntity
    {
        public string Name { get; set; }
        [DataType("NVARCHAR"), Required, MaxLength(150)]
        public string Controller { set; get; } = null!;
        [DataType("NVARCHAR"), Required, MaxLength(150)]
        public string Action { set; get; } = null!;
        public string FrontEndName { get; set; }
        public virtual ICollection<Role>? Roles { get; set; }
        public long PermissionCategoryId { get; set; }
        public PermissionCategory PermissionCategory { get; set; }
    }
}
