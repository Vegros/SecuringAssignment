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
            return code switch
            {
                400 => View("BadRequest"),
                401 => View("Unauthorized"),
                403 => View("Forbidden"),
                404 => View("NotFound"),
                500 => View("ServerError"),
                _ => View("Error")
            };
        }
    }
}
