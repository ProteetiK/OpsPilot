using Microsoft.EntityFrameworkCore;
using OpsPilot.Domain.Entities;
using OpsPilot.Domain.Enums;

namespace OpsPilot.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Service> Services => Set<Service>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .IsRequired();

            entity.Property(user => user.IsActive)
                .IsRequired();

            entity.Property(user => user.CreatedAtUtc)
                .IsRequired();

            entity.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);

            entity.Property(log => log.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(log => log.OldValue)
                .HasMaxLength(100);

            entity.Property(log => log.NewValue)
                .HasMaxLength(100);

            entity.Property(log => log.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(log => log.TargetUserId);

            entity.HasIndex(log => log.ActorUserId);

            entity.HasIndex(log => log.CreatedAtUtc);
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasKey(incident => incident.Id);

            entity.Property(incident => incident.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(incident => incident.Description)
                .HasMaxLength(5000)
                .IsRequired();

            entity.Property(incident => incident.Severity)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(incident => incident.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(incident => incident.CreatedAtUtc)
                .IsRequired();

            entity.Property(incident => incident.UpdatedAtUtc)
                .IsRequired();

            entity.HasIndex(incident => incident.Status);
            entity.HasIndex(incident => incident.Severity);
            entity.HasIndex(incident => incident.ServiceId);
            entity.HasIndex(incident => incident.AssignedToUserId);
        });
    }
}