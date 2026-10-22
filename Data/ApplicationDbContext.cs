
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrustVault.Api.Models;

namespace TrustVault.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Document>()
            .HasIndex(d => d.OwnerId);

        builder.Entity<DocumentPermission>()
            .HasIndex(p => new { p.DocumentId, p.UserId })
            .IsUnique();

        builder.Entity<AuditLog>()
            .HasIndex(a => a.TimestampUtc);
    }
}
