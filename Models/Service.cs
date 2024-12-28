using System.ComponentModel.DataAnnotations;

namespace BarberApplication.Models
{
    public class Service
    {
        public int ServiceID { get; set; }

        [Required(ErrorMessage = "Hizmet adı gereklidir")]
        [Display(Name = "Hizmet Adı")]
        [StringLength(100, ErrorMessage = "Hizmet adı en fazla 100 karakter olabilir")]
        public string ServiceName { get; set; }

        

        [Required(ErrorMessage = "Süre gereklidir")]
        [Display(Name = "Süre (dakika)")]
        [Range(1, 480, ErrorMessage = "Süre 1 ile 480 dakika arasında olmalıdır")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Fiyat gereklidir")]
        [Display(Name = "Fiyat")]
        [Range(0.01, 10000, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        // Navigation property
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}