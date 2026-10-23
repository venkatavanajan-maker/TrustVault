using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrustVault.Api.Data;
using TrustVault.Api.Models;
using TrustVault.Api.Services;

namespace TrustVault.Api.Tests;

public class AuditServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AuditLog CreateValidEntry()
    {
        var timestamp = new DateTime(
            DateTime.UtcNow.Ticks / 10 * 10,
            DateTimeKind.Utc);

        const string previousHash = "GENESIS";
        const string action = "Uploaded";
        const string resourceType = "Document";
        const string resourceId = "test-document-1";
        const string details = "Test upload";

        var canonicalData = JsonSerializer.Serialize(new
        {
            timestamp,
            userId = (string?)null,
            action,
            resourceType,
            resourceId,
            succeeded = true,
            details,
            previousHash
        });

        var entryHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalData)));

        return new AuditLog
        {
            Id = 1,
            TimestampUtc = timestamp,
            UserId = null,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Succeeded = true,
            Details = details,
            PreviousHash = previousHash,
            EntryHash = entryHash
        };
    }

    [Fact]
    public async Task VerifyChainAsync_ValidEntry_ReturnsValid()
    {
        await using var context = CreateContext();
        context.AuditLogs.Add(CreateValidEntry());
        await context.SaveChangesAsync();

        var service = new AuditService(context);

        var result = await service.VerifyChainAsync();

        Assert.True(result.IsValid);
        Assert.Null(result.InvalidEntryId);
    }

    [Fact]
    public async Task VerifyChainAsync_TamperedEntry_ReturnsInvalid()
    {
        await using var context = CreateContext();
        var entry = CreateValidEntry();
        context.AuditLogs.Add(entry);
        await context.SaveChangesAsync();

        entry.Details = "Modified after logging";
        await context.SaveChangesAsync();

        var service = new AuditService(context);

        var result = await service.VerifyChainAsync();

        Assert.False(result.IsValid);
        Assert.Equal(1, result.InvalidEntryId);
    }
}
