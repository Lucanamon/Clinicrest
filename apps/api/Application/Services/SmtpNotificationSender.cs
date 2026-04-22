using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace api.Application.Services;

public class SmtpNotificationSender : INotificationSender
{
    private readonly IOptions<SmtpOptions> _options;
    private readonly ILogger<SmtpNotificationSender> _logger;

    public SmtpNotificationSender(IOptions<SmtpOptions> options, ILogger<SmtpNotificationSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<bool> SendSmsAsync(string phone, string message)
    {
        _logger.LogInformation("MOCK SENDER: Sending SMS to {Contact} - {Message}", phone, message);
        return Task.FromResult(Random.Shared.Next(0, 10) < 9);
    }

    public async Task<bool> SendEmailAsync(string email, string message)
    {
        var o = _options.Value;

        if (!o.Enabled)
        {
            _logger.LogInformation("SMTP disabled - not sending to {Email}.", email);
            return false;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("SMTP: send skipped; empty email or message.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(o.User) || string.IsNullOrWhiteSpace(o.Password)
            || string.IsNullOrWhiteSpace(o.FromAddress) || string.IsNullOrWhiteSpace(o.Host))
        {
            _logger.LogError("SMTP: Enabled is true but Smtp:User, Password, FromAddress, or Host is missing.");
            return false;
        }

        try
        {
            var mime = new MimeMessage();
            var from = string.IsNullOrWhiteSpace(o.FromDisplayName)
                ? new MailboxAddress(o.FromAddress, o.FromAddress)
                : new MailboxAddress(o.FromDisplayName, o.FromAddress);
            mime.From.Add(from);
            mime.To.Add(MailboxAddress.Parse(email.Trim()));
            mime.Subject = o.DefaultSubject;
            mime.Body = new TextPart("plain") { Text = message };

            _logger.LogDebug("SMTP: connecting to {Host}:{Port} (StartTLS).", o.Host, o.Port);
            using var client = new SmtpClient();
            await client.ConnectAsync(o.Host, o.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(o.User, o.Password);
            await client.SendAsync(mime);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "SMTP: message sent to {To} (subject: {Subject}, body length: {Length}).",
                email,
                o.DefaultSubject,
                message.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP: failed to send to {Email}", email);
            return false;
        }
    }
}
