using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Tmmbs.Web.Filters;
using Tmmbs.Web.Models;

namespace Tmmbs.Web.Controllers
{
    [RequireAdmin]
    public class AdminController : Controller
    {
        private readonly FirestoreDb _db;
        public AdminController(FirestoreDb db) => _db = db;

        public async Task<IActionResult> Dashboard()
        {
            var contactsSnap = await _db.Collection("contact_messages")
                .OrderByDescending("createdAt").GetSnapshotAsync();

            var consultationsSnap = await _db.Collection("consultations")
                .OrderByDescending("createdAt").GetSnapshotAsync();

            var bookingsSnap = await _db.Collection("bookings")
                .OrderByDescending("createdAt").GetSnapshotAsync();

            string FirstNonEmpty(params string?[] values)
                => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "(Unknown service)";

            DateTime? ToDate(object? v)
            {
                if (v is null) return null;
                if (v is Timestamp ts) return ts.ToDateTime();
                if (v is DateTime dt) return dt;
                return null;
            }

            string? GetString(IDictionary<string, object> data, string key)
            {
                if (data.TryGetValue(key, out var v) && v is string s) return s;
                return null;
            }

            var vm = new AdminDashboardVM
            {
                Contacts = contactsSnap.Documents
                    .Select(d => new DocVM(d.Id, d.ToDictionary()))
                    .ToList(),

                Services = consultationsSnap.Documents
                    .Select(d => new DocVM(d.Id, d.ToDictionary()))
                    .ToList(),

                Bookings = bookingsSnap.Documents.Select(d =>
                {
                    var data = d.ToDictionary();

                    // Read service name from any of the three keys
                    var svcName = FirstNonEmpty(
                        GetString(data, "serviceName"),
                        GetString(data, "serviceTitle"),
                        GetString(data, "service")
                    );

                    return new BookingVM(
                        Id: d.Id,
                        UserId: GetString(data, "uid"),
                        Name: GetString(data, "name"),
                        Email: GetString(data, "email"),
                        Phone: GetString(data, "phone"),
                        ServiceName: svcName,                          // ✅ fixed
                        StartAt: ToDate(data.TryGetValue("startAt", out var startAt) ? startAt : null),
                        CreatedAt: ToDate(data.TryGetValue("createdAt", out var createdAt) ? createdAt : null),
                        Status: GetString(data, "status"),
                        Notes: GetString(data, "notes")
                    );
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(string id, string status)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            await _db.Collection("bookings").Document(id)
                .UpdateAsync(new Dictionary<string, object> { ["status"] = status });
            TempData["Msg"] = "Booking updated.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            await _db.Collection("bookings").Document(id).DeleteAsync();
            TempData["Msg"] = "Booking deleted.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            await _db.Collection("contact_messages").Document(id).DeleteAsync();
            TempData["Msg"] = "Message deleted.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
