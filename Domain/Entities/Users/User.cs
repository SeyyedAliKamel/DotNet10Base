using Common;
using Domain.Attributes;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Common;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Domain.Entities.Users
{
    [Table("users")]
    [Auditable]
    public class User : BaseEntity
    {
        public Guid UserGuid { get; set; } = Guid.NewGuid();
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Password { get; set; }
        public bool RequiredChangePassword { get; set; } = false;
        public bool NewsLetter { get; set; } = false;

        public DateTime? Birthday { get; set; }
        public Gender? Gender { get; set; }
        public string? Job { get; set; }

        public string? Code { get; set; }
        public DateTime? CodeGenerateDate { get; set; }
        public DateTime? SuspendGenerateCode { get; set; }
        public DateTime? LastLogin { set; get; }
        public int CodeFail { set; get; } = 0;
        public string? UserRegisterToken { set; get; }
        public string? AuthToken { set; get; }

        public string? ReferalCode { get; set; }
        public long? ReferalUserId { get; set; }

        public UserTypeEnum Type { get; set; } = UserTypeEnum.Individual;
        public UserStatusEnum Status { get; set; } = UserStatusEnum.Inactive;

        public virtual ICollection<Role>? Roles { get; set; }
        public ICollection<UserSession>? Sessions { get; set; }
    }
}