using Microsoft.AspNetCore.Mvc;
using Tmmbs.Web.Models;
using Tmmbs.Web.Services;

namespace Tmmbs.Web.Controllers
{
    public class BookingController : Controller
    {
        private readonly FirestoreService _fs;
        public BookingController(FirestoreService fs) => _fs = fs;

        // GET /Booking  (requires sign-in)
        [HttpGet("/Booking")]
        public async Task<IActionResult> Index([FromQuery] string? serviceId = null)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                var target = string.IsNullOrWhiteSpace(serviceId)
                    ? "/Booking"
                    : $"/Booking?serviceId={Uri.EscapeDataString(serviceId)}";

                return Redirect($"/Auth/SignIn?returnUrl={Uri.EscapeDataString(target)}");
            }

            ViewBag.Services = await _fs.GetActiveServicesAsync();
            ViewBag.SelectedServiceId = serviceId;
            return View(new Booking { StartAt = DateTime.Now.AddDays(1) });
        }

        // POST /Booking  (requires sign-in)
        [HttpPost("/Booking")]
        public async Task<IActionResult> Create([FromForm] Booking model)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Redirect($"/Auth/SignIn?returnUrl={Uri.EscapeDataString("/Booking")}");
            }

            // Read current user claims that our Firebase middleware set
            var uid = User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type.EndsWith("/emailaddress"))?.Value;

            var svc = await _fs.GetServiceByIdAsync(model.ServiceId);
            if (svc == null)
            {
                TempData["err"] = "Service not found.";
                return Redirect("/Booking");
            }

            model.Uid = uid ?? "";
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? (emailClaim ?? "") : model.Email;
            model.ServiceName = svc.Name;

            await _fs.CreateBookingAsync(model);
            TempData["ok"] = "Thanks! Your consultation request was submitted.";
            return Redirect("/Services");
        }
    }
}
