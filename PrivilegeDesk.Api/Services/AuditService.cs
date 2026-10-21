using PrivilegeDesk.Api.Data;
using PrivilegeDesk.Api.Models;

namespace PrivilegeDesk.Api.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(int userId, string action, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
