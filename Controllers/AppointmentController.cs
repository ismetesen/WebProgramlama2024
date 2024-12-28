using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarberApplication.Models;

namespace BarberApplication.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            ViewBag.Employees = _context.Employees.ToList();
            ViewBag.Services = _context.Services.ToList();
            return View();
        }

        [HttpGet]
        private async Task<bool> IsTimeSlotAvailable(int employeeId, DateTime startTime, int duration)
        {
            var endTime = startTime.AddMinutes(duration);

            var conflictingAppointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.EmployeeID == employeeId &&
                           ((a.AppointmentDateTime <= startTime && a.AppointmentDateTime.AddMinutes(a.Service.Duration) > startTime) ||
                            (a.AppointmentDateTime < endTime && a.AppointmentDateTime.AddMinutes(a.Service.Duration) >= endTime) ||
                            (a.AppointmentDateTime >= startTime && a.AppointmentDateTime.AddMinutes(a.Service.Duration) <= endTime)))
                .AnyAsync();

            return !conflictingAppointments;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Employees = _context.Employees.ToList();
                    ViewBag.Services = _context.Services.ToList();
                    return View(model);
                }

                var userEmail = HttpContext.Session.GetString("SesUsr");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var service = await _context.Services.FindAsync(model.ServiceID);
                var employee = await _context.Employees.FindAsync(model.EmployeeID);

                if (service == null || employee == null)
                {
                    ModelState.AddModelError("", "Seçilen hizmet veya berber bulunamadı.");
                    ViewBag.Employees = _context.Employees.ToList();
                    ViewBag.Services = _context.Services.ToList();
                    return View(model);
                }

                var appointmentTime = model.AppointmentDateTime;
                var appointmentTimeOfDay = appointmentTime.TimeOfDay;
                var startHour = new TimeSpan(9, 0, 0); // 09:00
                var endHour = new TimeSpan(18, 0, 0);  // 18:00

                if (appointmentTimeOfDay < startHour || appointmentTimeOfDay >= endHour)
                {
                    ModelState.AddModelError("", "Lütfen 09:00 - 18:00 saatleri arasında bir randevu seçin.");
                    ViewBag.Employees = _context.Employees.ToList();
                    ViewBag.Services = _context.Services.ToList();
                    return View(model);
                }

                if (!await IsTimeSlotAvailable(model.EmployeeID, model.AppointmentDateTime, service.Duration))
                {
                    ModelState.AddModelError("", "Seçilen zaman diliminde berber müsait değil. Lütfen başka bir zaman seçin.");
                    ViewBag.Employees = _context.Employees.ToList();
                    ViewBag.Services = _context.Services.ToList();
                    return View(model);
                }

                var appointment = new Appointment
                {
                    AppointmentDateTime = model.AppointmentDateTime,
                    ServiceID = model.ServiceID,
                    EmployeeID = model.EmployeeID,
                    CustomerName = user.FullName ?? "İsimsiz Müşteri",
                    TotalPrice = service.Price,
                    UserID = user.UserID,
                    User = user,
                    Service = service,
                    Employee = employee
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Randevunuz başarıyla oluşturuldu.";
                return RedirectToAction("Appointments", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Randevu oluşturulurken bir hata oluştu: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Detay: {ex.InnerException.Message}");
                }

                ViewBag.Employees = _context.Employees.ToList();
                ViewBag.Services = _context.Services.ToList();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Kullanıcının kendi randevusunu iptal ettiğinden emin ol
            var userEmail = HttpContext.Session.GetString("SesUsr");
            if (string.IsNullOrEmpty(userEmail) || appointment.User.Email != userEmail)
            {
                return Forbid();
            }

            // Randevunun iptal edilebilir olup olmadığını kontrol et
            if (appointment.AppointmentDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Geçmiş randevular iptal edilemez.";
                return RedirectToAction("Appointments", "Account");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevunuz başarıyla iptal edildi.";
            return RedirectToAction("Appointments", "Account");
        }
        [HttpPost, ActionName("CancelAppointment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointmentConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // Kullanıcının kendi randevusunu iptal ettiğinden emin ol
                var userIdString = HttpContext.Session.GetString("UserID");
                if (!int.TryParse(userIdString, out int userId) || appointment.UserID != userId)
                {
                    return Forbid();
                }

                appointment.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevunuz başarıyla iptal edildi.";
            }

            return RedirectToAction("Appointments", "Account");
        }
    }
}