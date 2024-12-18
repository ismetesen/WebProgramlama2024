namespace BarberApplication.Models
{
    public class User
    {
        public int UserID { get; set; } // Primary Key
        public string FullName { get; set; }
        public string Email { get; set; } // Unique constraint
        public string PasswordHash { get; set; }
        public string? PhoneNumber { get; set; } // Optional
        public DateTime RegistrationDate { get; set; }
        public string Role { get; set; } // Example: "User", "Admin"
    }
}
