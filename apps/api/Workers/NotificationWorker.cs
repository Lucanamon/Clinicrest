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

    private const string ReminderMessage = "Reminder: You have an appointment coming up.";

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
                        j.ScheduledSendTime <= now)
                    .ToListAsync(stoppingToken);

                foreach (var job in dueJobs)
                {
                    var ok = job.Channel == NotificationChannel.Sms
                        ? await sender.SendSmsAsync(job.PhoneNumber, ReminderMessage)
                        : await sender.SendEmailAsync(job.PhoneNumber, ReminderMessage);

                    if (ok)
                    {
                        job.Status = NotificationStatus.Sent;
                        job.ErrorMessage = null;
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
                        job.RetryCount++;
                        if (job.RetryCount < 3)
                        {
                            job.Status = NotificationStatus.Retrying;
                        }
                        else
                        {
                            job.Status = NotificationStatus.Failed;
                            job.ErrorMessage = "Mock sender failed 3 times";
                        }
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
}
