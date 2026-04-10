using System.Collections;
using api.Application.Abstractions;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace api.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserAccessor currentUserAccessor)
        : base(options)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Backlog> Backlogs => Set<Backlog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.FirstName)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("citext");

            entity.Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("citext");

            entity.Property(p => p.DateOfBirth)
                .IsRequired()
                .HasColumnType("date");

            entity.Property(p => p.Gender)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(p => p.PhoneNumber)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(p => p.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(p => p.DeletedAt)
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(p => new { p.LastName, p.FirstName })
                .HasDatabaseName("ix_patients_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(u => u.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(u => u.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(u => u.DeletedAt)
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(u => u.Username)
                .HasDatabaseName("ix_users_username_active_unique")
                .IsUnique()
                .HasFilter("\"IsDeleted\" = FALSE");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Status)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(a => a.Notes)
                .HasMaxLength(2000);

            entity.Property(a => a.AppointmentDate)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            entity.Property(a => a.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(a => a.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(a => a.DeletedAt)
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(a => a.AppointmentDate)
                .HasDatabaseName("ix_appointments_appointment_date");

            entity.HasIndex(a => a.DoctorId)
                .HasDatabaseName("ix_appointments_doctor_id");

            entity.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Backlog>(entity =>
        {
            entity.ToTable("backlogs");

            entity.HasKey(b => b.Id);

            entity.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(b => b.Description)
                .HasMaxLength(4000);

            entity.Property(b => b.Priority)
                .IsRequired()
                .HasMaxLength(16);

            entity.Property(b => b.Status)
                .IsRequired()
                .HasMaxLength(16);

            entity.Property(b => b.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(b => b.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(b => b.DeletedAt)
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(b => b.AssignedToUserId)
                .HasDatabaseName("ix_backlogs_assigned_to_user_id");

            entity.HasIndex(b => b.Status)
                .HasDatabaseName("ix_backlogs_status");

            entity.HasOne(b => b.AssignedTo)
                .WithMany()
                .HasForeignKey(b => b.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.UserId)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(a => a.Action)
                .IsRequired()
                .HasMaxLength(16);

            entity.Property(a => a.EntityName)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(a => a.EntityId)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(a => a.OldValues)
                .HasColumnType("text");

            entity.Property(a => a.NewValues)
                .HasColumnType("text");

            entity.Property(a => a.Timestamp)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(a => a.Timestamp)
                .HasDatabaseName("ix_audit_logs_timestamp");
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareSaveChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareSaveChanges();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    private void PrepareSaveChanges()
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var actorUserId = _currentUserAccessor.GetAuditUserId();

            ApplyCreatedAtForNewAuditableEntities(utcNow);

            var softDeletedViaRemove = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var pendingAudits = new List<AuditLog>();

            ConvertHardDeletesToSoftDeletes(utcNow, actorUserId, softDeletedViaRemove, pendingAudits);

            foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is not AuditLog))
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        pendingAudits.Add(CreateInsertAudit(entry, actorUserId, utcNow));
                        break;
                    case EntityState.Modified when !softDeletedViaRemove.Contains(entry.Entity):
                    {
                        var updateAudit = TryCreateUpdateAudit(entry, actorUserId, utcNow);
                        if (updateAudit is not null)
                        {
                            pendingAudits.Add(updateAudit);
                        }

                        break;
                    }
                }
            }

            foreach (var log in pendingAudits)
            {
                AuditLogs.Add(log);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Audit logging preparation failed; changes were not persisted.", ex);
        }
    }

    private void ApplyCreatedAtForNewAuditableEntities(DateTime utcNow)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }
    }

    private void ConvertHardDeletesToSoftDeletes(
        DateTime utcNow,
        string actorUserId,
        HashSet<object> softDeletedViaRemove,
        List<AuditLog> pendingAudits)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Deleted).ToList())
        {
            var entity = entry.Entity;
            var oldSnapshot = EntityAuditSerializer.SerializeSnapshot(entry, useOriginalValues: false);
            entry.State = EntityState.Modified;
            entity.IsDeleted = true;
            entity.DeletedAt = utcNow;
            softDeletedViaRemove.Add(entity);

            pendingAudits.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorUserId,
                Action = "DELETE",
                EntityName = entity.GetType().Name,
                EntityId = GetPrimaryKeyString(entry),
                OldValues = oldSnapshot,
                NewValues = EntityAuditSerializer.SerializeSoftDeleteOutcome(utcNow),
                Timestamp = utcNow
            });
        }
    }

    private static AuditLog CreateInsertAudit(EntityEntry entry, string actorUserId, DateTime utcNow)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorUserId,
            Action = "CREATE",
            EntityName = entry.Entity.GetType().Name,
            EntityId = GetPrimaryKeyString(entry),
            OldValues = null,
            NewValues = EntityAuditSerializer.SerializeSnapshot(entry, useOriginalValues: false),
            Timestamp = utcNow
        };
    }

    private static AuditLog? TryCreateUpdateAudit(EntityEntry entry, string actorUserId, DateTime utcNow)
    {
        var (oldJson, newJson) = EntityAuditSerializer.SerializeModifiedChanges(entry);
        if (oldJson is null || newJson is null)
        {
            return null;
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorUserId,
            Action = "UPDATE",
            EntityName = entry.Entity.GetType().Name,
            EntityId = GetPrimaryKeyString(entry),
            OldValues = oldJson,
            NewValues = newJson,
            Timestamp = utcNow
        };
    }

    private static string GetPrimaryKeyString(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
        if (pk is null)
        {
            return string.Empty;
        }

        var prop = entry.Property(pk.Name);
        return prop.CurrentValue?.ToString() ?? string.Empty;
    }
}
