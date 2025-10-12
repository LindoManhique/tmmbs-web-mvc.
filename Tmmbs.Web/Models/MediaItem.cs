namespace Tmmbs.Web.Models
{
    public class MediaItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Type { get; set; } = "image"; // image | video
        public string Url { get; set; } = "";
        public string? ThumbnailUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
