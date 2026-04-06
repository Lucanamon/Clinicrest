namespace api.Domain;

public static class Roles
{
    public const string RootAdmin = "RootAdmin";

    public const string Doctor = "Doctor";

    public const string Nurse = "Nurse";

    public const string Administrator = "Administrator";

    /// <summary>Roles allowed to use the API for clinical features (JWT must match).</summary>
    public const string ClinicalAll =
        RootAdmin + "," + Doctor + "," + Nurse + "," + Administrator;

    /// <summary>RootAdmin, Nurse, and Administrator may assign a doctor when scheduling appointments.</summary>
    public static bool CanAssignAppointmentDoctor(string role) =>
        string.Equals(role, RootAdmin, StringComparison.Ordinal) ||
        string.Equals(role, Nurse, StringComparison.Ordinal) ||
        string.Equals(role, Administrator, StringComparison.Ordinal);

    public static bool IsDoctor(string role) =>
        string.Equals(role, Doctor, StringComparison.Ordinal);

    public static bool IsRootAdmin(string role) =>
        string.Equals(role, RootAdmin, StringComparison.Ordinal);
}
