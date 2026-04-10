namespace api.Domain.Entities;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
}
