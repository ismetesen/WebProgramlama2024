namespace BarberApplication.Models
{
    public class Appointment
    {
        public int AppointmentID { get; set; } // Primary Key
        public DateTime AppointmentDateTime { get; set; }
        public int ServiceID { get; set; } // Foreign Key
        public int EmployeeID { get; set; } // Foreign Key
        public required string CustomerName { get; set; }
        public decimal TotalPrice { get; set; }

        // Foreign Key for User
        public int UserID { get; set; } // Foreign Key
        public required User User { get; set; } // Navigation property

        public required Service Service { get; set; } // Navigation property
        public required Employee Employee { get; set; } // Navigation property
    }
}
