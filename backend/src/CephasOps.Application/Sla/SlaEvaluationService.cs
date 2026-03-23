using CephasOps.Domain.Events;
using CephasOps.Domain.Sla.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Sla;

/// <summary>
/// Evaluates SLA rules against WorkflowJob, EventStore, and JobRun data. Records breaches, warnings, and escalations.
/// Uses existing timestamps only; no new instrumentation.
/// </summary>
public class SlaEvaluationService : ISlaEvaluationService
{
    private readonly ApplicationDbContext _context;
    private readonly ISlaAlertSender? _alertSender;

    public const string RuleTypeWorkflowTransition = "WorkflowTransition";
    public const string RuleTypeEventProcessing = "EventProcessing";
    public const string RuleTypeBackgroundJob = "BackgroundJob";
    public const string RuleTypeEventChainStall = "EventChainStall";

    public const string SeverityWarning = "Warning";
    public const string SeverityBreach = "Breach";
    public const string SeverityCritical = "Critical";

    public SlaEvaluationService(ApplicationDbContext context, ISlaAlertSender? alertSender = null)
    {
        _context = context;
        _alertSender = alertSender;
    }

    /// <inheritdoc />
    public async Task<SlaEvaluationResult> EvaluateAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var result = new SlaEvaluationResult();
        var rules = await _context.SlaRules.AsNoTracking()
            .Where(r => r.Enabled && !r.IsDeleted)
            .Where(r => !companyId.HasValue || r.CompanyId == companyId.Value)
            .ToListAsync(cancellationToken);

        result.RulesEvaluated = rules.Count;
        var now = DateTime.UtcNow;

