using Domain.Common;

namespace Domain.Entities;

public sealed class ContactMessage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
