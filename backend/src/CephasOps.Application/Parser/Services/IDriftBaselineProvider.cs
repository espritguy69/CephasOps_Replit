using CephasOps.Application.Parser.Utilities;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Provides the last known good baseline for a template profile (from latest successful ParsedOrderDraft for that profile).
/// Used by DriftDetector for LAYOUT_DRIFT triage (Phase 8).
/// </summary>
public interface IDriftBaselineProvider
{
    /// <summary>
    /// Get baseline (sheet name, header row, header score, best sheet score) from the latest Valid draft for the given template profile.
    /// Returns null if no such draft exists.
    /// </summary>
    Task<DriftDetector.DriftBaseline?> GetBaselineAsync(Guid templateProfileId, CancellationToken cancellationToken = default);
}
