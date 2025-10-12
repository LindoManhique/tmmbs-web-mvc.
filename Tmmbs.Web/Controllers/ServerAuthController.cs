using System.Text.Json.Serialization;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;

namespace Tmmbs.Web.Controllers
{
    [Route("auth")]
    public class ServerAuthController : Controller
    {
        private static bool _initialized;
        private static readonly object _lock = new();

        private static void EnsureFirebase(IConfiguration? config = null)
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;

                // Prefer explicit file path; fall back to ADC
                var credPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                              ?? config?["Google:CredentialsPath"];

                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = !string.IsNullOrWhiteSpace(credPath)
                            ? GoogleCredential.FromFile(credPath!)
                            : GoogleCredential.GetApplicationDefault()
                    });
                }
                _initialized = true;
            }
        }

        public class TokenDto
        {
            // Accept both "idToken" and "IdToken"
            [JsonPropertyName("idToken")]
            public string? IdToken { get; set; }
        }

        public ServerAuthController(IConfiguration cfg)
        {
            EnsureFirebase(cfg);
        }

        [HttpPost("set-token")]
        public async Task<IActionResult> SetToken([FromBody] TokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.IdToken))
                return BadRequest(new { error = "Missing idToken" });

            try
            {
                // Verify on the server
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(dto!.IdToken!);

                // Set secure HttpOnly cookie so middleware can authenticate requests
                Response.Cookies.Append(
                    "fb_idtoken",
                    dto.IdToken!,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Path = "/",
                        MaxAge = TimeSpan.FromHours(8)
                    });

                return Ok(new { uid = decoded.Uid });
            }
            catch
            {
                // Clear any old cookie on failure
                Response.Cookies.Delete("fb_idtoken", new CookieOptions { Path = "/", Secure = true, SameSite = SameSiteMode.Strict });
                return Unauthorized();
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("fb_idtoken", new CookieOptions { Path = "/", Secure = true, SameSite = SameSiteMode.Strict });
            return Ok();
        }
    }
}
