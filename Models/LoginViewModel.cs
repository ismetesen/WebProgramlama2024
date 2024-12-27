namespace BarberApplication.Models
{
    public class LoginViewModel
    {

        public string Email { get; set; }
        public string Password { get; set; }
        public object? Username { get; internal set; }
    }
}

