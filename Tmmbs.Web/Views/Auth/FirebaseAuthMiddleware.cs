using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using System.Security.Claims;

namespace Tmmbs.Web.Auth
{
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private static bool _initialized;
        private static readonly object _lock = new();

        public FirebaseAuthMiddleware(RequestDelegate next) => _next = next;

        private static void EnsureFirebase()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        // Falls back to GOOGLE_APPLICATION_CREDENTIALS
                        Credential = GoogleCredential.GetApplicationDefault()
                    });
                }
                _initialized = true;
            }
        }

        public async Task Invoke(HttpContext context)
        {
            // Make sure Admin SDK is ready before any verification
            EnsureFirebase();

            var token = context.Request.Cookies["fb_idtoken"];
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, decoded.Uid),
                        new Claim(ClaimTypes.Email,
                            decoded.Claims.TryGetValue("email", out var e) ? e?.ToString() ?? "" : "")
                    };

                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Firebase"));
                }
                catch
                {
                    // invalid/expired token -> leave user anonymous
                }
            }

            await _next(context);
        }
    }
}
