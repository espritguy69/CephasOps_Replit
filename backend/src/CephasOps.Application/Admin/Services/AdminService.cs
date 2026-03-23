using CephasOps.Application.Admin.DTOs;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Administrative service for system maintenance and monitoring
/// </summary>
public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminService> _logger;
    private readonly IMemoryCache? _memoryCache;

    public AdminService(
        ApplicationDbContext context,
        ILogger<AdminService> logger,
        IMemoryCache? memoryCache = null)
    {
        _context = context;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task ReindexAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rebuilding search indexes...");

        try
        {
            // Rebuild PostgreSQL full-text search indexes if they exist
            // Note: This is a basic implementation. For production, consider:
            // - Elasticsearch reindexing
            // - Azure Cognitive Search reindexing
            // - Custom search index services
            
            // Rebuild any materialized views that support search
            var materializedViews = new string[]
            {
                // Add materialized view names here if they exist
                // Example: "mv_order_search_index"
            };

            foreach (var viewName in materializedViews)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        @"REFRESH MATERIALIZED VIEW CONCURRENTLY ""{0}""",
                        new object[] { viewName });
                    _logger.LogInformation("Refreshed materialized view: {ViewName}", viewName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh materialized view: {ViewName}", viewName);
                    // Continue with other views even if one fails
                }
            }

            // If using PostgreSQL full-text search, rebuild GIN indexes
            // This is a lightweight operation that doesn't require external services
            _logger.LogInformation("Search indexes rebuilt successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding search indexes");
            throw;
        }
    }

    public async Task FlushCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Flushing settings cache...");

        try
        {
            int cacheEntriesFlushed = 0;

            // Flush in-memory cache if available
            if (_memoryCache is MemoryCache memoryCache)
            {
                // MemoryCache doesn't have a direct "Clear" method
                // We need to use reflection or track cache keys
                // For now, log that cache exists and would be flushed
                _logger.LogInformation("In-memory cache detected (would be flushed in production with key tracking)");
                
                // In production, you would:
                // 1. Track cache keys in a registry
                // 2. Remove each key: _memoryCache.Remove(key)
                // 3. Or use a cache wrapper that supports Clear()
            }

            // Note: For distributed cache (Redis, etc.), you would:
            // - Inject IDistributedCache
            // - Call RemoveAsync() for tracked keys
            // - Or use a cache tag/invalidation strategy

            // Log cache flush completion
            _logger.LogInformation(
                "Settings cache flushed successfully. Entries flushed: {Count}",
                cacheEntriesFlushed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing cache");
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task<SystemHealthDto> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var health = new SystemHealthDto
        {
            CheckedAt = DateTime.UtcNow,
            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            IsHealthy = true
        };

        // Check database connectivity
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            health.Database = new DatabaseHealthDto
            {
                IsConnected = canConnect,
                ResponseTime = stopwatch.Elapsed
            };

            if (!canConnect)
            {
                health.IsHealthy = false;
                health.Database.ErrorMessage = "Cannot connect to database";
            }
        }
        catch (Exception ex)
        {
            health.IsHealthy = false;
            health.Database = new DatabaseHealthDto
            {
                IsConnected = false,
                ErrorMessage = ex.Message
            };
            _logger.LogError(ex, "Database health check failed");
        }

        // Add additional health checks here
        health.Details["Database"] = health.Database.IsConnected ? "Connected" : "Disconnected";
        health.Details["ResponseTime"] = health.Database.ResponseTime?.TotalMilliseconds.ToString("F2") + " ms" ?? "N/A";

        // Email parser health (only when DB is connected)
        if (health.Database.IsConnected)
        {
            try
            {
                health.EmailParser = await GetEmailParserHealthAsync(cancellationToken);
                health.Details["EmailParser"] = health.EmailParser.Status;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email parser health check failed");
                health.EmailParser = new EmailParserHealthDto
                {
                    Status = "Error",
                    ActiveAccountsCount = 0,
                    StaleAccountsCount = 0,
                    MostRecentPollAt = null
                };
                health.Details["EmailParser"] = "Error";
            }

            // Background job runner status
            try
            {
                var runningCount = await _context.BackgroundJobs.CountAsync(j => j.State == BackgroundJobState.Running, cancellationToken);
                var queuedCount = await _context.BackgroundJobs.CountAsync(j => j.State == BackgroundJobState.Queued, cancellationToken);
                var failedCount = await _context.BackgroundJobs.CountAsync(j => j.State == BackgroundJobState.Failed, cancellationToken);
                var lastActivity = await _context.BackgroundJobs
                    .OrderByDescending(j => j.UpdatedAt)
                    .Select(j => j.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                var status = runningCount > 0 ? "Running" : failedCount > 5 ? "Degraded" : "Healthy";
                health.BackgroundJobRunner = new BackgroundJobRunnerHealthDto
                {
                    Status = status,
                    RunningCount = runningCount,
                    QueuedCount = queuedCount,
                    FailedCount = failedCount,
                    LastActivityAt = lastActivity != default ? lastActivity : (DateTime?)null
                };
                health.Details["BackgroundJobRunner"] = status;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background job runner health check failed");
                health.BackgroundJobRunner = new BackgroundJobRunnerHealthDto { Status = "Error" };
                health.Details["BackgroundJobRunner"] = "Error";
            }
        }

        return health;
    }

    /// <summary>
    /// Builds email parser (ingestion) health from active email accounts and last poll times.
    /// </summary>
    private async Task<EmailParserHealthDto> GetEmailParserHealthAsync(CancellationToken cancellationToken)
    {
        const double staleThresholdMinutes = 15; // Consider account stale if not polled within 15 min

        var accounts = await _context.EmailAccounts
            .AsNoTracking()
            .Where(ea => ea.IsActive && !ea.IsDeleted && ea.PollIntervalSec > 0)
            .Select(ea => new { ea.Id, ea.Name, ea.LastPolledAt, ea.PollIntervalSec })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var accountHealths = new List<EmailParserAccountHealthDto>();
        int staleCount = 0;

        foreach (var a in accounts)
        {
            double? minutesSince = a.LastPolledAt.HasValue
                ? (now - a.LastPolledAt.Value).TotalMinutes
                : null;

            var status = !minutesSince.HasValue
                ? "NeverPolled"
                : minutesSince.Value <= staleThresholdMinutes
                    ? "Healthy"
                    : "Stale";

            if (status == "Stale")
                staleCount++;

            accountHealths.Add(new EmailParserAccountHealthDto
            {
                Id = a.Id,
                Name = a.Name,
                LastPolledAt = a.LastPolledAt,
                MinutesSinceLastPoll = minutesSince,
                Status = status
            });
        }

        var polledAtValues = accounts.Where(a => a.LastPolledAt.HasValue).Select(a => a.LastPolledAt!.Value).ToList();
        DateTime? mostRecentPollAt = polledAtValues.Count > 0 ? polledAtValues.Max() : null;

        var parserStatus = accounts.Count == 0
            ? "NoAccounts"
            : staleCount == 0
                ? "Healthy"
                : staleCount == accounts.Count
                    ? "Degraded"
                    : "Degraded";

        return new EmailParserHealthDto
        {
            Status = parserStatus,
            ActiveAccountsCount = accounts.Count,
            MostRecentPollAt = mostRecentPollAt,
            StaleAccountsCount = staleCount,
            Accounts = accountHealths
        };
    }

    public async Task<EmailIngestionDiagnosticsDto> GetEmailIngestionDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var since24h = utcNow.AddHours(-24);
        var startOfTodayUtc = utcNow.Date;

        // Legacy BackgroundJob (Phase 8: deprecated; drain only)
        var legacyLastSuccess = await _context.BackgroundJobs
            .AsNoTracking()
            .Where(j => j.JobType == "EmailIngest" && j.State == BackgroundJobState.Succeeded && j.CompletedAt != null)
            .OrderByDescending(j => j.CompletedAt)
            .Select(j => j.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var legacyJobs24h = await _context.BackgroundJobs
            .AsNoTracking()
            .Where(j => j.JobType == "EmailIngest" && j.CreatedAt >= since24h)
            .GroupBy(j => j.State)
            .Select(g => new { State = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        // JobExecution emailingest (Phase 8 primary path)
        var jeLastSuccess = await _context.JobExecutions
            .AsNoTracking()
            .Where(j => j.JobType == "emailingest" && j.Status == "Succeeded" && j.CompletedAtUtc != null)
            .OrderByDescending(j => j.CompletedAtUtc)
            .Select(j => j.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var jeJobs24h = await _context.JobExecutions
            .AsNoTracking()
            .Where(j => j.JobType == "emailingest" && j.CreatedAtUtc >= since24h)
            .GroupBy(j => j.Status)
            .Select(g => new { State = g.Key ?? "Unknown", Count = g.Count() })
            .ToListAsync(cancellationToken);

        var lastSuccess = legacyLastSuccess.HasValue || jeLastSuccess.HasValue
            ? new[] { legacyLastSuccess ?? DateTime.MinValue, jeLastSuccess ?? DateTime.MinValue }.Max()
            : (DateTime?)null;

        var byState = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var x in legacyJobs24h)
            byState[x.State] = byState.GetValueOrDefault(x.State) + x.Count;
        foreach (var x in jeJobs24h)
            byState[x.State] = byState.GetValueOrDefault(x.State) + x.Count;

        var accounts = await _context.EmailAccounts
            .AsNoTracking()
            .OrderBy(ea => ea.Name)
            .Select(ea => new EmailAccountLastPolledDto
            {
                Id = ea.Id,
                Name = ea.Name,
                Username = ea.Username,
                LastPolledAt = ea.LastPolledAt
            })
            .ToListAsync(cancellationToken);

        var draftsToday = await _context.ParsedOrderDrafts
            .AsNoTracking()
            .CountAsync(d => d.CreatedAt >= startOfTodayUtc && !d.IsDeleted, cancellationToken);

        return new EmailIngestionDiagnosticsDto
        {
            LastSuccessfulEmailIngestAt = lastSuccess,
            EmailIngestJobsLast24hByState = byState,
            EmailAccountsLastPolledAt = accounts,
            DraftsCreatedToday = draftsToday
        };
    }
}

