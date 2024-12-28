namespace BarberApplication.ViewModels
{
    public class PerformanceFilterViewModel
    {
        public int? EmployeeID { get; set; }
        public string DateRange { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}