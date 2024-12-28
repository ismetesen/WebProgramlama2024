using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberApplication.Models
{
    public class EmployeePerformance
    {
        [Key]
        public int PerformanceID { get; set; }

        [ForeignKey("Employee")]
        public int EmployeeID { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalIncome { get; set; }

        [Required]
        public int TotalAppointments { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal EfficiencyScore { get; set; }

        // Navigation property
        public virtual Employee Employee { get; set; }
    }
}