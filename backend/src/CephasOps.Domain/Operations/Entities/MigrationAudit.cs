namespace CephasOps.Domain.Operations.Entities;

/// <summary>
/// Operational audit record for migration rollouts. Records when a migration was deployed
/// in an environment, by whom, and with what verification/smoke-test result. Not used by
/// application logic; filled by operators after successful rollout. See MIGRATION_AUDIT.md.
/// </summary>
public class MigrationAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Environment where the migration was applied (e.g. Development, Staging, Production).</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>EF migration ID (e.g. 20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText).</summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>When the migration was applied (UTC).</summary>
    public DateTime AppliedAtUtc { get; set; }

    /// <summary>Who or what applied it (e.g. operator name, pipeline ID, service account).</summary>
    public string AppliedBy { get; set; } = string.Empty;

    /// <summary>Method used: EF database update, Idempotent script, Migration bundle, etc.</summary>
    public string MethodUsed { get; set; } = string.Empty;

    /// <summary>Verification result: Pass, Fail, Skipped. Run verification script before recording Pass.</summary>
    public string VerificationStatus { get; set; } = string.Empty;

    /// <summary>Smoke test result: Pass, Fail, Skipped, N/A.</summary>
    public string SmokeTestStatus { get; set; } = string.Empty;

    /// <summary>Optional notes (e.g. ticket ref, issues encountered).</summary>
    public string? Notes { get; set; }
}
