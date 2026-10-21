using Microsoft.EntityFrameworkCore;
using PrivilegeDesk.Api.Models;

namespace PrivilegeDesk.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Create / update the stored procedure that the pending-requests endpoint uses.
        await db.Database.ExecuteSqlRawAsync(GetPendingRequestsSql);

        if (await db.Users.AnyAsync())
        {
            return;
        }

        // Demo password for every seeded account. BCrypt stores a one-way hash, not the password.
        const string demoPassword = "Password123!";

        db.Users.AddRange(
            new User
            {
                Username = "employee",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword),
                Role = UserRole.Employee
            },
            new User
            {
                Username = "approver",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword),
                Role = UserRole.Approver
            },
            new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword),
                Role = UserRole.Admin
            });

        db.Resources.AddRange(
            new Resource { Name = "Prod DB Server", Description = "Production SQL Server used by the billing app." },
            new Resource { Name = "Linux Jump Host", Description = "Bastion host for SSH into private subnets." },
            new Resource { Name = "AWS Root Console", Description = "Break-glass AWS account root credentials." },
            new Resource { Name = "Payroll File Share", Description = "Restricted Windows share with payroll exports." });

        await db.SaveChangesAsync();
    }

    // CREATE OR ALTER is supported on modern SQL Server / LocalDB.
    private const string GetPendingRequestsSql = """
        CREATE OR ALTER PROCEDURE dbo.GetPendingRequests
        AS
        BEGIN
            SET NOCOUNT ON;

            SELECT
                ar.Id,
                ar.UserId,
                u.Username,
                ar.ResourceId,
                r.Name AS ResourceName,
                ar.Reason,
                ar.DurationMinutes,
                ar.Status,
                ar.RequestedAt,
                ar.ApprovedBy,
                ar.ApprovedAt,
                ar.ExpiresAt
            FROM AccessRequests ar
            INNER JOIN Users u ON u.Id = ar.UserId
            INNER JOIN Resources r ON r.Id = ar.ResourceId
            WHERE ar.Status = 0 -- RequestStatus.Pending
            ORDER BY ar.RequestedAt;
        END
        """;
}
