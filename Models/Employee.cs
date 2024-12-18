namespace BarberApplication.Models
{
    public class Employee
    {
        public int EmployeeID { get; set; } // Primary Key
        public string Name { get; set; }
        public string Specialization { get; set; } // Example: "Boya, Kesim"
        public string Availability { get; set; } // Time range stored as a string (e.g., "09:00-17:00")

        public ICollection<Appointment> Appointments { get; set; } // One-to-Many relationship
        public ICollection<EmployeePerformance> Performances { get; set; } // One-to-Many relationship
    }
}
