using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarberApplication.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using BarberApplication.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BarberApplication.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (User.Identity.IsAuthenticated)
            {
                // Session'ı temizle
                HttpContext.Session.Clear();
                // Oturumu kapat
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Admin");
            }
            if (ModelState.IsValid)
            {
                // Önce mevcut oturumu kontrol et ve kapat
                if (User.Identity.IsAuthenticated)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }

                var adminUsername = _configuration["AdminSettings:Username"];
                var adminPassword = _configuration["AdminSettings:Password"];

                if (model.Username == adminUsername && model.Password == adminPassword)
                {
                    // Admin bilgileri doğruysa yeni oturum aç
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("IsAdmin", "true") // Ek güvenlik için
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24),
                        // Ek güvenlik seçenekleri
                        AllowRefresh = true,
                        IssuedUtc = DateTimeOffset.UtcNow
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Session'a admin bilgisini ekle
                    HttpContext.Session.SetString("IsAdmin", "true");
                    HttpContext.Session.SetString("AdminUser", model.Username);

                    TempData["SuccessMessage"] = "Admin girişi başarılı!";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Geçersiz admin kullanıcı adı veya şifre");
                TempData["ErrorMessage"] = "Geçersiz admin girişi denemesi!";
            }

            return View(model);
        }





        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu başarıyla silindi.";
            }
            return RedirectToAction("Appointments");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Employees()
        {
            var employees = await _context.Employees
                .Include(e => e.Appointments)
                .ToListAsync();
            return View(employees);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddEmployee()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddEmployee(Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Çalışan başarıyla eklendi.";
                return RedirectToAction("Employees");
            }
            return View(employee);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditEmployee(Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Çalışan başarıyla güncellendi.";
                return RedirectToAction("Employees");
            }
            return View(employee);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Çalışan başarıyla silindi.";
            }
            return RedirectToAction("Employees");
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var pendingAppointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .Include(a => a.Employee)
                .Where(a => a.Status == AppointmentStatus.Pending)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            ViewBag.PendingCount = pendingAppointments.Count;
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalServices = await _context.Services.CountAsync();

            return View(pendingAppointments);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .Include(a => a.Employee)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Approved;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu başarıyla onaylandı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Rejected;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu reddedildi.";
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Appointments)
                .OrderByDescending(u => u.RegistrationDate)
                .ToListAsync();

            return View(users);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Employee()
        {
            var employees = await _context.Employees
                .Include(e => e.Appointments)
                .ToListAsync();
            return View(employees);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddService()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla eklendi.";
                return RedirectToAction("Services");
            }
            return View(service);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Update(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla güncellendi.";
                return RedirectToAction("Services");
            }
            return View(service);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla silindi.";
            }
            return RedirectToAction("Services");
        }

        public async Task<IActionResult> EmployeePerformance()
        {
            var performances = await _context.EmployeePerformances
                .Include(p => p.Employee)
                .OrderByDescending(p => p.Date)
                .Select(p => new EmployeePerformanceViewModel
                {
                    PerformanceID = p.PerformanceID,
                    EmployeeID = p.EmployeeID,
                    EmployeeName = p.Employee.Name,
                    Date = p.Date,
                    TotalIncome = p.TotalIncome,
                    TotalAppointments = p.TotalAppointments,
                    EfficiencyScore = p.EfficiencyScore
                })
                .ToListAsync();

            // Çalışanları ViewBag'e ekle
            ViewBag.Employees = await _context.Employees
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeID.ToString(),
                    Text = e.Name
                })
                .ToListAsync();

            // Özet istatistikler
            ViewBag.TotalAppointments = performances.Sum(p => p.TotalAppointments);
            ViewBag.TotalIncome = performances.Sum(p => p.TotalIncome);
            ViewBag.AverageEfficiency = performances.Any() ? performances.Average(p => p.EfficiencyScore) : 0;
            ViewBag.BestEmployee = performances.Any() ?
                performances.OrderByDescending(p => p.EfficiencyScore).FirstOrDefault()?.EmployeeName : "Henüz veri yok";

            return View(performances);
        }

        // Performans verisi ekleme metodu
        [HttpPost]
        public async Task<IActionResult> AddPerformance(int employeeId)
        {
            try
            {
                var today = DateTime.Today;

                // Bugün için zaten performans kaydı var mı kontrol et
                var existingPerformance = await _context.EmployeePerformances
                    .FirstOrDefaultAsync(p => p.EmployeeID == employeeId && p.Date.Date == today);

                if (existingPerformance != null)
                {
                    TempData["Error"] = "Bu çalışan için bugünün performans kaydı zaten mevcut.";
                    return RedirectToAction(nameof(EmployeePerformance));
                }

                // Çalışanın varlığını kontrol et
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null)
                {
                    TempData["Error"] = "Çalışan bulunamadı.";
                    return RedirectToAction(nameof(EmployeePerformance));
                }

                // Bugünkü tamamlanmış randevuları al
                var appointments = await _context.Appointments
                    .Where(a => a.EmployeeID == employeeId &&
                               a.AppointmentDateTime.Date == today &&
                               a.Status == AppointmentStatus.Completed)
                    .ToListAsync();

                var totalIncome = appointments.Sum(a => a.TotalPrice);
                var totalAppointments = appointments.Count;

                // Verimlilik puanı hesapla
                var efficiencyScore = totalAppointments > 0
                    ? (decimal)(totalIncome / totalAppointments) * 100 / 1000
                    : 0;

                var performance = new EmployeePerformance
                {
                    EmployeeID = employeeId,
                    Date = today,
                    TotalAppointments = totalAppointments,
                    TotalIncome = totalIncome,
                    EfficiencyScore = efficiencyScore
                };

                _context.EmployeePerformances.Add(performance);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Performans kaydı başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Performans kaydı eklenirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(EmployeePerformance));
        }

        [HttpGet]
        public async Task<IActionResult> FilterPerformance(int? employeeId, string dateRange, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.EmployeePerformances.Include(p => p.Employee).AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(p => p.EmployeeID == employeeId);
            }

            switch (dateRange)
            {
                case "week":
                    startDate = DateTime.Today.AddDays(-7);
                    endDate = DateTime.Today;
                    break;
                case "month":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    endDate = DateTime.Today;
                    break;
                case "3months":
                    startDate = DateTime.Today.AddMonths(-3);
                    endDate = DateTime.Today;
                    break;
                case "year":
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    endDate = DateTime.Today;
                    break;
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(p => p.Date >= startDate && p.Date <= endDate);
            }

            var performances = await query
                .Select(p => new EmployeePerformanceViewModel
                {
                    EmployeeID = p.EmployeeID,
                    EmployeeName = p.Employee.Name,
                    Date = p.Date,
                    TotalAppointments = p.TotalAppointments,
                    TotalIncome = p.TotalIncome,
                    EfficiencyScore = p.EfficiencyScore
                })
                .ToListAsync();

            return Json(performances);
        }

        [HttpGet]
        public async Task<IActionResult> GetPerformanceDetails(int employeeId, DateTime date)
        {
            var performance = await _context.EmployeePerformances
                .Include(p => p.Employee)
                .Include(p => p.Employee.Appointments)
                    .ThenInclude(a => a.Service)
                .Where(p => p.EmployeeID == employeeId && p.Date.Date == date.Date)
                .FirstOrDefaultAsync();

            if (performance == null)
                return NotFound();

            var details = new
            {
                performance = new EmployeePerformanceViewModel
                {
                    EmployeeID = performance.EmployeeID,
                    EmployeeName = performance.Employee.Name,
                    Date = performance.Date,
                    TotalAppointments = performance.TotalAppointments,
                    TotalIncome = performance.TotalIncome,
                    EfficiencyScore = performance.EfficiencyScore
                },
                appointments = performance.Employee.Appointments
                    .Where(a => a.AppointmentDateTime.Date == date.Date)
                    .Select(a => new
                    {
                        time = a.AppointmentDateTime.ToString("HH:mm"),
                        service = a.Service.ServiceName,
                        price = a.TotalPrice
                    })
            };

            return Json(details);
        }

        

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}