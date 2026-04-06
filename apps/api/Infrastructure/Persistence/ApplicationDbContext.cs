using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Backlog> Backlogs => Set<Backlog>();

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
                .HasMaxLength(16);

            entity.HasIndex(u => u.Username)
                .HasDatabaseName("ix_users_username_unique")
                .IsUnique();
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
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Patient>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Appointment>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Backlog>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
