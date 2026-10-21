namespace PrivilegeDesk.Api.Models;

// Append-only history of important actions (login, request, approve, reject, expire).
public class AuditLog
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
