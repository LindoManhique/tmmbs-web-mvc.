using System;
using System.IO;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmmbs.Web.Auth;           // your custom Firebase cookie middleware
using Tmmbs.Web.Services;       // FirestoreService

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------
// MVC + Services
// ------------------------------------------------------
builder.Services.AddControllersWithViews();

// === FirestoreDb via DI for controllers/services that need it ===
builder.Services.AddSingleton(provider =>
{
    var cfg = provider.GetRequiredService<IConfiguration>();

    // Project Id (from appsettings or fallback)
    var projectId = cfg["FirebaseWeb:projectId"];
    if (string.IsNullOrWhiteSpace(projectId))
        projectId = "tmmbs-25364"; // fallback

    // --- Resolve GoogleCredential ---
    // Priority:
    // 1) GOOGLE_CREDENTIALS_JSON (env or config)
    // 2) GOOGLE_APPLICATION_CREDENTIALS / Google:CredentialPath (path)
    // 3) Application Default Credentials (ADC)
    GoogleCredential credential;

    // (1) Raw JSON in settings (Option A)
    var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON")
              ?? cfg["Google:CredentialsJson"];
    if (!string.IsNullOrWhiteSpace(json))
    {
        credential = GoogleCredential.FromJson(json);
    }
    else
    {
        // (2) Path to key file (Option B)
        string? credPath =
            Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
            ?? cfg["Google:CredentialPath"]
            ?? cfg["Google:CredentialsPath"];

        // Fallback to local file next to published binary (Azure “Advanced Tools” upload pattern)
        if (string.IsNullOrWhiteSpace(credPath))
        {
            var localKey = Path.Combine(AppContext.BaseDirectory, "tmmbs-firebase-key.json");
            if (File.Exists(localKey))
                credPath = localKey;
        }

        if (!string.IsNullOrWhiteSpace(credPath) && File.Exists(credPath))
        {
            credential = GoogleCredential.FromFile(credPath);
        }
        else
        {
            // (3) As a final fallback, use ADC (will work on GCP; on Azure only if set up)
            credential = GoogleCredential.GetApplicationDefault();
        }
    }

    // Ensure Firestore scopes
    credential = credential.CreateScoped(FirestoreClient.DefaultScopes);

    // Build FirestoreDb explicitly with credential
    var db = new FirestoreDbBuilder
    {
        ProjectId = projectId!,
        Credential = credential
    }.Build();

    return db;
});

// Optional app-level wrapper if you use it elsewhere
builder.Services.AddSingleton<FirestoreService>();

// ------------------------------------------------------
// Initialize Firebase Admin SDK once (so your middleware can verify tokens)
// ------------------------------------------------------
static FirebaseApp EnsureFirebaseApp(IConfiguration cfg)
{
    // Try JSON first
    var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON")
              ?? cfg["Google:CredentialsJson"];

    GoogleCredential cred;
    if (!string.IsNullOrWhiteSpace(json))
    {
        cred = GoogleCredential.FromJson(json);
    }
    else
    {
        // then path variants
        string? credPath =
            Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
            ?? cfg["Google:CredentialPath"]
            ?? cfg["Google:CredentialsPath"];

        if (string.IsNullOrWhiteSpace(credPath))
        {
            var localKey = Path.Combine(AppContext.BaseDirectory, "tmmbs-firebase-key.json");
            if (File.Exists(localKey)) credPath = localKey;
        }

        cred = !string.IsNullOrWhiteSpace(credPath) && File.Exists(credPath)
            ? GoogleCredential.FromFile(credPath)
            : GoogleCredential.GetApplicationDefault();
    }

    return FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
    {
        Credential = cred
    });
}

EnsureFirebaseApp(builder.Configuration);

// ------------------------------------------------------
// HTTP pipeline
// ------------------------------------------------------
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // HSTS is OK if you need it; left out for brevity.
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ IMPORTANT: verify Firebase cookie and set HttpContext.User BEFORE MVC routing
app.UseMiddleware<FirebaseAuthMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
