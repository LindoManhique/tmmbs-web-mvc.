using System;
using System.Collections.Generic;

namespace Tmmbs.Web.Models
{
    public record DocVM(string Id, IDictionary<string, object> Data);

    public record BookingVM(
        string Id,
        string? UserId,
        string? Name,
        string? Email,
        string? Phone,
        string? ServiceName,
        DateTime? StartAt,
        DateTime? CreatedAt,
        string? Status,
        string? Notes
    );

    public class AdminDashboardVM
    {
        public List<DocVM> Contacts { get; set; } = new();
        public List<DocVM> Services { get; set; } = new();
        public List<BookingVM> Bookings { get; set; } = new();
    }
}
