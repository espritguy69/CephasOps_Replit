using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Notifications;

/// <summary>
/// Runs notification retention on a schedule (Phase 7). Replaces legacy notificationretention BackgroundJob trigger.
/// </summary>
public class NotificationRetentionHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationRetentionHostedService> _logger;
    private readonly NotificationRetentionOptions _options;

    public NotificationRetentionHostedService(
        IServiceProvider serviceProvider,
        ILogger<NotificationRetentionHostedService> logger,
        IOptions<NotificationRetentionOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new NotificationRetentionOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, _options.IntervalHours));
        _logger.LogInformation("Notification retention hosted service started. IntervalHours={IntervalHours}, ArchiveDays={ArchiveDays}, DeleteDays={DeleteDays}",
            _options.IntervalHours, _options.ArchiveDays, _options.DeleteDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;

                using var scope = _serviceProvider.CreateScope();
                var retention = scope.ServiceProvider.GetService<INotificationRetentionService>();
                if (retention == null)
                {
                    _logger.LogDebug("INotificationRetentionService not registered; skipping run");
                    continue;
                }

                var result = await retention.RunRetentionAsync(
                    _options.ArchiveDays,
                    _options.DeleteDays,
                    companyId: null,
                    stoppingToken);

                _logger.LogInformation("Notification retention run: archived={Archived}, deleted={Deleted}", result.ArchivedCount, result.DeletedCount);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification retention run failed");
            }
        }

        _logger.LogInformation("Notification retention hosted service stopped");
    }
}

/// <summary>Options for NotificationRetentionHostedService.</summary>
public class NotificationRetentionOptions
{
    public const string SectionName = "Notifications:Retention";

    /// <summary>Hours between retention runs (default 24).</summary>
    public int IntervalHours { get; set; } = 24;
    /// <summary>Archive Read/Unread older than this many days (default 90).</summary>
    public int ArchiveDays { get; set; } = 90;
    /// <summary>Hard-delete Archived older than this many days (default 365).</summary>
    public int DeleteDays { get; set; } = 365;
}
