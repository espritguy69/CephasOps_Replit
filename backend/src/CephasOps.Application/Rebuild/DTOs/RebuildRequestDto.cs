namespace CephasOps.Application.Rebuild.DTOs;

/// <summary>Request for a rebuild run (preview or execute).</summary>
public class RebuildRequestDto
{
    public string RebuildTargetId { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public DateTime? FromOccurredAtUtc { get; set; }
    public DateTime? ToOccurredAtUtc { get; set; }
    public bool DryRun { get; set; } = true;
}
