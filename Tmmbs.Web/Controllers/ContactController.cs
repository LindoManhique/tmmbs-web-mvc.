using Microsoft.AspNetCore.Mvc;
using Tmmbs.Web.Services;

namespace Tmmbs.Web.Controllers
{
    public class ContactController : Controller
    {
        private readonly FirestoreService _fs;
        public ContactController(FirestoreService fs) => _fs = fs;

        [HttpGet("/Contact")]
        public IActionResult Index() => View();

        public class ContactForm { public string Name { get; set; } = ""; public string Email { get; set; } = ""; public string Message { get; set; } = ""; }

        [HttpPost("/Contact")]
        public async Task<IActionResult> Submit([FromForm] ContactForm form)
        {
            var uid = User?.Identity?.IsAuthenticated == true
                ? User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value
                : null;

            await _fs.AddContactMessageAsync(uid, form.Name, form.Email, form.Message);
            TempData["ok"] = "Thanks! We received your message.";
            return Redirect("/Contact");
        }
    }
}
