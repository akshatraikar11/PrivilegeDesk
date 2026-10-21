using Microsoft.EntityFrameworkCore;

namespace PrivilegeDesk.ExpiryService.Models;

// These types match PrivilegeDesk.Api. In a bigger product they would live in a
// shared class library. This homework keeps exactly three projects.

public enum RequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Role { get; set; }
}

public class Resource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AccessRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ResourceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Resource> Resources => Set<Resource>();
}
