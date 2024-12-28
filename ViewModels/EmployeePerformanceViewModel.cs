using System;
using System.Collections.Generic;

namespace BarberApplication.ViewModels
{
    public class EmployeePerformanceViewModel
    {
        public int PerformanceID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalIncome { get; set; }
        public int TotalAppointments { get; set; }
        public decimal EfficiencyScore { get; set; }
    }
}