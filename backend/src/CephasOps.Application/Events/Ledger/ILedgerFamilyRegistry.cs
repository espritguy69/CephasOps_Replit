using CephasOps.Application.Events.Ledger.DTOs;

namespace CephasOps.Application.Events.Ledger;

/// <summary>Registry of supported ledger families with ordering and display metadata.</summary>
public interface ILedgerFamilyRegistry
{
    IReadOnlyList<LedgerFamilyDescriptorDto> GetAll();
}
