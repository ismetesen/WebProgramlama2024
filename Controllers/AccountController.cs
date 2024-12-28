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
        public async Task<IActionResult> EditProfile()
        {
            var userEmail = HttpContext.Session.GetString("SesUsr");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            var user = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePhotoPath = user.ProfilePhotoPath
            };

            return View(model);
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            // Sadece zorunlu alanların validasyonunu kontrol et
            if (!ModelState.IsValid)
            {
                var passwordErrors = ModelState.Keys
                    .Where(k => k.Contains("Password"))
                    .SelectMany(k => ModelState[k].Errors)
                    .Count();

                // Eğer sadece şifre alanlarında hata varsa ve yeni şifre girilmemişse, bu hataları temizle
                if (string.IsNullOrEmpty(model.NewPassword) &&
                    string.IsNullOrEmpty(model.ConfirmNewPassword) &&
                    ModelState.ErrorCount == passwordErrors)
                {
                    ModelState.Clear();
                }
                else
                {
                    // Diğer alanlarda hata varsa formu tekrar göster
                    return View(model);
                }
            }

            var userEmail = HttpContext.Session.GetString("SesUsr");
            var user = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound();
            }

            // Mevcut şifreyi kontrol et
            string saltedPassword = model.CurrentPassword + _configuration.GetValue<string>("AppSettings:MD5Salt");
            string hashedPassword = saltedPassword.MD5();

            if (hashedPassword != user.PasswordHash)
            {
                ModelState.AddModelError("CurrentPassword", "Mevcut şifre yanlış");
                return View(model);
            }

            // Email değişikliği varsa kontrol et
            if (model.Email != user.Email)
            {
                if (await _databaseContext.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Bu email adresi başka bir kullanıcı tarafından kullanılıyor");
                    return View(model);
                }
            }

            // Bilgileri güncelle
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            // Yeni şifre varsa güncelle
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                string newSaltedPassword = model.NewPassword + _configuration.GetValue<string>("AppSettings:MD5Salt");
                user.PasswordHash = newSaltedPassword.MD5();
            }

            await _databaseContext.SaveChangesAsync();

            // Session'ı güncelle
            HttpContext.Session.SetString("SesUsr", user.Email);
            HttpContext.Session.SetString("UserFullName", user.FullName);

            TempData["Success"] = "Profil bilgileriniz başarıyla güncellendi.";
            return RedirectToAction("Profile");
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "Lütfen bir fotoğraf seçin.";
                return RedirectToAction("EditProfile");
            }

            var userEmail = HttpContext.Session.GetString("SesUsr");
            var user = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound();
            }

            // Eski fotoğrafı sil
            if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
            {
                var oldPhotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(oldPhotoPath))
                {
                    System.IO.File.Delete(oldPhotoPath);
                }
            }

            // Yeni fotoğrafı kaydet
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            user.ProfilePhotoPath = $"/uploads/profiles/{fileName}";
            await _databaseContext.SaveChangesAsync();

            TempData["Success"] = "Profil fotoğrafınız başarıyla güncellendi.";
            return RedirectToAction("EditProfile");
        }

        [Authorize]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            var userEmail = HttpContext.Session.GetString("SesUsr");
            var user = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
            {
                // Dosya yolunu düzelt
                var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    user.ProfilePhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                // Dosya varsa sil
                if (System.IO.File.Exists(photoPath))
                {
                    try
                    {
                        System.IO.File.Delete(photoPath);
                    }
                    catch (Exception ex)
                    {
                        // Hata logla ama devam et
                        Console.WriteLine($"Error deleting file: {ex.Message}");
                    }
                }

                // Veritabanındaki referansı temizle
                user.ProfilePhotoPath = null;
                await _databaseContext.SaveChangesAsync();

                TempData["Success"] = "Profil fotoğrafınız başarıyla silindi.";
            }

            return RedirectToAction("EditProfile");
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

        [AllowAnonymous]
        public async Task<IActionResult> AdminLogin()
        {
            // Eğer normal kullanıcı girişi yapılmışsa, çıkış yap
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Admin");
            }

            return RedirectToAction("Login", "Admin");
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