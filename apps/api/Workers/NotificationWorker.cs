using api.Application.Services;
using api.Domain.Entities;
using api.Hubs;
using api.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api.Workers;

public class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private readonly ILogger<NotificationWorker> _logger = logger;

    private const string DefaultReminderMessage = "Reminder: your appointment is coming soon";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

                var now = DateTime.UtcNow;
                var dueJobs = await db.NotificationJobs
                    .Where(j =>
                        (j.Status == NotificationStatus.Pending || j.Status == NotificationStatus.Retrying) &&
                        j.NextAttemptAt <= now)
                    .ToListAsync(stoppingToken);

                foreach (var job in dueJobs)
                {
                    _logger.LogInformation(
                        "Picked notification job {JobId} (channel {Channel}, booking {BookingId}, retry {RetryCount}).",
                        job.Id,
                        job.Channel,
                        job.BookingId,
                        job.RetryCount);

                    var text = string.IsNullOrWhiteSpace(job.Message) ? DefaultReminderMessage : job.Message!;

                    var ok = job.Channel == NotificationChannel.Sms
                        ? await sender.SendSmsAsync(job.PhoneNumber, text)
                        : await sender.SendEmailAsync(job.EmailAddress ?? string.Empty, text);

                    if (ok)
                    {
                        var sentAt = DateTime.UtcNow;
                        job.Status = NotificationStatus.Sent;
                        job.SentAt = sentAt;
                        job.ErrorMessage = null;
                        _logger.LogInformation(
                            "Notification job {JobId} sent successfully at {SentAt} (channel {Channel}, booking {BookingId}).",
                            job.Id,
                            sentAt,
                            job.Channel,
                            job.BookingId);
                        try
                        {
                            await _hubContext.Clients.All.SendAsync(
                                "ReceiveSystemAlert",
                                $"✅ Reminder sent to {job.PatientName}",
                                cancellationToken: stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to broadcast notification completion for job {JobId}.", job.Id);
                        }
                    }
                    else
                    {
                        ApplyFailedAttempt(job, now);
                    }
                }

                if (dueJobs.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error while processing notification jobs.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void ApplyFailedAttempt(NotificationJob job, DateTime utcNow)
    {
        job.RetryCount++;
        if (job.RetryCount > 3)
        {
            job.Status = NotificationStatus.Failed;
            job.ErrorMessage = "Notification delivery failed after 3 retries.";
            _logger.LogWarning(
                "Notification job {JobId} (channel {Channel}, booking {BookingId}) failed permanently after {RetryCount} failed attempts (max 3 retries).",
                job.Id,
                job.Channel,
                job.BookingId,
                job.RetryCount);
            return;
        }

        job.NextAttemptAt = job.RetryCount switch
        {
            1 => utcNow.AddMinutes(1),
            2 => utcNow.AddMinutes(5),
            3 => utcNow.AddMinutes(15),
            _ => utcNow
        };
        job.Status = NotificationStatus.Retrying;
        _logger.LogInformation(
            "Scheduled retry for notification job {JobId} (channel {Channel}): next attempt at {NextAttempt} (retry count {RetryCount}).",
            job.Id,
            job.Channel,
            job.NextAttemptAt,
            job.RetryCount);
    }
}
