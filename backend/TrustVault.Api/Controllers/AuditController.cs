
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrustVault.Api.Data;
using TrustVault.Api.Services;

namespace TrustVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class AuditController : ControllerBase
{
    private readonly AuditService _audit;
    private readonly ApplicationDbContext _db;

    public AuditController(
        AuditService audit,
        ApplicationDbContext db)
    {
        _audit = audit;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent(
        CancellationToken cancellationToken)
    {
        var entries = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Id)
            .Take(100)
            .Select(a => new
            {
                a.Id,
                a.TimestampUtc,
                a.UserId,
                a.Action,
                a.ResourceType,
                a.ResourceId,
                a.Succeeded,
                a.Details
            })
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("verify")]
    public async Task<IActionResult> Verify(
        CancellationToken cancellationToken)
    {
        var result = await _audit.VerifyChainAsync(
            cancellationToken);

        if (!result.IsValid)
        {
            return Conflict(new
            {
                status = "TAMPERING_DETECTED",
                message = "The audit hash chain is invalid.",
                invalidEntryId = result.InvalidEntryId
            });
        }

        return Ok(new
        {
            status = "VALID",
            message = "The audit hash chain is valid."
        });
    }
}
