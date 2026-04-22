using api.Application.Notifications;
using api.Application.Services;
using api.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(INotificationSender notificationSender) : ControllerBase
{
    /// <summary>Manual test hook for the mock/real notification sender. Authenticated only.</summary>
    [Authorize]
    [HttpPost("test-send")]
    [ProducesResponseType(typeof(TestSendNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TestSendNotificationResponse>> TestSend([FromBody] TestSendNotificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var sent = request.Channel switch
        {
            NotificationChannel.Sms => await notificationSender.SendSmsAsync(request.PhoneNumber ?? string.Empty, request.Message),
            NotificationChannel.Email => await notificationSender.SendEmailAsync(request.EmailAddress ?? string.Empty, request.Message),
            _ => false
        };

        var message = sent
            ? "Test notification sent successfully."
            : "The notification sender reported failure (see server logs for mock sender).";

        return Ok(new TestSendNotificationResponse { Message = message, SenderAccepted = sent });
    }
}
