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
        public async Task<IActionResult> GetAvailableTimeSlots(int employeeId, int serviceId, DateTime date)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                var service = await _context.Services.FindAsync(serviceId);

                if (employee == null || service == null)
                {
                    return Json(new List<string>());
                }

                var workStartTime = new TimeSpan(9, 0, 0);
                var workEndTime = new TimeSpan(18, 0, 0);

                // Onaylanmış randevuları al
                var existingAppointments = await _context.Appointments
                    .Include(a => a.Service)
                    .Where(a => a.EmployeeID == employeeId &&
                               a.AppointmentDateTime.Date == date.Date &&
                               (a.Status == AppointmentStatus.Approved ||
                                a.Status == AppointmentStatus.Pending))
                    .ToListAsync();

                var availableSlots = new List<string>();
                var currentTime = workStartTime;

                while (currentTime.Add(TimeSpan.FromMinutes(service.Duration)) <= workEndTime)
                {
                    var slotStartTime = date.Date.Add(currentTime);
                    var slotEndTime = slotStartTime.AddMinutes(service.Duration);

                    if ((date.Date != DateTime.Today || currentTime > DateTime.Now.TimeOfDay) &&
                        !existingAppointments.Any(a =>
                            (a.AppointmentDateTime < slotEndTime &&
                             a.AppointmentDateTime.AddMinutes(a.Service.Duration) > slotStartTime)))
                    {
                        availableSlots.Add(currentTime.ToString(@"hh\:mm"));
                    }

                    currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                }

                return Json(availableSlots);
            }
            catch (Exception ex)
            {
                return Json(new List<string>());
            }
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

                // Randevu çakışması kontrolü
                var isSlotAvailable = await IsTimeSlotAvailable(model.EmployeeID,
                                                              model.AppointmentDateTime,
                                                              service.Duration);
                if (!isSlotAvailable)
                {
                    ModelState.AddModelError("", "Seçilen zaman dilimi artık müsait değil.");
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
                    Status = AppointmentStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Randevunuz oluşturuldu ve onay bekliyor.";
                return RedirectToAction("Appointments", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Randevu oluşturulurken bir hata oluştu.");
                ViewBag.Employees = _context.Employees.ToList();
                ViewBag.Services = _context.Services.ToList();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userEmail = HttpContext.Session.GetString("SesUsr");
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AppointmentID == id &&
                                        a.User.Email == userEmail);

            if (appointment == null)
            {
                return NotFound();
            }

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

        private async Task<bool> IsTimeSlotAvailable(int employeeId, DateTime startTime, int duration)
        {
            var endTime = startTime.AddMinutes(duration);

            return !await _context.Appointments
                .AnyAsync(a => a.EmployeeID == employeeId &&
                              a.Status != AppointmentStatus.Cancelled &&
                              a.Status != AppointmentStatus.Rejected &&
                              ((a.AppointmentDateTime <= startTime &&
                                a.AppointmentDateTime.AddMinutes(a.Service.Duration) > startTime) ||
                               (a.AppointmentDateTime < endTime &&
                                a.AppointmentDateTime.AddMinutes(a.Service.Duration) >= endTime) ||
                               (a.AppointmentDateTime >= startTime &&
                                a.AppointmentDateTime.AddMinutes(a.Service.Duration) <= endTime)));
        }
    }
}