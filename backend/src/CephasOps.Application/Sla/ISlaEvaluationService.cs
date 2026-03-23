namespace CephasOps.Application.Sla;

/// <summary>
/// Evaluates SLA rules against existing observability data and records breaches, warnings, and escalations.
/// </summary>
public interface ISlaEvaluationService
{
    /// <summary>
    /// Run full SLA evaluation for all enabled rules (all companies or optional company filter).
    /// Returns counts of breaches, warnings, and escalations recorded.
    /// </summary>
    Task<SlaEvaluationResult> EvaluateAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
}

public class SlaEvaluationResult
{
    public int BreachesRecorded { get; set; }
    public int WarningsRecorded { get; set; }
    public int EscalationsRecorded { get; set; }
    public int RulesEvaluated { get; set; }
}
