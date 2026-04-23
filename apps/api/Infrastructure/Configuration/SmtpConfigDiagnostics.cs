using api.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace api.Infrastructure.Configuration;

/// <summary>
/// One-time visibility into whether <see cref="SmtpOptions"/> values are present after configuration
/// (appsettings + environment variables, etc.). Remove or downgrade to Debug if noisy.
/// </summary>
public static class SmtpConfigDiagnostics
{
    public static void LogSmtpConfigSnapshot(IConfiguration configuration, ILogger logger)
    {
        var section = configuration.GetSection(SmtpOptions.SectionName);
        var enabled = section.GetValue("Enabled", false);
        var hostOk = !string.IsNullOrWhiteSpace(section["Host"]);
        var userOk = !string.IsNullOrWhiteSpace(section["Username"]);
        var passOk = !string.IsNullOrWhiteSpace(section["Password"]);

        logger.LogInformation(
            "[SMTP config] Email:Smtp:Enabled={Enabled}, host present={HostPresent}, username present={UserPresent}, password present={PasswordPresent}",
            enabled,
            hostOk,
            userOk,
            passOk);

        if (enabled && (!userOk || !passOk))
        {
            logger.LogWarning(
                "[SMTP config] Email:Smtp:Enabled is true but username or password is not configured. " +
                "Set environment variables Email__Smtp__Username and Email__Smtp__Password. " +
                "They bind to configuration keys Email:Smtp:Username and Email:Smtp:Password. " +
                "A .env file next to the repo is not read by 'dotnet run' unless you load it into the process environment; Docker Compose can inject the same variable names from .env.");
        }
    }
}
