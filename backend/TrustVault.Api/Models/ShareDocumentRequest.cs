
using System.ComponentModel.DataAnnotations;

namespace TrustVault.Api.Models;

public class ShareDocumentRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Viewer|Editor)$")]
    public string Role { get; set; } = "Viewer";
}
