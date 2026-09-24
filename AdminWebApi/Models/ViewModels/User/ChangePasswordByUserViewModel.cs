namespace AdminWebApi.Models.ViewModels.User
{
    public class ChangePasswordByUserViewModel
    {
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
        public string OldPassword { get; set; }
    }
}
