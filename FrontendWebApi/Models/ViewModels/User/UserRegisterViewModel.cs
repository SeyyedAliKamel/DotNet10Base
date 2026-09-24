namespace FrontendWebApi.Models.ViewModels.User
{
    public class UserRegisterViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string? ReferalCode { get; set; }
        public bool NewsLetter { get; set; } = false;
    }
}
