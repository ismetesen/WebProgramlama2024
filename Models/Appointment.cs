using BarberApplication.Models;

public class Appointment
{
    public int AppointmentID { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public int ServiceID { get; set; }
    public int EmployeeID { get; set; }
    public string CustomerName { get; set; }
    public decimal TotalPrice { get; set; }
    public int UserID { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual User User { get; set; }
    public virtual Service Service { get; set; }
    public virtual Employee Employee { get; set; }
}

public enum AppointmentStatus
{
    Pending,    // Beklemede
    Approved,   // Onaylandı
    Rejected,   // Reddedildi
    Completed,  // Tamamlandı
    Cancelled   // İptal Edildi
}