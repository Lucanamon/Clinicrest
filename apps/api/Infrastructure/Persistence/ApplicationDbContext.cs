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

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("citext");

            entity.Property(p => p.Age)
                .IsRequired();

            entity.Property(p => p.Phone)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(p => p.Underlying)
                .HasMaxLength(1000);

            entity.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(p => p.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(p => p.Name)
                .HasDatabaseName("ix_patients_name_unique_ci")
                .IsUnique();
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
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
