using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;

namespace Tmmbs.Web.Auth
{
    /// <summary>
    /// Verifies Firebase ID token from cookie "fb_session" and builds HttpContext.User.
    /// Adds Role=Admin for allowlisted emails (edit below).
    /// </summary>
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CookieName = "fb_session";

        private static readonly HashSet<string> AdminEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "user1@admin.com",
            "user@admin.com"
            // add more admin emails if needed
        };

        public FirebaseAuthMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            if (!context.Request.Cookies.TryGetValue(CookieName, out var idToken) ||
                string.IsNullOrWhiteSpace(idToken))
            {
                await _next(context);
                return;
            }

            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, checkRevoked: true);

                decoded.Claims.TryGetValue("user_id", out var uidObj);
                decoded.Claims.TryGetValue("email", out var emailObj);

                var uid = uidObj?.ToString() ?? "";
                var email = emailObj?.ToString() ?? "";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, uid),
                    new Claim(ClaimTypes.Name, string.IsNullOrEmpty(email) ? uid : email),
                    new Claim(ClaimTypes.Email, email ?? string.Empty),
                    new Claim("firebase_uid", uid)
                };

                if (!string.IsNullOrEmpty(email) && AdminEmails.Contains(email))
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));

                // Optional domain rule:
                // if (email.EndsWith("@admin.com", StringComparison.OrdinalIgnoreCase))
                //     claims.Add(new Claim(ClaimTypes.Role, "Admin"));

                var identity = new ClaimsIdentity(claims, "Firebase");
                context.User = new ClaimsPrincipal(identity);
            }
            catch
            {
                // Invalid/expired token: clear cookie to stop redirect loops
                context.Response.Cookies.Delete(CookieName, new CookieOptions
                {
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                });
            }

            await _next(context);
        }
    }
}