        foreach (var rule in rules)
        {
            if (string.Equals(rule.RuleType, RuleTypeWorkflowTransition, StringComparison.OrdinalIgnoreCase))
                await EvaluateWorkflowTransitionRuleAsync(rule, now, result, cancellationToken);
            else if (string.Equals(rule.RuleType, RuleTypeEventProcessing, StringComparison.OrdinalIgnoreCase))
                await EvaluateEventProcessingRuleAsync(rule, now, result, cancellationToken);
            else if (string.Equals(rule.RuleType, RuleTypeBackgroundJob, StringComparison.OrdinalIgnoreCase))
                await EvaluateBackgroundJobRuleAsync(rule, now, result, cancellationToken);
            else if (string.Equals(rule.RuleType, RuleTypeEventChainStall, StringComparison.OrdinalIgnoreCase))
                await EvaluateEventChainStallRuleAsync(rule, now, result, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task EvaluateWorkflowTransitionRuleAsync(SlaRule rule, DateTime now, SlaEvaluationResult result, CancellationToken cancellationToken)
    {
        var query = _context.WorkflowJobs.AsNoTracking()
            .Where(w => w.CompletedAt != null && (w.State == WorkflowJobState.Succeeded || w.State == WorkflowJobState.Failed))
            .Where(w => !rule.CompanyId.HasValue || w.CompanyId == rule.CompanyId.Value);

        if (!string.IsNullOrEmpty(rule.TargetName) && rule.TargetName != "*")
        {
            if (Guid.TryParse(rule.TargetName, out var wfId))
                query = query.Where(w => w.WorkflowDefinitionId == wfId);
            else
                query = query.Where(w => w.EntityType == rule.TargetName);
        }

        var windowStart = now.AddHours(-24);
        var jobs = await query.Where(w => w.CompletedAt >= windowStart).ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            var durationSeconds = (job.CompletedAt!.Value - job.CreatedAt).TotalSeconds;
            var (severity, shouldRecord) = Classify(rule, durationSeconds);
            if (!shouldRecord) continue;
            if (AlreadyRecorded(rule.Id, "workflow", job.Id.ToString())) continue;

            var breach = CreateBreach(rule, "workflow", job.Id.ToString(), job.CorrelationId, job.CompanyId, now, durationSeconds, severity,
                $"Workflow {job.EntityType} {job.CurrentStatus} → {job.TargetStatus}");
            _context.SlaBreaches.Add(breach);
            IncrementResult(result, severity);
            await NotifyCriticalAsync(breach, cancellationToken);
        }
    }

    private async Task EvaluateEventProcessingRuleAsync(SlaRule rule, DateTime now, SlaEvaluationResult result, CancellationToken cancellationToken)
    {
        var query = _context.EventStore.AsNoTracking()
            .Where(e => e.ProcessedAtUtc != null)
            .Where(e => !rule.CompanyId.HasValue || e.CompanyId == rule.CompanyId.Value);

        if (!string.IsNullOrEmpty(rule.TargetName) && rule.TargetName != "*")
            query = query.Where(e => e.EventType == rule.TargetName);

        var windowStart = now.AddHours(-24);
        var events = await query.Where(e => e.ProcessedAtUtc >= windowStart).ToListAsync(cancellationToken);

        foreach (var evt in events)
        {
            var durationSeconds = (evt.ProcessedAtUtc!.Value - evt.CreatedAtUtc).TotalSeconds;
            var (severity, shouldRecord) = Classify(rule, durationSeconds);
            if (!shouldRecord) continue;
            if (AlreadyRecorded(rule.Id, "event", evt.EventId.ToString())) continue;

            var breach = CreateBreach(rule, "event", evt.EventId.ToString(), evt.CorrelationId, evt.CompanyId, now, durationSeconds, severity,
                $"Event {evt.EventType}");
            _context.SlaBreaches.Add(breach);
            IncrementResult(result, severity);
            await NotifyCriticalAsync(breach, cancellationToken);
        }
    }

    private async Task EvaluateBackgroundJobRuleAsync(SlaRule rule, DateTime now, SlaEvaluationResult result, CancellationToken cancellationToken)
    {
        var query = _context.JobRuns.AsNoTracking()
            .Where(r => r.CompletedAtUtc != null)
            .Where(r => !rule.CompanyId.HasValue || r.CompanyId == rule.CompanyId.Value);

        if (!string.IsNullOrEmpty(rule.TargetName) && rule.TargetName != "*")
            query = query.Where(r => r.JobType == rule.TargetName || r.JobName == rule.TargetName);

        var windowStart = now.AddHours(-24);
        var runs = await query.Where(r => r.CompletedAtUtc >= windowStart).ToListAsync(cancellationToken);

        foreach (var run in runs)
        {
            var durationSeconds = run.DurationMs.HasValue ? run.DurationMs.Value / 1000.0 : (run.CompletedAtUtc!.Value - run.StartedAtUtc).TotalSeconds;
            var (severity, shouldRecord) = Classify(rule, durationSeconds);
            if (!shouldRecord) continue;
            if (AlreadyRecorded(rule.Id, "job", run.Id.ToString())) continue;

            var breach = CreateBreach(rule, "job", run.Id.ToString(), run.CorrelationId, run.CompanyId, now, durationSeconds, severity,
                $"Job {run.JobName} ({run.JobType})");
            _context.SlaBreaches.Add(breach);
            IncrementResult(result, severity);
            await NotifyCriticalAsync(breach, cancellationToken);
        }
    }

    private async Task EvaluateEventChainStallRuleAsync(SlaRule rule, DateTime now, SlaEvaluationResult result, CancellationToken cancellationToken)
    {
        var threshold = TimeSpan.FromSeconds(rule.MaxDurationSeconds);
        var cutoff = now - threshold;

        var query = _context.EventStore.AsNoTracking()
            .Where(e => (e.Status == "Pending" || e.Status == "Processing") && e.CreatedAtUtc < cutoff)
            .Where(e => !rule.CompanyId.HasValue || e.CompanyId == rule.CompanyId.Value);

        if (!string.IsNullOrEmpty(rule.TargetName) && rule.TargetName != "*")
            query = query.Where(e => e.EventType == rule.TargetName);

        var stalled = await query.Take(500).ToListAsync(cancellationToken);

        foreach (var evt in stalled)
        {
            var durationSeconds = (now - evt.CreatedAtUtc).TotalSeconds;
            if (AlreadyRecorded(rule.Id, "event", evt.EventId.ToString())) continue;

            var breach = CreateBreach(rule, "event", evt.EventId.ToString(), evt.CorrelationId, evt.CompanyId, now, durationSeconds, SeverityCritical,
                $"Event chain stall: {evt.EventType}");
            _context.SlaBreaches.Add(breach);
            result.EscalationsRecorded++;
            await NotifyCriticalAsync(breach, cancellationToken);
        }
    }

    private static (string Severity, bool ShouldRecord) Classify(SlaRule rule, double durationSeconds)
    {
        if (rule.EscalationThresholdSeconds.HasValue && durationSeconds >= rule.EscalationThresholdSeconds.Value)
            return (SeverityCritical, true);
        if (durationSeconds >= rule.MaxDurationSeconds)
            return (SeverityBreach, true);
        if (rule.WarningThresholdSeconds.HasValue && durationSeconds >= rule.WarningThresholdSeconds.Value)
            return (SeverityWarning, true);
        return (SeverityWarning, false);
    }

    private bool AlreadyRecorded(Guid ruleId, string targetType, string targetId)
    {
        return _context.SlaBreaches.Any(b => b.RuleId == ruleId && b.TargetType == targetType && b.TargetId == targetId && b.Status == "Open");
    }

    private static SlaBreach CreateBreach(SlaRule rule, string targetType, string targetId, string? correlationId, Guid? companyId, DateTime detectedAtUtc, double durationSeconds, string severity, string? title)
    {
        return new SlaBreach
        {
            CompanyId = companyId,
            RuleId = rule.Id,
            TargetType = targetType,
            TargetId = targetId,
            CorrelationId = correlationId,
            DetectedAtUtc = detectedAtUtc,
            DurationSeconds = durationSeconds,
            Severity = severity,
            Status = "Open",
            Title = title
        };
    }

    private static void IncrementResult(SlaEvaluationResult result, string severity)
    {
        if (severity == SeverityCritical) result.EscalationsRecorded++;
        else if (severity == SeverityBreach) result.BreachesRecorded++;
        else result.WarningsRecorded++;
    }

    private Task NotifyCriticalAsync(SlaBreach breach, CancellationToken cancellationToken)
    {
        if (_alertSender == null || breach.Severity != SeverityCritical) return Task.CompletedTask;
        var payload = new SlaBreachAlertPayload
        {
            BreachId = breach.Id,
            CompanyId = breach.CompanyId,
            Severity = breach.Severity,
            TargetType = breach.TargetType,
            TargetId = breach.TargetId,
            CorrelationId = breach.CorrelationId,
            Title = breach.Title,
            DurationSeconds = breach.DurationSeconds,
            DetectedAtUtc = breach.DetectedAtUtc
        };
        return _alertSender.SendBreachAlertAsync(payload, cancellationToken);
    }
}
