namespace api.Application.Services;

public interface INotificationSender
{
    Task<bool> SendSmsAsync(string phone, string message);

    Task<bool> SendEmailAsync(string email, string message);
}
