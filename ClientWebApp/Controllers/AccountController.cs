using Microsoft.AspNetCore.Mvc;

namespace ClientWebApp.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
