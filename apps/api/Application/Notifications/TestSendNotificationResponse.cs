namespace api.Application.Notifications;

public sealed class TestSendNotificationResponse
{
    public required string Message { get; init; }
    public bool SenderAccepted { get; init; }
}
