namespace BarberApplication.Models
{
    public class EmployeePerformance
    {
        public int PerformanceID { get; set; } // Primary Key
        public int EmployeeID { get; set; } // Foreign Key
        public DateTime Date { get; set; } // Specific day
        public decimal TotalIncome { get; set; } // Total earnings
        public int TotalAppointments { get; set; } // Number of appointments
        public decimal EfficiencyScore { get; set; } // Example: TotalIncome / TotalAppointments

        public Employee Employee { get; set; } // Navigation property
    }
}
