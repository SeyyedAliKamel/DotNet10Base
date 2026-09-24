using Domain.Entities.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Common
{
    public abstract class BaseEntity<TKey>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public bool IsActive { get; set; } = true;

        public long? DeletedById { get; set; }
        [NotMapped]
        public User? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public long? CreatedById { get; set; }
        [NotMapped]
        public  User? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public long? UpdatedById { get; set; }
        [NotMapped]
        public User? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; } = null;

        public bool IsDeleted { get; set; } = false;

    }
    public abstract class BaseEntity : BaseEntity<long>
    {

    }
}
