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

        if (string.IsNullOrWhiteSpace(o.Username) || string.IsNullOrWhiteSpace(o.Password))
        {
            _logger.LogError(
                "SMTP: credentials missing. Set Email__Smtp__Username and Email__Smtp__Password environment variables.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(o.FromEmail) || string.IsNullOrWhiteSpace(o.Host))
        {
            _logger.LogError("SMTP: Enabled is true but Email:Smtp:Host or Email:Smtp:FromEmail is missing.");
            return false;
        }

        try
        {
            var mime = new MimeMessage();
            var from = string.IsNullOrWhiteSpace(o.FromName)
                ? new MailboxAddress(o.FromEmail, o.FromEmail)
                : new MailboxAddress(o.FromName, o.FromEmail);
            mime.From.Add(from);
            mime.To.Add(MailboxAddress.Parse(email.Trim()));
            mime.Subject = o.DefaultSubject;
            mime.Body = new TextPart("plain") { Text = message };

            var socketOptions = o.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            _logger.LogDebug("SMTP: connecting to {Host}:{Port}. SSL enabled: {EnableSsl}", o.Host, o.Port, o.EnableSsl);
            using var client = new SmtpClient();
            await client.ConnectAsync(o.Host, o.Port, socketOptions);
            await client.AuthenticateAsync(o.Username, o.Password);
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
