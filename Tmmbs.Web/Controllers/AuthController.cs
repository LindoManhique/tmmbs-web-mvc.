using Microsoft.AspNetCore.Mvc;

namespace Tmmbs.Web.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult SignIn() => View();
        public IActionResult SignUp() => View();
    }
}
