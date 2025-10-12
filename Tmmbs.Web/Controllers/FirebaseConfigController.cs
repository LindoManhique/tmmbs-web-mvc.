using Microsoft.AspNetCore.Mvc;

namespace Tmmbs.Web.Controllers
{
    [Route("firebaseConfig.js")]
    public class FirebaseConfigController : Controller
    {
        private readonly IConfiguration _cfg;
        public FirebaseConfigController(IConfiguration cfg) => _cfg = cfg;

        [HttpGet]
        public ContentResult Get()
        {
            var s = _cfg.GetSection("FirebaseWeb");
            var js = $@"
window.firebaseConfig = {{
  apiKey: '{s["apiKey"]}',
  authDomain: '{s["authDomain"]}',
  projectId: '{s["projectId"]}',
  storageBucket: '{s["storageBucket"]}',
  messagingSenderId: '{s["messagingSenderId"]}',
  appId: '{s["appId"]}'
}};";
            return Content(js, "application/javascript");
        }
    }
}
