namespace api.Application.Services;

/// <summary>
/// Binds the <c>Email:Smtp</c> section via <see cref="SectionName"/>.
/// Credentials are supplied with environment variables <c>Email__Smtp__Username</c> and <c>Email__Smtp__Password</c>
/// (do not commit them; keep them out of appsettings). For Gmail, use <c>smtp.gmail.com</c> port <c>587</c> and an
/// <see href="https://support.google.com/accounts/answer/185833">app password</see> (2FA on the account).
/// </summary>
public class SmtpOptions
{
    /// <summary>Configuration path <c>Email:Smtp</c> (environment prefix <c>Email__Smtp__</c>).</summary>
    public const string SectionName = "Email:Smtp";

    public bool Enabled { get; set; }

    /// <summary>SMTP host (Gmail: smtp.gmail.com).</summary>
    public string Host { get; set; } = "smtp.gmail.com";

    /// <summary>587 for Gmail (submission + StartTLS).</summary>
    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string DefaultSubject { get; set; } = "Clinicrest notification";
}
