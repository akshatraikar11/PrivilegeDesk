using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivilegeDesk.Api.Data;
using PrivilegeDesk.Api.Dtos;
using PrivilegeDesk.Api.Models;
using PrivilegeDesk.Api.Services;

namespace PrivilegeDesk.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public RequestsController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<AccessRequestDto>> Create(CreateAccessRequestDto dto)
    {
        var userId = GetUserId();

        var resourceExists = await _db.Resources.AnyAsync(r => r.Id == dto.ResourceId);
        if (!resourceExists)
        {
            return BadRequest("Unknown resource.");
        }

        var request = new AccessRequest
        {
            UserId = userId,
            ResourceId = dto.ResourceId,
            Reason = dto.Reason.Trim(),
            DurationMinutes = dto.DurationMinutes,
            Status = RequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _db.AccessRequests.Add(request);
        await _db.SaveChangesAsync();

        await _audit.WriteAsync(
            userId,
            "RequestCreated",
            $"Request {request.Id} created for resource {dto.ResourceId} ({dto.DurationMinutes} minutes).");

        var created = await LoadDtoAsync(request.Id);
        return Created($"/api/requests/{request.Id}", created);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<IEnumerable<AccessRequestDto>>> GetMine()
    {
        var userId = GetUserId();

        var items = await _db.AccessRequests
            .Include(r => r.User)
            .Include(r => r.Resource)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Approver,Admin")]
    public async Task<ActionResult<IEnumerable<AccessRequestDto>>> GetPending()
    {
        // Calls the SQL stored procedure instead of writing the JOIN in LINQ.
        var rows = await _db.PendingRequestRows
            .FromSqlRaw("EXEC dbo.GetPendingRequests")
            .ToListAsync();

        var items = rows.Select(r => new AccessRequestDto
        {
            Id = r.Id,
            UserId = r.UserId,
            Username = r.Username,
            ResourceId = r.ResourceId,
            ResourceName = r.ResourceName,
            Reason = r.Reason,
            DurationMinutes = r.DurationMinutes,
            Status = r.Status.ToString(),
            RequestedAt = r.RequestedAt,
            ApprovedBy = r.ApprovedBy,
            ApprovedAt = r.ApprovedAt,
            ExpiresAt = r.ExpiresAt
        });

        return Ok(items);
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Approver,Admin")]
    public async Task<ActionResult<AccessRequestDto>> Approve(int id)
    {
        var request = await _db.AccessRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != RequestStatus.Pending)
        {
            return BadRequest("Only pending requests can be approved.");
        }

        var approverId = GetUserId();
        var now = DateTime.UtcNow;

        request.Status = RequestStatus.Approved;
        request.ApprovedBy = approverId;
        request.ApprovedAt = now;
        request.ExpiresAt = now.AddMinutes(request.DurationMinutes);

        await _db.SaveChangesAsync();
        await _audit.WriteAsync(
            approverId,
            "RequestApproved",
            $"Request {request.Id} approved. Expires at {request.ExpiresAt:u}.");

        return Ok(await LoadDtoAsync(request.Id));
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Approver,Admin")]
    public async Task<ActionResult<AccessRequestDto>> Reject(int id)
    {
        var request = await _db.AccessRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != RequestStatus.Pending)
        {
            return BadRequest("Only pending requests can be rejected.");
        }

        var approverId = GetUserId();
        request.Status = RequestStatus.Rejected;
        request.ApprovedBy = approverId;
        request.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.WriteAsync(approverId, "RequestRejected", $"Request {request.Id} rejected.");

        return Ok(await LoadDtoAsync(request.Id));
    }

    private int GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(raw!);
    }

    private async Task<AccessRequestDto?> LoadDtoAsync(int id)
    {
        var request = await _db.AccessRequests
            .Include(r => r.User)
            .Include(r => r.Resource)
            .FirstOrDefaultAsync(r => r.Id == id);

        return request is null ? null : ToDto(request);
    }

    private static AccessRequestDto ToDto(AccessRequest r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Username = r.User?.Username ?? string.Empty,
        ResourceId = r.ResourceId,
        ResourceName = r.Resource?.Name ?? string.Empty,
        Reason = r.Reason,
        DurationMinutes = r.DurationMinutes,
        Status = r.Status.ToString(),
        RequestedAt = r.RequestedAt,
        ApprovedBy = r.ApprovedBy,
        ApprovedAt = r.ApprovedAt,
        ExpiresAt = r.ExpiresAt
    };
}
