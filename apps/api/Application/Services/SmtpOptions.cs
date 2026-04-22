namespace api.Application.Services;

/// <summary>
/// Binds the <c>Smtp</c> config section. For Gmail, use <c>smtp.gmail.com</c> port <c>587</c> and an
/// <see href="https://support.google.com/accounts/answer/185833">app password</see> (2FA on the account).
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; set; }

    /// <summary>SMTP host (Gmail: smtp.gmail.com).</summary>
    public string Host { get; set; } = "smtp.gmail.com";

    /// <summary>587 for Gmail (submission + StartTLS).</summary>
    public int Port { get; set; } = 587;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string? FromDisplayName { get; set; }

    public string DefaultSubject { get; set; } = "Clinicrest notification";
}
