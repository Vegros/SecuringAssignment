using Microsoft.AspNetCore.Mvc;

namespace ClientWebApp.Controllers
{
    public class TestController : Controller
    {
        private readonly String username = "admin";
        private readonly String password = "admin123";

        [HttpGet("test/login")]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost("test/login")]
        public IActionResult Login(string username, string password)
        {
            if (username.Equals(this.username) && password.Equals(this.password))
            {
                return new OkResult();
            }
            else
            {
                Thread.Sleep(2000);
                return new ForbidResult();
            }



        }
    }

}

