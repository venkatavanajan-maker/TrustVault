using Microsoft.AspNetCore.Identity;

namespace TrustVault.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
