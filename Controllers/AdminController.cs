using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarberApplication.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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
            if (ModelState.IsValid)
            {
                var adminUsername = _configuration["AdminSettings:Username"];
                var adminPassword = _configuration["AdminSettings:Password"];

                if (model.Username == adminUsername && model.Password == adminPassword)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.Username),
                        new Claim(ClaimTypes.Role, "Admin")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Geçersiz kullanıcı adı veya şifre");
            }

            return View(model);
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
        public async Task<IActionResult> Employees()
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

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}