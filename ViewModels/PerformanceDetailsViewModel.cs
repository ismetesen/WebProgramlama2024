namespace BarberApplication.ViewModels
{
    public class PerformanceDetailsViewModel
    {
        public EmployeePerformanceViewModel Performance { get; set; }
        public List<AppointmentDetailViewModel> Appointments { get; set; }
        public Dictionary<string, decimal> HourlyStats { get; set; }
        public Dictionary<string, int> ServiceStats { get; set; }
    }

    public class AppointmentDetailViewModel
    {
        public DateTime Time { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
    }
}