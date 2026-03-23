namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Phase 9: Optional context passed when running replay in profile/pack mode. Stored in ResultSummary for ops traceability.
/// </summary>
public class ProfileLifecycleContext
{
    public Guid ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfileVersion { get; set; }
    public string? EffectiveFrom { get; set; }
    public string? Owner { get; set; }
    public string? PackName { get; set; }
    public string? ProfileChangeNotes { get; set; }
}
