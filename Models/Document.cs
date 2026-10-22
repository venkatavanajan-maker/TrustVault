
namespace TrustVault.Api.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    // Identity ID of the user who owns this document.
    public string OwnerId { get; set; } = string.Empty;

    // Random storage key, not the original filename.
    public string StorageKey { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
