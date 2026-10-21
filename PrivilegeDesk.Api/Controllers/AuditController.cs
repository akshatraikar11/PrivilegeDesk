using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivilegeDesk.Api.Data;
using PrivilegeDesk.Api.Dtos;

namespace PrivilegeDesk.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAll()
    {
        var items = await _db.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Username = a.User != null ? a.User.Username : string.Empty,
                Action = a.Action,
                Details = a.Details,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(items);
    }
}
