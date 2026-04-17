using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class CreateBookingRequest
{
    [JsonPropertyName("slot_id")]
    public long? SlotId { get; set; }

    [JsonPropertyName("slotId")]
    public long? SlotIdCamel { get; set; }

    [JsonPropertyName("patient_name")]
    public string? PatientName { get; set; }

    [JsonPropertyName("patientName")]
    public string? PatientNameCamel { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumberCamel { get; set; }

    public long ResolveSlotId() => SlotId ?? SlotIdCamel ?? 0;

    public string? ResolvePatientName()
    {
        var a = PatientName?.Trim();
        var b = PatientNameCamel?.Trim();
        if (!string.IsNullOrEmpty(a))
        {
            return a;
        }

        return string.IsNullOrEmpty(b) ? null : b;
    }

    public string? ResolvePhoneNumber()
    {
        var a = PhoneNumber?.Trim();
        var b = PhoneNumberCamel?.Trim();
        if (!string.IsNullOrEmpty(a))
        {
            return a;
        }

        return string.IsNullOrEmpty(b) ? null : b;
    }
}
