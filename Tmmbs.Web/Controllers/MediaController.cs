using Microsoft.AspNetCore.Mvc;
using Tmmbs.Web.Services;

namespace Tmmbs.Web.Controllers
{
    public class MediaController : Controller
    {
        private readonly FirestoreService _fs;
        public MediaController(FirestoreService fs) => _fs = fs;

        public async Task<IActionResult> Index()
        {
            var items = await _fs.GetMediaAsync(60);
            return View(items);
        }
    }
}
