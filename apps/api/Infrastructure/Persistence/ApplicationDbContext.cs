using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<User> Users => Set<User>();

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
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(16);

            entity.HasIndex(u => u.Username)
                .HasDatabaseName("ix_users_username_unique")
                .IsUnique();
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

        return base.SaveChangesAsync(cancellationToken);
    }
}
