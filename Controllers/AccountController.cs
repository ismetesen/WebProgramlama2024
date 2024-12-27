using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NETCore.Encrypt.Extensions;
using BarberApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace BarberApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _databaseContext;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext databaseContext, IConfiguration configuration)
        {
            _databaseContext = databaseContext;
            _configuration = configuration;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? md5Salt = _configuration.GetValue<string>("AppSettings:MD5Salt");
                if (md5Salt == null)
                {
                    ModelState.AddModelError("", "Configuration error: MD5Salt is missing.");
                    return View(model);
                }
                string saltedPassword = model.Password + md5Salt;
                string hashedPassword = saltedPassword.MD5();

                User user = _databaseContext.Users.SingleOrDefault(x => x.Email.ToLower() == model.Email.ToLower() && x.PasswordHash == hashedPassword);

                if (user != null)
                {
                    // Session'a kullanıcı bilgilerini ekle
                    HttpContext.Session.SetString("SesUsr", user.Email);
                    HttpContext.Session.SetString("UserFullName", user.FullName);
                    HttpContext.Session.SetString("UserID", user.UserID.ToString());

                    // Claims oluştur
                    List<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role ?? "User")
                    };

                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email veya şifre hatalı");
                }
            }

            return View(model);
        }
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_databaseContext.Users.Any(x => x.Email.ToLower() == model.Email.ToLower()))
                {
                    ModelState.AddModelError(nameof(model.Email), "Bu email adresi zaten kayıtlı.");
                }
                else
                {
                    string? md5Salt = _configuration.GetValue<string>("AppSettings:MD5Salt");
                    if (md5Salt == null)
                    {
                        ModelState.AddModelError("", "Configuration error: MD5Salt is missing.");
                        return View(model);
                    }
                    string saltedPassword = model.Password + md5Salt;
                    string hashedPassword = saltedPassword.MD5();

                    User user = new User
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        PasswordHash = hashedPassword,
                        RegistrationDate = DateTime.Now,
                        Role = "User"
                    };

                    _databaseContext.Users.Add(user);
                    int affectedRowCount = _databaseContext.SaveChanges();

                    if (affectedRowCount == 0)
                    {
                        ModelState.AddModelError("", "Kullanıcı eklenemedi.");
                    }
                    else
                    {
                        return RedirectToAction(nameof(Login));
                    }
                }
            }

            return View(model);
        }

        [Authorize]
        public IActionResult Profile()
        {
            string? userEmail = HttpContext.Session.GetString("SesUsr");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            // Include ile ilişkili verileri de çekiyoruz
            var user = _databaseContext.Users
                .Include(u => u.Appointments)
                    .ThenInclude(a => a.Service)
                .Include(u => u.Appointments)
                    .ThenInclude(a => a.Employee)
                .FirstOrDefault(x => x.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult Appointments()
        {
            string? userEmail = HttpContext.Session.GetString("SesUsr");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            // Kullanıcının randevularını getir
            var appointments = _databaseContext.Appointments
                .Include(a => a.Service)
                .Include(a => a.Employee)
                .Where(a => a.User.Email == userEmail)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToList();

            return View(appointments);
        }
    }
}