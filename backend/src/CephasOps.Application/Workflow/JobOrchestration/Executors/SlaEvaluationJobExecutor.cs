using CephasOps.Domain.Workflow.Entities;
using CephasOps.Application.Sla;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Runs SLA evaluation for all or optional company (Phase 4). Payload: companyId (optional).
/// </summary>
public sealed class SlaEvaluationJobExecutor : IJobExecutor
{
    public string JobType => "SlaEvaluation";

    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly ILogger<SlaEvaluationJobExecutor> _logger;

    public SlaEvaluationJobExecutor(ISlaEvaluationService slaEvaluation, ILogger<SlaEvaluationJobExecutor> logger)
    {
        _slaEvaluation = slaEvaluation;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing SLA evaluation job {JobId}", job.Id);
        Guid? companyId = job.CompanyId;
        if (!string.IsNullOrEmpty(job.PayloadJson))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
                if (payload != null && payload.TryGetValue("companyId", out var cidEl))
                {
                    var cidStr = cidEl.ValueKind == JsonValueKind.String ? cidEl.GetString() : cidEl.ToString();
                    if (!string.IsNullOrEmpty(cidStr) && Guid.TryParse(cidStr, out var cid))
                        companyId = cid;
                }
            }
            catch { /* use job.CompanyId */ }
        }
        var result = await _slaEvaluation.EvaluateAsync(companyId, cancellationToken);
        _logger.LogInformation("SLA evaluation job {JobId} completed: {RulesEvaluated} rules, {Breaches} breaches, {Warnings} warnings, {Escalations} escalations",
            job.Id, result.RulesEvaluated, result.BreachesRecorded, result.WarningsRecorded, result.EscalationsRecorded);
        return true;
    }
}
