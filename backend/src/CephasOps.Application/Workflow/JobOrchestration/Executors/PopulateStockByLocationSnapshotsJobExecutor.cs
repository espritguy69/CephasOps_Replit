using System.Text.Json;
using CephasOps.Application.Inventory.Services;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Populates stock-by-location snapshots for a period (Phase 4). Payload: periodEndDate (optional, ISO), snapshotType (optional, default Daily), companyId (optional).
/// </summary>
public sealed class PopulateStockByLocationSnapshotsJobExecutor : IJobExecutor
{
    public string JobType => "PopulateStockByLocationSnapshots";

    private readonly IStockLedgerService _ledgerService;
    private readonly ILogger<PopulateStockByLocationSnapshotsJobExecutor> _logger;

    public PopulateStockByLocationSnapshotsJobExecutor(IStockLedgerService ledgerService, ILogger<PopulateStockByLocationSnapshotsJobExecutor> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing populate stock-by-location snapshots job {JobId}", job.Id);
        var periodEndDate = DateTime.UtcNow.Date.AddDays(-1);
        var snapshotType = "Daily";
        Guid? companyId = job.CompanyId;
        if (!string.IsNullOrEmpty(job.PayloadJson))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
                if (payload != null)
                {
                    if (payload.TryGetValue("periodEndDate", out var pd) && DateTime.TryParse(pd.ValueKind == JsonValueKind.String ? pd.GetString() : pd.ToString(), out var parsed))
                        periodEndDate = parsed.Date;
                    if (payload.TryGetValue("snapshotType", out var st) && !string.IsNullOrWhiteSpace(st.ToString()))
                        snapshotType = st.ToString()!.Trim();
                    if (payload.TryGetValue("companyId", out var cid))
                    {
                        var cidStr = cid.ValueKind == JsonValueKind.String ? cid.GetString() : cid.ToString();
                        if (!string.IsNullOrEmpty(cidStr) && Guid.TryParse(cidStr, out var g))
                            companyId = g;
                    }
                }
            }
            catch { /* use defaults */ }
        }
        await _ledgerService.EnsureSnapshotsForPeriodAsync(periodEndDate, snapshotType, companyId, cancellationToken);
        _logger.LogInformation("Populate stock-by-location snapshots job {JobId} completed for {Period:yyyy-MM-dd}", job.Id, periodEndDate);
        return true;
    }
}
