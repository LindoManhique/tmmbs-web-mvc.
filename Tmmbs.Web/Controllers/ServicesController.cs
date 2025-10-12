using Microsoft.AspNetCore.Mvc;
using Tmmbs.Web.Services;

namespace Tmmbs.Web.Controllers
{
    public class ServicesController : Controller
    {
        private readonly FirestoreService _fs;
        public ServicesController(FirestoreService fs) => _fs = fs;

        public async Task<IActionResult> Index()
        {
            var services = await _fs.GetActiveServicesAsync();
            return View(services);
        }

        [HttpGet("/Services/Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var svc = await _fs.GetServiceByIdAsync(id);
            if (svc == null) return NotFound();
            return View(svc);
        }
    }
}
