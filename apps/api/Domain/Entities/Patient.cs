namespace api.Domain.Entities;

public class Patient : AuditableEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateTime DateOfBirth { get; set; }

    public required string Gender { get; set; }

    public required string PhoneNumber { get; set; }
}
