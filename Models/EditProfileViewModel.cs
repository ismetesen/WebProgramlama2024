using System.ComponentModel.DataAnnotations;

namespace BarberApplication.Models
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }

        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mevcut şifre gereklidir")]
        [Display(Name = "Mevcut Şifre")]
        public string? CurrentPassword { get; set; }

        [Display(Name = "Yeni Şifre (Opsiyonel)")]
        [StringLength(100, ErrorMessage = "Şifre en az {2} karakter uzunluğunda olmalıdır", MinimumLength = 6)]
        public string? NewPassword { get; set; }

        [Display(Name = "Yeni Şifre Tekrar")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
        public string? ConfirmNewPassword { get; set; }

        public string ProfilePhotoPath { get; set; }
    }
}