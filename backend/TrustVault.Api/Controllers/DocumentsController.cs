
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrustVault.Api.Data;
using TrustVault.Api.Models;
using TrustVault.Api.Services;

namespace TrustVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 10 * 1024 * 1024;

    public DocumentsController(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        AuditService audit)
    {
        _db = db;
        _environment = environment;
        _audit = audit;
    }

    [Authorize(Roles = "Owner,Administrator")]
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string title,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("Please select a file.");

        if (file.Length > MaxFileSize)
            return BadRequest("Maximum file size is 10 MB.");

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("Document title is required.");

        var originalName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(originalName))
            return BadRequest("Invalid file name.");

        var storageDirectory = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Documents");

        Directory.CreateDirectory(storageDirectory);

        var storageKey = Guid.NewGuid().ToString("N");
        var storagePath = Path.Combine(
            storageDirectory,
            storageKey);

        try
        {
            await using (var stream = new FileStream(
                storagePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await file.CopyToAsync(
                    stream,
                    cancellationToken);
            }

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Title = title.Trim(),
                OwnerId = userId,
                StorageKey = storageKey,
                OriginalFileName = originalName,
                ContentType = "application/octet-stream",
                SizeBytes = file.Length,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Documents.Add(document);
            await _db.SaveChangesAsync(cancellationToken);

            await _audit.AppendAsync(
                userId,
                "DocumentUploaded",
                "Document",
                document.Id.ToString(),
                true,
                $"Uploaded document '{document.Title}' ({document.SizeBytes} bytes).",
                cancellationToken);

            return Ok(new
            {
                message = "Document uploaded successfully.",
                documentId = document.Id,
                title = document.Title,
                fileName = document.OriginalFileName,
                sizeBytes = document.SizeBytes,
                createdAtUtc = document.CreatedAtUtc
            });
        }
        catch
        {
            if (System.IO.File.Exists(storagePath))
                System.IO.File.Delete(storagePath);

            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyDocuments(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var documents = await _db.Documents
            .AsNoTracking()
            .Where(d => d.OwnerId == userId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.OriginalFileName,
                d.ContentType,
                d.SizeBytes,
                d.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(documents);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var document = await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == id,
                cancellationToken);

        if (document == null)
            return NotFound("Document not found.");

        var isOwner = document.OwnerId == userId;

        var hasPermission = await _db.DocumentPermissions
            .AnyAsync(
                p => p.DocumentId == id &&
                     p.UserId == userId &&
                     (p.Role == "Viewer" || p.Role == "Editor"),
                cancellationToken);

        
if (!isOwner && !hasPermission)
{
    await _audit.AppendAsync(
        userId,
        "DocumentDownloadDenied",
        "Document",
        id.ToString(),
        false,
        $"Denied download attempt for document '{document.Title}'.",
        cancellationToken);

    return NotFound("Document not found.");
}


        var storagePath = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Documents",
            document.StorageKey);

        if (!System.IO.File.Exists(storagePath))
            return NotFound("Stored document file not found.");

        await _audit.AppendAsync(
            userId,
            "DocumentDownloaded",
            "Document",
            document.Id.ToString(),
            true,
            $"Downloaded document '{document.Title}'.",
            cancellationToken);

        return PhysicalFile(
            storagePath,
            document.ContentType,
            document.OriginalFileName);
    }

    [HttpPost("{id:guid}/share")]
    public async Task<IActionResult> ShareDocument(
        Guid id,
        [FromBody] ShareDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
            return Unauthorized();

        if (request == null ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Recipient email is required.");
        }

        if (request.Role != "Viewer" &&
            request.Role != "Editor")
        {
            return BadRequest("Role must be Viewer or Editor.");
        }

        var document = await _db.Documents
            .FirstOrDefaultAsync(
                d => d.Id == id && d.OwnerId == ownerId,
                cancellationToken);

        if (document == null)
            return NotFound("Document not found.");

        var email = request.Email.Trim();

        var recipient = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Email != null &&
                     u.Email.ToLower() == email.ToLower(),
                cancellationToken);

        if (recipient == null)
            return NotFound("Registered user not found.");

        if (recipient.Id == ownerId)
            return BadRequest("You already own this document.");

        var permission = await _db.DocumentPermissions
            .FirstOrDefaultAsync(
                p => p.DocumentId == id &&
                     p.UserId == recipient.Id,
                cancellationToken);

        if (permission == null)
        {
            permission = new DocumentPermission
            {
                DocumentId = id,
                UserId = recipient.Id,
                Role = request.Role,
                GrantedAtUtc = DateTime.UtcNow,
                GrantedByUserId = ownerId
            };

            _db.DocumentPermissions.Add(permission);
        }
        else
        {
            permission.Role = request.Role;
            permission.GrantedAtUtc = DateTime.UtcNow;
            permission.GrantedByUserId = ownerId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.AppendAsync(
            ownerId,
            "DocumentPermissionGranted",
            "Document",
            document.Id.ToString(),
            true,
            $"Granted role '{permission.Role}' to user '{recipient.Email}' for document '{document.Title}'.",
            cancellationToken);

        return Ok(new
        {
            message = "Document shared successfully.",
            documentId = id,
            recipientEmail = recipient.Email,
            role = permission.Role
        });
    }
[HttpGet("{id:guid}/shares")]
public async Task<IActionResult> GetDocumentShares(
    Guid id,
    CancellationToken cancellationToken)
{
    var ownerId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(ownerId))
        return Unauthorized();

    var document = await _db.Documents
        .FirstOrDefaultAsync(
            d => d.Id == id && d.OwnerId == ownerId,
            cancellationToken);

    if (document == null)
        return NotFound("Document not found.");

    var shares = await (
        from permission in _db.DocumentPermissions.AsNoTracking()
        join recipient in _db.Users.AsNoTracking()
            on permission.UserId equals recipient.Id
        where permission.DocumentId == id
        orderby permission.GrantedAtUtc descending
        select new
        {
            userId = recipient.Id,
            email = recipient.Email,
            role = permission.Role,
            grantedAtUtc = permission.GrantedAtUtc
        })
        .ToListAsync(cancellationToken);

    return Ok(shares);
}
    [HttpDelete("{id:guid}/share/{userId}")]
    public async Task<IActionResult> RevokeDocumentAccess(
        Guid id,
        string userId,
        CancellationToken cancellationToken)
    {
        var ownerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("User ID is required.");

        var document = await _db.Documents
            .FirstOrDefaultAsync(
                d => d.Id == id && d.OwnerId == ownerId,
                cancellationToken);

        if (document == null)
            return NotFound("Document not found.");

        var permission = await _db.DocumentPermissions
            .FirstOrDefaultAsync(
                p => p.DocumentId == id &&
                     p.UserId == userId,
                cancellationToken);

        if (permission == null)
            return NotFound("Permission not found.");

        _db.DocumentPermissions.Remove(permission);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.AppendAsync(
            ownerId,
            "DocumentPermissionRevoked",
            "Document",
            document.Id.ToString(),
            true,
            $"Revoked access for user '{userId}' to document '{document.Title}'.",
            cancellationToken);

        return Ok(new
        {
            message = "Document access revoked successfully.",
            documentId = id,
            revokedUserId = userId
        });
    }

    [HttpGet("shared-with-me")]
    public async Task<IActionResult> GetSharedDocuments(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var documents = await (
            from permission in _db.DocumentPermissions.AsNoTracking()
            join document in _db.Documents.AsNoTracking()
                on permission.DocumentId equals document.Id
            where permission.UserId == userId
                  && document.OwnerId != userId
            orderby permission.GrantedAtUtc descending
            select new
            {
                document.Id,
                document.Title,
                document.OriginalFileName,
                document.ContentType,
                document.SizeBytes,
                document.CreatedAtUtc,
                document.OwnerId,
                permission.Role,
                permission.GrantedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(documents);
    }
}
