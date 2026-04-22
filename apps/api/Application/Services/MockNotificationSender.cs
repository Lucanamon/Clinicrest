namespace api.Application.Services;

public class MockNotificationSender : INotificationSender
{
    private readonly ILogger<MockNotificationSender> _logger;

    public MockNotificationSender(ILogger<MockNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendSmsAsync(string phone, string message)
    {
        _logger.LogInformation("MOCK SENDER: Sending SMS to {Contact} - {Message}", phone, message);
        return Task.FromResult(Random.Shared.Next(0, 10) < 9);
    }

    public Task<bool> SendEmailAsync(string email, string message)
    {
        _logger.LogInformation("MOCK SENDER: Sending Email to {Contact} - {Message}", email, message);
        return Task.FromResult(Random.Shared.Next(0, 10) < 9);
    }
}
