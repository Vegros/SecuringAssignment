using Microsoft.AspNetCore.Mvc;

namespace ClientWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
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
            if (code == 404)
            {
                return View("NotFound");
            }
            return View("Error");
        }
    }
}
