namespace CephasOps.Application.Rebuild.DTOs;

/// <summary>API-facing descriptor for a rebuild target (from registry).</summary>
public class RebuildTargetDescriptorDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceOfTruth { get; set; } = string.Empty;
    public string RebuildStrategy { get; set; } = string.Empty;
    public IReadOnlyList<string> ScopeRuleNames { get; set; } = Array.Empty<string>();
    public string OrderingGuarantee { get; set; } = string.Empty;
    public bool IsFullRebuild { get; set; }
    public bool SupportsPreview { get; set; }
    public IReadOnlyList<string> Limitations { get; set; } = Array.Empty<string>();
    public bool SupportsResume { get; set; }
}
