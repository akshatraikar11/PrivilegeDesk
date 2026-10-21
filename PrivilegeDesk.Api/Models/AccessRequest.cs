namespace PrivilegeDesk.Api.Models;

// One row = one "please let me into this resource for N minutes" ticket.
public class AccessRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int ResourceId { get; set; }
    public Resource? Resource { get; set; }

    public string Reason { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public RequestStatus Status { get; set; }

    public DateTime RequestedAt { get; set; }

    // Null until an Approver/Admin approves the request.
    public int? ApprovedBy { get; set; }
    public User? Approver { get; set; }

    public DateTime? ApprovedAt { get; set; }

    // Set on approval: ApprovedAt + DurationMinutes.
    // The worker service later marks the row Expired when this time has passed.
    public DateTime? ExpiresAt { get; set; }
}
