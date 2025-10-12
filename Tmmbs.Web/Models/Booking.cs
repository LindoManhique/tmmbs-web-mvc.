namespace Tmmbs.Web.Models
{
    public class Booking
    {
        public string Id { get; set; } = "";
        public string ServiceId { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string Uid { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public DateTime StartAt { get; set; } // local from form; will store as Firestore Timestamp
        public string Status { get; set; } = "pending"; // pending|confirmed|cancelled
    }
}
