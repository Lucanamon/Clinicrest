namespace api.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public int Age { get; set; }

    public required string Phone { get; set; }

    public string? Underlying { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
