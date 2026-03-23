namespace CephasOps.Application.Events.Ledger.DTOs;

public class LedgerFamilyDescriptorDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OrderingStrategyId { get; set; }
    public string? OrderingGuaranteeLevel { get; set; }
}
