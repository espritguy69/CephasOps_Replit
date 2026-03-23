using CephasOps.Application.Pnl.Services;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Executes P&amp;L rebuild job (Phase 3). Payload: companyId (guid), period (optional, default current month).
/// </summary>
public sealed class PnlRebuildJobExecutor : IJobExecutor
{
    public string JobType => "PnlRebuild";

    private readonly IPnlService _pnlService;
    private readonly ILogger<PnlRebuildJobExecutor> _logger;

    public PnlRebuildJobExecutor(IPnlService pnlService, ILogger<PnlRebuildJobExecutor> logger)
    {
        _pnlService = pnlService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing P&L rebuild job {JobId}", job.Id);

        Dictionary<string, JsonElement>? payload = null;
        if (!string.IsNullOrEmpty(job.PayloadJson))
        {
            try
            {
                payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
            }
            catch
            {
                throw new ArgumentException("Invalid PayloadJson for PnlRebuild job");
            }
        }

        var companyId = job.CompanyId ?? Guid.Empty;
        if (payload != null && payload.TryGetValue("companyId", out var cidEl))
        {
            var cidStr = cidEl.ValueKind == JsonValueKind.String ? cidEl.GetString() : cidEl.ToString();
            if (!string.IsNullOrEmpty(cidStr) && Guid.TryParse(cidStr, out var cid))
                companyId = cid;
        }

        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required for P&L rebuild job");

        var period = payload != null && payload.TryGetValue("period", out var pEl)
            ? (pEl.ValueKind == JsonValueKind.String ? pEl.GetString() : pEl.ToString())
            : null;
        if (string.IsNullOrWhiteSpace(period))
            period = DateTime.UtcNow.ToString("yyyy-MM");

        await _pnlService.RebuildPnlAsync(companyId, period, cancellationToken);
        _logger.LogInformation("P&L rebuild job {JobId} completed for company {CompanyId}, period {Period}", job.Id, companyId, period);
        return true;
    }
}
