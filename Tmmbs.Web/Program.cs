using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Tmmbs.Web.Auth;

var builder = WebApplication.CreateBuilder(args);

// MVC + DI services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<Tmmbs.Web.Services.FirestoreService>();

// Initialize Firebase Admin once (prefer explicit file path)
var credPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
              ?? builder.Configuration["Google:CredentialsPath"];

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
