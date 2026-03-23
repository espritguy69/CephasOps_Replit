using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Company-scoped alias mapping from a parsed material name (noisy/vendor-specific) to a canonical Material.
/// Enables "learn once, reuse forever": manual resolve or seeded aliases improve future parser resolution.
/// </summary>
public class ParsedMaterialAlias : CompanyScopedEntity
{
    /// <summary>Original alias text as seen in parser (e.g. "ONU Adaptor", "Fiber Patch Cord").</summary>
    public string AliasText { get; set; } = string.Empty;

    /// <summary>Normalized form for lookup (trim, collapse whitespace, case-insensitive match).</summary>
    public string NormalizedAliasText { get; set; } = string.Empty;

    /// <summary>Material master this alias resolves to.</summary>
    public Guid MaterialId { get; set; }

    /// <summary>Who created the alias (manual resolve or import).</summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>Source: ParserManualResolve, Seeded, Imported.</summary>
    public string? Source { get; set; }

    /// <summary>When false, alias is ignored in resolution (soft disable).</summary>
    public bool IsActive { get; set; } = true;
}
