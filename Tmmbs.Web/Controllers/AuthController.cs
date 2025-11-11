using System;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tmmbs.Web.Controllers
{
    public class AuthController : Controller
    {
        // Keep this name identical to the middleware
        private const string CookieName = "fb_session";

        [HttpGet]
        public IActionResult SignIn(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult SignUp(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View();
        }

        /// <summary>
        /// Called by the client after Firebase JS sign-in.
        /// Verifies the Firebase ID token and issues a secure cookie the middleware reads later.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken] // <-- FIX: allow fetch without an antiforgery token
        public async Task<IActionResult> SessionLogin([FromForm] string idToken, [FromForm] string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                return BadRequest("Missing idToken.");

            try
            {
                // Verify so we don't store a bad token
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, checkRevoked: true);

                // If you want to require verified emails, uncomment:
                // if (!(decoded.Claims.TryGetValue("email_verified", out var ev) && (ev as bool? == true)))
                //     return Unauthorized("Email not verified.");

                Response.Cookies.Append(
                    CookieName,
                    idToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,                                  // Azure/HTTPS only
                        SameSite = SameSiteMode.None,                   // allow cross-site
                        IsEssential = true,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddHours(8)
                    });

                return Ok();
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet]
        [IgnoreAntiforgeryToken] // invoked via fetch; no form post
        public IActionResult Logout()
        {
            Response.Cookies.Delete(CookieName, new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });

            return Redirect("/");
        }
    }
}
