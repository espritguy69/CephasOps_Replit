using CephasOps.Domain.Files.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Files;

/// <summary>SaaS storage lifecycle: periodically updates File.StorageTier based on LastAccessedAtUtc/CreatedAt. Runs per-tenant via TenantScopeExecutor.</summary>
public class StorageLifecycleService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StorageLifecycleService> _logger;
    private readonly StorageLifecycleOptions _options;

    public StorageLifecycleService(
        IServiceProvider serviceProvider,
        ILogger<StorageLifecycleService> logger,
        IOptions<StorageLifecycleOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new StorageLifecycleOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Storage lifecycle service is disabled");
            return;
        }

        _logger.LogInformation("Storage lifecycle service started. Interval={Interval}", _options.Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunLifecycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storage lifecycle run failed");
            }

            await Task.Delay(_options.Interval, stoppingToken);
        }

        _logger.LogInformation("Storage lifecycle service stopped");
    }

    private async Task RunLifecycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var companyIds = await context.Files
            .Where(f => f.CompanyId != null)
            .Select(f => f.CompanyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var warmCutoff = DateTime.UtcNow.AddDays(-_options.WarmAfterDays);
        var coldCutoff = DateTime.UtcNow.AddDays(-_options.ColdAfterDays);
        var archiveCutoff = DateTime.UtcNow.AddDays(-_options.ArchiveAfterDays);
        var totalUpdated = 0;

        foreach (var companyId in companyIds)
        {
            try
            {
                var updated = await TenantScopeExecutor.RunWithTenantScopeAsync(companyId, async ct =>
                {
                    var toWarm = await context.Files
                        .Where(f => f.CompanyId == companyId && f.StorageTier == "Hot"
                            && _options.WarmAfterDays > 0
                            && (f.LastAccessedAtUtc ?? f.CreatedAt) < warmCutoff)
                        .Take(_options.MaxFilesPerTenantPerRun)
                        .ToListAsync(ct);
                    var toCold = await context.Files
                        .Where(f => f.CompanyId == companyId && f.StorageTier == "Warm"
                            && _options.ColdAfterDays > 0
                            && (f.LastAccessedAtUtc ?? f.CreatedAt) < coldCutoff)
                        .Take(_options.MaxFilesPerTenantPerRun)
                        .ToListAsync(ct);
                    var toArchive = await context.Files
                        .Where(f => f.CompanyId == companyId && (f.StorageTier == "Cold" || f.StorageTier == "Warm")
                            && _options.ArchiveAfterDays > 0
                            && (f.LastAccessedAtUtc ?? f.CreatedAt) < archiveCutoff)
                        .Take(_options.MaxFilesPerTenantPerRun)
                        .ToListAsync(ct);

                    foreach (var f in toWarm) f.StorageTier = "Warm";
                    foreach (var f in toCold) f.StorageTier = "Cold";
                    foreach (var f in toArchive) f.StorageTier = "Archive";

                    var count = toWarm.Count + toCold.Count + toArchive.Count;
                    if (count > 0)
                        await context.SaveChangesAsync(ct);
                    return count;
                }, cancellationToken);

                totalUpdated += updated;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Storage lifecycle failed for CompanyId={CompanyId}", companyId);
            }
        }

        if (totalUpdated > 0)
            _logger.LogInformation("Storage lifecycle updated {Count} file(s) across {Tenants} tenant(s)", totalUpdated, companyIds.Count);
    }
}
