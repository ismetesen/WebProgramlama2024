using Microsoft.AspNetCore.Mvc;

namespace BarberApplication.Controllers
{
    public class UserController : Controller 
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
