
namespace TrustVault.Api.Models;

public class AuditLog
{
    public long Id { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string ResourceId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string Details { get; set; } = string.Empty;

    public string PreviousHash { get; set; } = string.Empty;

    public string EntryHash { get; set; } = string.Empty;
}
