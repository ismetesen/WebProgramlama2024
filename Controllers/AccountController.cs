using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using NETCore.Encrypt.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using User = BarberApplication.Models.User;
using BarberApplication.Controllers;
using BarberApplication.Models;


namespace WebOdev.Controllers
{
    [Authorize]
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
                string md5Salt = _configuration.GetValue<string>("Appsettings:MD5Salt");
                string saltedPassword = model.Password + md5Salt;
                string hashedPassword = saltedPassword.MD5();

                User user = _databaseContext.Users.SingleOrDefault(x => x.Email.ToLower() == model.Email.ToLower() && x.PasswordHash == hashedPassword);

                if (user != null) // If user is found
                {
                    // Assuming you have a 'Locked' property or similar logic to handle locked users
                    // if (user.Locked)
                    // {
                    //     ModelState.AddModelError(nameof(model.Username), "User is locked.");
                    //     return View(model);
                    // }

                    List<Claim> claims = new List<Claim>
                    {
                        new Claim("id", user.UserID.ToString()),
                        new Claim("FullName", user.FullName ?? string.Empty),
                        new Claim("Email", user.Email),
                        new Claim("Role", user.Role)
                    };

                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email or password is incorrect");
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
                // Check if email already exists
                if (_databaseContext.Users.Any(x => x.Email.ToLower() == model.Email.ToLower()))
                {
                    ModelState.AddModelError(nameof(model.Email), "Email is already exists.");
                }
                else
                {
                    string md5Salt = _configuration.GetValue<string>("Appsettings:MD5Salt");
                    string saltedPassword = model.Password + md5Salt;
                    string hashedPassword = saltedPassword.MD5();

                    User user = new User
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        PasswordHash = hashedPassword,
                        RegistrationDate = DateTime.Now,
                        Role = "User" // Default role
                    };

                    _databaseContext.Users.Add(user);
                    int affectedRowCount = _databaseContext.SaveChanges();

                    if (affectedRowCount == 0)
                    {
                        ModelState.AddModelError("", "User cannot be added.");
                    }
                    else
                    {
                        return RedirectToAction(nameof(Login));
                    }
                }
            }

            return View(model);
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}