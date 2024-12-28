using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarberApplication.Models
{
    public class Employee
    {
        public int EmployeeID { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public string Availability { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<EmployeePerformance> Performances { get; set; } = new List<EmployeePerformance>();
    }
}