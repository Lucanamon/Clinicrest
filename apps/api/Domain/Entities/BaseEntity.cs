namespace api.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }
}
