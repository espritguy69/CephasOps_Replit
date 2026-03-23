using CephasOps.Domain.Common;

namespace CephasOps.Domain.Sla.Entities;

/// <summary>
/// SLA rule definition for operational SLA Intelligence (workflow, event, job duration and stall detection).
/// Company-scoped; defines max duration, warning and escalation thresholds.
/// </summary>
public class SlaRule : CompanyScopedEntity
{
    /// <summary>
    /// Rule type: WorkflowTransition, EventProcessing, BackgroundJob, EventChainStall.
    /// </summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>
    /// Target type: workflow, event, job.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Target identifier: workflow definition name or ID, event type name, job type name, or "*" for all.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum allowed duration in seconds. Exceeding this is a breach.
    /// </summary>
    public int MaxDurationSeconds { get; set; }

    /// <summary>
    /// Optional. Duration in seconds at which to record a warning (below breach).
    /// </summary>
    public int? WarningThresholdSeconds { get; set; }

    /// <summary>
    /// Optional. Duration in seconds at which to escalate (e.g. critical alert).
    /// </summary>
    public int? EscalationThresholdSeconds { get; set; }

    /// <summary>
    /// Whether this rule is enabled for evaluation.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
