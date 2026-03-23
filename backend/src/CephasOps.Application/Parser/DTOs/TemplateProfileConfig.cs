using System.Text.Json.Serialization;

namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// JSON-serializable template profile config. Stored in ParserTemplates.Description as "PROFILE_JSON: {...}".
/// Phase 9: versioning and pack for lifecycle and regression testing.
/// </summary>
public class TemplateProfileConfig
{
    [JsonPropertyName("profileId")]
    public Guid ProfileId { get; set; }

    [JsonPropertyName("profileName")]
    public string ProfileName { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("matchRules")]
    public TemplateProfileMatchRules? MatchRules { get; set; }

    [JsonPropertyName("parseHints")]
    public TemplateProfileParseHints? ParseHints { get; set; }

    [JsonPropertyName("driftThresholds")]
    public TemplateProfileDriftThresholds? DriftThresholds { get; set; }

    /// <summary>Phase 9: version string (e.g. "1.0.0"). Absent => null.</summary>
    [JsonPropertyName("profileVersion")]
    public string? ProfileVersion { get; set; }

    /// <summary>Phase 9: effective from date (ISO). Absent => null.</summary>
    [JsonPropertyName("effectiveFrom")]
    public string? EffectiveFrom { get; set; }

    /// <summary>Phase 9: change notes. Absent => null.</summary>
    [JsonPropertyName("changeNotes")]
    public string? ChangeNotes { get; set; }

    /// <summary>Phase 9: owner (email/name). Absent => null.</summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>Phase 9: pack definition for regression testing. Absent => null.</summary>
    [JsonPropertyName("pack")]
    public ProfilePackConfig? Pack { get; set; }
}

/// <summary>
/// Phase 9: Pack definition for profile regression testing.
/// </summary>
public class ProfilePackConfig
{
    [JsonPropertyName("attachmentIds")]
    public List<Guid>? AttachmentIds { get; set; }

    [JsonPropertyName("parseSessionIds")]
    public List<Guid>? ParseSessionIds { get; set; }

    [JsonPropertyName("packName")]
    public string? PackName { get; set; }

    [JsonPropertyName("packDescription")]
    public string? PackDescription { get; set; }
}

public class TemplateProfileMatchRules
{
    [JsonPropertyName("senderDomains")]
    public List<string>? SenderDomains { get; set; }

    [JsonPropertyName("subjectContains")]
    public List<string>? SubjectContains { get; set; }

    [JsonPropertyName("filenameRegex")]
    public List<string>? FilenameRegex { get; set; }

    [JsonPropertyName("partnerIds")]
    public List<Guid>? PartnerIds { get; set; }
}

public class TemplateProfileParseHints
{
    [JsonPropertyName("preferredSheetNames")]
    public List<string>? PreferredSheetNames { get; set; }

    [JsonPropertyName("headerRowRange")]
    public HeaderRowRange? HeaderRowRange { get; set; }

    [JsonPropertyName("requiredFieldSynonymOverrides")]
    public Dictionary<string, List<string>>? RequiredFieldSynonymOverrides { get; set; }

    [JsonPropertyName("orderTypeHints")]
    public OrderTypeHints? OrderTypeHints { get; set; }
}

public class HeaderRowRange
{
    [JsonPropertyName("min")]
    public int Min { get; set; } = 1;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 30;
}

public class OrderTypeHints
{
    [JsonPropertyName("assuranceIndicators")]
    public List<string>? AssuranceIndicators { get; set; }

    [JsonPropertyName("activationIndicators")]
    public List<string>? ActivationIndicators { get; set; }

    [JsonPropertyName("modificationIndicators")]
    public List<string>? ModificationIndicators { get; set; }
}

public class TemplateProfileDriftThresholds
{
    [JsonPropertyName("bestSheetScoreDrop")]
    public int BestSheetScoreDrop { get; set; } = 3;

    [JsonPropertyName("headerScoreDrop")]
    public int HeaderScoreDrop { get; set; } = 2;

    [JsonPropertyName("headerRowShift")]
    public int HeaderRowShift { get; set; } = 3;
}
