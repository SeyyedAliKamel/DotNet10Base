using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Users
{
    [Table("permission_categories")]

    public class PermissionCategory : BaseEntity
    {
        public string Title { get; set; }
        public virtual ICollection<Permission>? Permissions { get; set; } = new List<Permission>();
    }
}
