using Microsoft.AspNetCore.Mvc;

namespace ClientWebApp.Controllers
{
    public class FileUploadController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
