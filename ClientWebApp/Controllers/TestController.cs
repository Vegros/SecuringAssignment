using Microsoft.AspNetCore.Mvc;

namespace ClientWebApp.Controllers
{
    public class TestController : Controller
    {
        private readonly List<(string Username, string Password)> accounts = new()
        {
            ("admin", "admin123"),
            ("user", "password"),
            ("test", "123456"),
            ("guest", "guest123"),
            ("john", "doe123")
        };

        [HttpGet("test/login")]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost("test/login")]
        public IActionResult Login(string username, string password)
        {
            if (accounts.Any(acc =>
                acc.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                && acc.Password == password))
            {
                return new OkResult();
            }
            else
            {
                Thread.Sleep(2000);
                return new UnauthorizedResult();
            }



        }
    }

}

