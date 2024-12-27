using System.ComponentModel.DataAnnotations;

namespace BarberApplication.Models
{
    public class AppointmentViewModel
    {
        [Required(ErrorMessage = "Lütfen tarih ve saat seçin")]
        [Display(Name = "Randevu Tarihi ve Saati")]
        public DateTime AppointmentDateTime { get; set; }

        [Required(ErrorMessage = "Lütfen bir hizmet seçin")]
        [Display(Name = "Hizmet")]
        public int ServiceID { get; set; }

        [Required(ErrorMessage = "Lütfen bir berber seçin")]
        [Display(Name = "Berber")]
        public int EmployeeID { get; set; }
    }
}