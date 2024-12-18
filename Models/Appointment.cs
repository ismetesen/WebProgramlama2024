namespace BarberApplication.Models
{
    public class Appointment
    {
        public int AppointmentID { get; set; } // Primary Key
        public DateTime AppointmentDateTime { get; set; }
        public int ServiceID { get; set; } // Foreign Key
        public int EmployeeID { get; set; } // Foreign Key
        public string CustomerName { get; set; }
        public decimal TotalPrice { get; set; }

        public Service Service { get; set; } // Navigation property
        public Employee Employee { get; set; } // Navigation property
    }
}
