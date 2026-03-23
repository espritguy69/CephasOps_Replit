using CephasOps.Application.Inventory.Services;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Reconciles ledger balance cache with ledger + allocations (Phase 4). No payload.
/// </summary>
public sealed class ReconcileLedgerBalanceCacheJobExecutor : IJobExecutor
{
    public string JobType => "ReconcileLedgerBalanceCache";

    private readonly IStockLedgerService _ledgerService;
    private readonly ILogger<ReconcileLedgerBalanceCacheJobExecutor> _logger;

    public ReconcileLedgerBalanceCacheJobExecutor(IStockLedgerService ledgerService, ILogger<ReconcileLedgerBalanceCacheJobExecutor> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing reconcile ledger balance cache job {JobId}", job.Id);
        await _ledgerService.ReconcileBalanceCacheAsync(cancellationToken);
        _logger.LogInformation("Reconcile ledger balance cache job {JobId} completed", job.Id);
        return true;
    }
}
