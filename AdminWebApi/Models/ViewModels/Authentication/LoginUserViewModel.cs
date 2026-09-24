namespace EndPoint.WebService.Models.ViewModels.Authentication
{
    public class LoginUserViewModel
    {
        public string Mobile { get; set; }
        public string Password { get; set; }
        //public string Email { get; set; }
        // public RequestViewModel RequestViewModel { get; set; }
    }
    public class LoginInsteadUserViewModel
    {
        public long userId { get; set; }
    }
 
}
