using System.IO;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Tmmbs.Web.Auth;

var builder = WebApplication.CreateBuilder(args);

// MVC + DI services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<Tmmbs.Web.Services.FirestoreService>();

// --- Locate Firebase Admin credentials in this order (first found wins):
// 1) GOOGLE_APPLICATION_CREDENTIALS environment variable
// 2) appsettings.json -> "Google:CredentialsPath"
// 3) A file named "tmmbs-firebase-key.json" sitting next to the EXE (USB-friendly)
string? credPath =
    Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
    ?? builder.Configuration["Google:CredentialsPath"];

if (string.IsNullOrWhiteSpace(credPath))
{
    var localKey = Path.Combine(AppContext.BaseDirectory, "tmmbs-firebase-key.json");
    if (File.Exists(localKey))
        credPath = localKey;
}

// 🔴 IMPORTANT: Tell Google client libraries (FirestoreDb) where the key is.
if (!string.IsNullOrWhiteSpace(credPath))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credPath);
}

// Initialize Firebase Admin once (uses the same key)
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = !string.IsNullOrWhiteSpace(credPath)
            ? GoogleCredential.FromFile(credPath!)
            : GoogleCredential.GetApplicationDefault()
    });
}

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Verify Firebase ID token from cookie for each request
app.UseMiddleware<FirebaseAuthMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
