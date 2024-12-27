namespace BarberApplication.Models
{
    public class Service
    {
        public int ServiceID { get; set; } // Primary Key
        public string ServiceName { get; set; } // Example: "Kısa Saç Kesimi"
        public int Duration { get; set; } // Duration in minutes
        public decimal Price { get; set; }

        // Navigation property
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}