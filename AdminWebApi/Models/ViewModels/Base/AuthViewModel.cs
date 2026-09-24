
using Domain.Attributes;

namespace EndPoint.WebService.Models.ViewModels.Base
{

    public class CurrentUser
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public string Token { get; set; }
        public DateTime LastActivity { get; set; }
        //public CurrentUserSession Session { set; get; } = null!;
        //public IEnumerable<RolePermissionClass> Roles { set; get; } = null!;
    }
    public class CurrentUserSession
    {
        public int Id { set; get; }
        public string Token { set; get; } = null!;
    }
    public class LoginViewModel
    {
        [Required, MaxLength(150), Email]
        public string Email { init; get; } = null!;
        [Required, MinLength(5), MaxLength(50)]
        public string Password { init; get; } = null!;
    }
    public class RegisterViewModel
    {
        [Required, MaxLength(150), Email]
        public string Email { set; get; } = null!;
        [Required, MinLength(5), MaxLength(50)]
        public string Password { set; get; } = null!;
        [Required, MaxLength(500)]
        public string FirstName { set; get; } = null!;
        [Required, MaxLength(500)]
        public string LastName { set; get; } = null!;
        [MaxLength(500)]
        public string DisplayName { set; get; } = null!;
        [MaxLength(500)]
        public string Code { set; get; } = null;
        public int CodeFail { set; get; } = 0;
    }
}