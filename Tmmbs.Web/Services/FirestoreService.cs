using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.Configuration;
using Tmmbs.Web.Models;

namespace Tmmbs.Web.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _db;

        public FirestoreService(IConfiguration cfg)
        {
            var projectId = cfg["FirebaseWeb:projectId"];
            if (string.IsNullOrWhiteSpace(projectId))
                throw new InvalidOperationException("FirebaseWeb:projectId is missing from configuration.");

            // ---- Resolve GoogleCredential (JSON first, then file path, then ADC) ----
            GoogleCredential ResolveGoogleCredential()
            {
                // JSON blob stored in an App Setting (Azure-friendly)
                var credJson =
                    Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON")
                    ?? cfg["Google:CredentialsJson"];

                if (!string.IsNullOrWhiteSpace(credJson))
                    return GoogleCredential.FromJson(credJson);

                // File path (env or config)
                var credPath =
                    Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                    ?? cfg["Google:CredentialPath"]
                    ?? cfg["Google:CredentialsPath"];

                if (string.IsNullOrWhiteSpace(credPath))
                {
                    var localKey = Path.Combine(AppContext.BaseDirectory, "tmmbs-firebase-key.json");
                    if (File.Exists(localKey))
                        credPath = localKey;
                }

                if (!string.IsNullOrWhiteSpace(credPath) && File.Exists(credPath))
                    return GoogleCredential.FromFile(credPath);

                // Application Default Credentials (last resort)
                return GoogleCredential.GetApplicationDefault();
            }

            var cred = ResolveGoogleCredential().CreateScoped(FirestoreClient.DefaultScopes);

            // Build Firestore with explicit credential (stable on Azure)
            _db = new FirestoreDbBuilder
            {
                ProjectId = projectId!,
                Credential = cred
            }.Build();
        }

        // ===== USERS =====
        public async Task<Dictionary<string, object>?> GetUserAsync(string uid)
        {
            var snap = await _db.Collection("users").Document(uid).GetSnapshotAsync();
            return snap.Exists ? snap.ToDictionary() : null;
        }

        // ===== CONTACT =====
        public async Task AddContactMessageAsync(string? uid, string name, string email, string message)
        {
            await _db.Collection("contact_messages").AddAsync(new
            {
                uid = string.IsNullOrWhiteSpace(uid) ? "anonymous" : uid,
                name,
                email,
                message,
                createdAt = Timestamp.GetCurrentTimestamp()
            });
        }

        // ===== SERVICES =====
        public async Task<List<ServiceItem>> GetActiveServicesAsync()
        {
            var list = new List<ServiceItem>();

            // Merge lowercase and capitalized collections
            var lowerTask = _db.Collection("services").GetSnapshotAsync();
            var upperTask = _db.Collection("Services").GetSnapshotAsync();
            await Task.WhenAll(lowerTask, upperTask);

            var allDocs = lowerTask.Result.Documents
                .Concat(upperTask.Result.Documents)
                .GroupBy(d => d.Id)
                .Select(g => g.First());

            foreach (var d in allDocs)
            {
                var name = d.TryGetValue("name", out string n1) ? n1 :
                           d.TryGetValue("title", out string n2) ? n2 : "(Untitled)";

                var desc = d.TryGetValue("description", out string de1) ? de1 :
                           d.TryGetValue("intro", out string de2) ? de2 : "";

                var price = d.TryGetValue("price", out double p) ? (decimal)p : 0m;
                var duration = d.TryGetValue("durationMins", out long dm) ? (int)dm : 0;
                var active = d.TryGetValue("active", out bool a) ? a : true;

                var item = new ServiceItem
                {
                    Id = d.Id,
                    Name = name,
                    Category = d.TryGetValue("category", out string cat) ? cat : "",
                    Description = desc,
                    Price = price,
                    DurationMins = duration,
                    ImageUrl = d.TryGetValue("imageUrl", out string img) ? img : null,
                    Active = active
                };

                if (d.TryGetValue("webUrl", out string web) && !string.IsNullOrWhiteSpace(web))
                {
                    item.Description = string.IsNullOrWhiteSpace(item.Description) ? web : $"{item.Description}\nMore: {web}";
                }

                if (item.Active) list.Add(item);
            }

            return list.OrderBy(s => s.Name).ToList();
        }

        public async Task<ServiceItem?> GetServiceByIdAsync(string id)
        {
            var snap = await _db.Collection("services").Document(id).GetSnapshotAsync();
            if (!snap.Exists)
                snap = await _db.Collection("Services").Document(id).GetSnapshotAsync();
            if (!snap.Exists) return null;

            var name = snap.TryGetValue("name", out string n1) ? n1 :
                       snap.TryGetValue("title", out string n2) ? n2 : "(Untitled)";

            var desc = snap.TryGetValue("description", out string de1) ? de1 :
                       snap.TryGetValue("intro", out string de2) ? de2 : "";

            var item = new ServiceItem
            {
                Id = snap.Id,
                Name = name,
                Category = snap.TryGetValue("category", out string cat) ? cat : "",
                Description = desc,
                Price = snap.TryGetValue("price", out double p) ? (decimal)p : 0m,
                DurationMins = snap.TryGetValue("durationMins", out long dm) ? (int)dm : 0,
                ImageUrl = snap.TryGetValue("imageUrl", out string img) ? img : null,
                Active = snap.TryGetValue("active", out bool a) ? a : true
            };

            if (snap.TryGetValue("webUrl", out string web) && !string.IsNullOrWhiteSpace(web))
            {
                item.Description = string.IsNullOrWhiteSpace(item.Description) ? web : $"{item.Description}\nMore: {web}";
            }

            return item;
        }

        // ===== MEDIA =====
        public async Task<List<MediaItem>> GetMediaAsync(int take = 48)
        {
            var col = _db.Collection("media");
            var q = await col.Limit(take).GetSnapshotAsync();

            return q.Documents.Select(d => new MediaItem
            {
                Id = d.Id,
                Title = d.TryGetValue("title", out string t) ? t : "",
                Type = d.TryGetValue("type", out string tp) ? tp : "image",
                Url = d.TryGetValue("url", out string u) ? u : "",
                ThumbnailUrl = d.TryGetValue("thumbnailUrl", out string th) ? th : null,
                CreatedAt = d.TryGetValue("createdAt", out Timestamp ts) ? ts.ToDateTime() : (DateTime?)null
            }).ToList();
        }

        // ===== BOOKINGS =====
        public async Task<string> CreateBookingAsync(Booking b)
        {
            var utc = b.StartAt.Kind == DateTimeKind.Utc ? b.StartAt : b.StartAt.ToUniversalTime();

            var doc = await _db.Collection("bookings").AddAsync(new
            {
                serviceId = b.ServiceId,
                serviceName = b.ServiceName,
                uid = string.IsNullOrWhiteSpace(b.Uid) ? "anonymous" : b.Uid,
                name = b.Name,
                email = b.Email,
                phone = b.Phone,
                notes = b.Notes,
                startAt = Timestamp.FromDateTime(utc),
                status = string.IsNullOrWhiteSpace(b.Status) ? "pending" : b.Status,
                createdAt = Timestamp.GetCurrentTimestamp()
            });
            return doc.Id;
        }

        public async Task<List<Booking>> GetBookingsForUserAsync(string uid)
        {
            var q = await _db.Collection("bookings")
                             .WhereEqualTo("uid", uid)
                             .OrderByDescending("startAt")
                             .GetSnapshotAsync();

            return q.Documents.Select(d => new Booking
            {
                Id = d.Id,
                ServiceId = d.TryGetValue("serviceId", out string sid) ? sid : "",
                ServiceName = d.TryGetValue("serviceName", out string sn) ? sn : "",
                Uid = uid,
                Name = d.TryGetValue("name", out string n) ? n : "",
                Email = d.TryGetValue("email", out string e) ? e : "",
                Phone = d.TryGetValue("phone", out string ph) ? ph : "",
                Notes = d.TryGetValue("notes", out string nt) ? nt : "",
                StartAt = d.TryGetValue("startAt", out Timestamp ts) ? ts.ToDateTime() : DateTime.MinValue,
                Status = d.TryGetValue("status", out string st) ? st : "pending"
            }).ToList();
        }
    }

    // ---- Helpers to safely read fields ----
    internal static class SnapExt
    {
        public static bool TryGetValue<T>(this DocumentSnapshot snap, string field, out T value)
        {
            if (snap.ContainsField(field))
            {
                try
                {
                    value = snap.GetValue<T>(field);
                    return true;
                }
                catch
                {
                    // ignore conversion errors
                }
            }

            value = default!;
            return false;
        }
    }
}
