using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Workflow.JobObservability;

/// <summary>
/// Resolves job definitions from DB with fallback to built-in defaults for known types.
/// </summary>
public class JobDefinitionProvider : IJobDefinitionProvider
{
    private readonly ApplicationDbContext _context;

    /// <summary>Built-in defaults when no row exists in JobDefinitions.</summary>
    private static readonly IReadOnlyDictionary<string, JobDefinitionDto> Defaults = new Dictionary<string, JobDefinitionDto>(StringComparer.OrdinalIgnoreCase)
    {
        ["EmailIngest"] = new JobDefinitionDto { JobType = "EmailIngest", DisplayName = "Email Ingest", RetryAllowed = true, MaxRetries = 3, DefaultStuckThresholdSeconds = 600 },
        ["pnlrebuild"] = new JobDefinitionDto { JobType = "pnlrebuild", DisplayName = "P&L Rebuild", RetryAllowed = true, MaxRetries = 3, DefaultStuckThresholdSeconds = 7200 },
        ["reconcileledgerbalancecache"] = new JobDefinitionDto { JobType = "reconcileledgerbalancecache", DisplayName = "Reconcile Ledger Balance Cache", RetryAllowed = true, MaxRetries = 3, DefaultStuckThresholdSeconds = 7200 },
        ["populatestockbylocationsnapshots"] = new JobDefinitionDto { JobType = "populatestockbylocationsnapshots", DisplayName = "Populate Stock by Location Snapshots", RetryAllowed = true, MaxRetries = 3, DefaultStuckThresholdSeconds = 7200 },
        ["inventoryreportexport"] = new JobDefinitionDto { JobType = "inventoryreportexport", DisplayName = "Inventory Report Export", RetryAllowed = true, MaxRetries = 3, DefaultStuckThresholdSeconds = 3600 },
        ["notificationsend"] = new JobDefinitionDto { JobType = "notificationsend", DisplayName = "Notification Send", RetryAllowed = true, MaxRetries = 3, DefaultStuckThresholdSeconds = 300 },
        ["notificationretention"] = new JobDefinitionDto { JobType = "notificationretention", DisplayName = "Notification Retention", RetryAllowed = true, MaxRetries = 3, DefaultStuckThresholdSeconds = 3600 },
        ["documentgeneration"] = new JobDefinitionDto { JobType = "documentgeneration", DisplayName = "Document Generation", RetryAllowed = false, MaxRetries = 3, DefaultStuckThresholdSeconds = 600 },
        ["myinvoisstatuspoll"] = new JobDefinitionDto { JobType = "myinvoisstatuspoll", DisplayName = "MyInvois Status Poll", RetryAllowed = false, MaxRetries = 3, DefaultStuckThresholdSeconds = 300 },
        ["PayoutAnomalyAlert"] = new JobDefinitionDto { JobType = "PayoutAnomalyAlert", DisplayName = "Payout Anomaly Alert", RetryAllowed = false, MaxRetries = 0, DefaultStuckThresholdSeconds = 3600 },
        ["MissingPayoutSnapshotRepair"] = new JobDefinitionDto { JobType = "MissingPayoutSnapshotRepair", DisplayName = "Missing Payout Snapshot Repair", RetryAllowed = false, MaxRetries = 0, DefaultStuckThresholdSeconds = 7200 },
        ["EmailIngestionScheduler"] = new JobDefinitionDto { JobType = "EmailIngestionScheduler", DisplayName = "Email Ingestion Scheduler", RetryAllowed = false, MaxRetries = 0, DefaultStuckThresholdSeconds = 120 },
        ["StockSnapshotScheduler"] = new JobDefinitionDto { JobType = "StockSnapshotScheduler", DisplayName = "Stock Snapshot Scheduler", RetryAllowed = false, MaxRetries = 0, DefaultStuckThresholdSeconds = 300 },
        ["LedgerReconciliationScheduler"] = new JobDefinitionDto { JobType = "LedgerReconciliationScheduler", DisplayName = "Ledger Reconciliation Scheduler", RetryAllowed = false, MaxRetries = 0, DefaultStuckThresholdSeconds = 300 },
        ["PnlRebuildScheduler"] = new JobDefinitionDto { JobType = "PnlRebuildScheduler", DisplayName = "P&L Rebuild Scheduler", RetryAllowed = false, MaxRetries = 0, DefaultStuckThresholdSeconds = 300 },
        ["EventHandlingAsync"] = new JobDefinitionDto { JobType = "EventHandlingAsync", DisplayName = "Event Handling (Async)", RetryAllowed = true, MaxRetries = 5, DefaultStuckThresholdSeconds = 600 },
        ["OperationalReplay"] = new JobDefinitionDto { JobType = "OperationalReplay", DisplayName = "Operational Replay", RetryAllowed = true, MaxRetries = 2, DefaultStuckThresholdSeconds = 7200 },
        ["slaevaluation"] = new JobDefinitionDto { JobType = "slaevaluation", DisplayName = "SLA Evaluation", RetryAllowed = true, MaxRetries = 2, DefaultStuckThresholdSeconds = 600 },
    };

    public JobDefinitionProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobDefinitionDto?> GetByJobTypeAsync(string jobType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobType)) return null;

        var fromDb = await _context.JobDefinitions
            .AsNoTracking()
            .Where(d => d.JobType == jobType)
            .Select(d => new JobDefinitionDto
            {
                Id = d.Id,
                JobType = d.JobType,
                DisplayName = d.DisplayName,
                RetryAllowed = d.RetryAllowed,
                MaxRetries = d.MaxRetries,
                DefaultStuckThresholdSeconds = d.DefaultStuckThresholdSeconds
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (fromDb != null) return fromDb;

        return Defaults.TryGetValue(jobType.Trim(), out var def) ? def : null;
    }

    public async Task<IReadOnlyList<JobDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var fromDb = await _context.JobDefinitions
            .AsNoTracking()
            .OrderBy(d => d.JobType)
            .Select(d => new JobDefinitionDto
            {
                Id = d.Id,
                JobType = d.JobType,
                DisplayName = d.DisplayName,
                RetryAllowed = d.RetryAllowed,
                MaxRetries = d.MaxRetries,
                DefaultStuckThresholdSeconds = d.DefaultStuckThresholdSeconds
            })
            .ToListAsync(cancellationToken);

        var fromDefaults = Defaults.Values
            .Where(d => !fromDb.Any(x => string.Equals(x.JobType, d.JobType, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return fromDb.Concat(fromDefaults).OrderBy(d => d.JobType, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
