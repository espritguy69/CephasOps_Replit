namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Context passed to the parser when a template profile matches. All hints are optional; parser falls back to v7 behavior when null.
/// </summary>
public class TemplateProfileContext
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>Ordered preferred sheet names; parser may boost or choose first that scores above threshold.</summary>
    public IReadOnlyList<string>? PreferredSheetNames { get; set; }

    /// <summary>Header row scan range (min..max). Default 1..30.</summary>
    public (int Min, int Max)? HeaderRowRange { get; set; }

    /// <summary>Override synonyms per required field (e.g. ServiceId -> ["TBBN", "Service ID"]). Profile takes precedence over built-in.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? RequiredFieldSynonymOverrides { get; set; }

    /// <summary>Drift thresholds from profile for drift detection.</summary>
    public TemplateProfileDriftThresholds? DriftThresholds { get; set; }
}
