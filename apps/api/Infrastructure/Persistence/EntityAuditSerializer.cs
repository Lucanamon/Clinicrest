using System.Text.Json;
using System.Text.Json.Serialization;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace api.Infrastructure.Persistence;

internal static class EntityAuditSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    internal static string SerializeSoftDeleteOutcome(DateTime deletedAtUtc)
    {
        var dict = new Dictionary<string, object?>
        {
            [nameof(BaseEntity.IsDeleted)] = true,
            [nameof(BaseEntity.DeletedAt)] = NormalizeForJson(deletedAtUtc)
        };
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    internal static string SerializeSnapshot(EntityEntry entry, bool useOriginalValues)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in entry.Properties)
        {
            var name = prop.Metadata.Name;
            if (entry.Entity is User && name == nameof(User.PasswordHash))
            {
                continue;
            }

            var value = useOriginalValues ? prop.OriginalValue : prop.CurrentValue;
            dict[name] = NormalizeForJson(value);
        }

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    internal static (string? OldJson, string? NewJson) SerializeModifiedChanges(EntityEntry entry)
    {
        var oldDict = new Dictionary<string, object?>(StringComparer.Ordinal);
        var newDict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in entry.Properties.Where(p => p.IsModified))
        {
            var name = prop.Metadata.Name;
            if (entry.Entity is User && name == nameof(User.PasswordHash))
            {
                continue;
            }

            oldDict[name] = NormalizeForJson(prop.OriginalValue);
            newDict[name] = NormalizeForJson(prop.CurrentValue);
        }

        if (oldDict.Count == 0)
        {
            return (null, null);
        }

        return (JsonSerializer.Serialize(oldDict, JsonOptions), JsonSerializer.Serialize(newDict, JsonOptions));
    }

    private static object? NormalizeForJson(object? value)
    {
        return value switch
        {
            DateTime dt => dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : dt.ToUniversalTime(),
            DateTimeOffset dto => dto.ToUniversalTime(),
            _ => value
        };
    }
}
