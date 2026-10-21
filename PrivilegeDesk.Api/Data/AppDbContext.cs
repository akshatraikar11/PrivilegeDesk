using Microsoft.EntityFrameworkCore;
using PrivilegeDesk.Api.Dtos;
using PrivilegeDesk.Api.Models;

namespace PrivilegeDesk.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Keyless type used only to read the GetPendingRequests stored procedure.
    public DbSet<PendingRequestRow> PendingRequestRows => Set<PendingRequestRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(64).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(100).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(400).IsRequired();
        });

        modelBuilder.Entity<AccessRequest>(entity =>
        {
            entity.Property(r => r.Reason).HasMaxLength(500).IsRequired();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Resource)
                .WithMany()
                .HasForeignKey(r => r.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Approver)
                .WithMany()
                .HasForeignKey(r => r.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.Action).HasMaxLength(80).IsRequired();
            entity.Property(a => a.Details).HasMaxLength(1000).IsRequired();

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Not a table. EF Core uses this shape when we call FromSqlRaw("EXEC ...").
        modelBuilder.Entity<PendingRequestRow>().HasNoKey().ToView(null);
    }
}
