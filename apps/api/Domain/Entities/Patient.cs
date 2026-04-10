using System.ComponentModel.DataAnnotations.Schema;

namespace api.Domain.Entities;

public class Patient : AuditableEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateTime DateOfBirth { get; set; }

    public required string Gender { get; set; }

    public required string PhoneNumber { get; set; }

    public string? UnderlyingDisease { get; set; }

    [NotMapped]
    public int Age => CalculateAge(DateOfBirth);

    private static int CalculateAge(DateTime dob)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dob.Year;

        if (dob.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
