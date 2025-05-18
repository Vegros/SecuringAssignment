using Microsoft.AspNetCore.Mvc;

namespace ClientWebApp.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        [Route("Home/Error")]
        public IActionResult Error()
        {
            return View();
        }

        [Route("Home/StatusCode")]
        public IActionResult StatusCodeHandler(int code)
        {
            ViewData["code"] = code;
            return View("Error");

        }
    }
}
