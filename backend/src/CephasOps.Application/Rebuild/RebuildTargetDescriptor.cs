namespace CephasOps.Application.Rebuild;

/// <summary>
/// Descriptor for a single rebuildable operational state target.
/// Explicit and discoverable; no hardcoded if/else for target logic.
/// </summary>
public sealed class RebuildTargetDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>Source of truth: EventStore or EventLedger.</summary>
    public string SourceOfTruth { get; init; } = string.Empty;

    /// <summary>Rebuild strategy: FullReplace, IdempotentUpsert, or BoundedAppend.</summary>
    public string RebuildStrategy { get; init; } = string.Empty;

    /// <summary>Scope rules: e.g. CompanyId, FromOccurredAtUtc, ToOccurredAtUtc.</summary>
    public IReadOnlyList<string> ScopeRuleNames { get; init; } = Array.Empty<string>();

    /// <summary>Ordering guarantee description (e.g. OccurredAtUtc ASC, EventId ASC).</summary>
    public string OrderingGuarantee { get; init; } = string.Empty;

    /// <summary>True = full table/scope replace; false = bounded (e.g. time window only).</summary>
    public bool IsFullRebuild { get; init; }

    /// <summary>Whether preview (dry-run) is supported for this target.</summary>
    public bool SupportsPreview { get; init; } = true;

    /// <summary>Optional limitations or caveats.</summary>
    public IReadOnlyList<string> Limitations { get; init; } = Array.Empty<string>();

    /// <summary>Whether this target supports checkpoint/resume (Phase 2).</summary>
    public bool SupportsResume { get; init; }
}
