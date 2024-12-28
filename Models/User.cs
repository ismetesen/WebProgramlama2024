using System.ComponentModel.DataAnnotations;

namespace BarberApplication.Models
{
    public class User
    {
        public int UserID { get; set; } // Primary Key

        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        [Display(Name = "Ad Soyad")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        [StringLength(100)]
        public string Email { get; set; } // Unique constraint

        [Required]
        public string PasswordHash { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public string? ProfilePhotoPath { get; set; }

        [Display(Name = "Telefon")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Kayıt Tarihi")]
        public DateTime RegistrationDate { get; set; }

        [Required]
        public string Role { get; set; } = "User"; // Default value: "User"

        // Navigation property
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}