using System.ComponentModel.DataAnnotations;
using PrivilegeDesk.Api.Models;

namespace PrivilegeDesk.Api.Dtos;

public class CreateAccessRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "ResourceId must be a positive number.")]
    public int ResourceId { get; set; }

    [Required]
    [MinLength(5, ErrorMessage = "Please explain why you need access (at least 5 characters).")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Range(15, 480, ErrorMessage = "Duration must be between 15 and 480 minutes.")]
    public int DurationMinutes { get; set; }
}

public class AccessRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// Matches the columns returned by stored procedure dbo.GetPendingRequests.
// HasNoKey() in AppDbContext because this is a query result, not a table.
public class PendingRequestRow
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
