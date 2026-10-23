using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrustVault.Api.Data;
using TrustVault.Api.Models;

namespace TrustVault.Api.Services;

public sealed class AuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AppendAsync(
        string? userId,
        string action,
        string resourceType,
        string resourceId,
        bool succeeded,
        string details,
        CancellationToken cancellationToken = default)
    {
        // Serialize writes so two requests don't select the same previous hash.
        // SQL Server's transaction-level application lock coordinates app instances.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();

        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = 'TrustVault.AuditChain',
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            SELECT @result;
            """;

        var lockResult = await command.ExecuteScalarAsync(cancellationToken);
        var lockCode = Convert.ToInt32(lockResult);
if (lockCode < 0)
{
    throw new InvalidOperationException(
        $"Failed to acquire the audit-chain lock. SQL Server returned {lockCode}.");
}
                var verification = await VerifyChainAsync(cancellationToken);

        if (!verification.IsValid)
        {
            throw new InvalidOperationException(
                $"Audit chain tampering detected at entry {verification.InvalidEntryId}.");
        }
        var previous = await _db.AuditLogs
            .OrderByDescending(a => a.Id)
            .Select(a => new { a.Id, a.EntryHash })
            .FirstOrDefaultAsync(cancellationToken);

        var previousHash = previous?.EntryHash ?? "GENESIS";
        var timestamp = new DateTime(
    DateTime.UtcNow.Ticks / 10 * 10,
    DateTimeKind.Utc);

        var canonicalData = JsonSerializer.Serialize(new
        {
            timestamp,
            userId,
            action,
            resourceType,
            resourceId,
            succeeded,
            details,
            previousHash
        });

        var entryHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalData)));

        var entry = new AuditLog
        {
            TimestampUtc = timestamp,
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Succeeded = succeeded,
            Details = details,
            PreviousHash = previousHash,
            EntryHash = entryHash
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<(bool IsValid, long? InvalidEntryId)> VerifyChainAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await _db.AuditLogs
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var expectedPreviousHash = "GENESIS";

        foreach (var entry in entries)
        {
            if (!string.Equals(
                    entry.PreviousHash,
                    expectedPreviousHash,
                    StringComparison.Ordinal))
            {
                return (false, entry.Id);
            }

            var canonicalData = JsonSerializer.Serialize(new
            {
                timestamp = DateTime.SpecifyKind(
    entry.TimestampUtc,
    DateTimeKind.Utc),
                userId = entry.UserId,
                action = entry.Action,
                resourceType = entry.ResourceType,
                resourceId = entry.ResourceId,
                succeeded = entry.Succeeded,
                details = entry.Details,
                previousHash = entry.PreviousHash
            });

            var expectedHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(canonicalData)));

            if (!string.Equals(
        entry.EntryHash,
        expectedHash,
        StringComparison.Ordinal))
{
    return (false, entry.Id);
}
            expectedPreviousHash = entry.EntryHash;
        }

        return (true, null);
    }
}
