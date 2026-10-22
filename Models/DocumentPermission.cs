
namespace TrustVault.Api.Models;

public class DocumentPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }

    public string UserId { get; set; } = string.Empty;

    // Examples: Viewer or Editor.
    public string Role { get; set; } = "Viewer";

    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;

    public string GrantedByUserId { get; set; } = string.Empty;
}
