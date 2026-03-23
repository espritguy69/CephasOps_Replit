namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Validates ledger payload snapshots before write: JSON validity and size limit.
/// Single validation point for all ledger writes.
/// </summary>
public interface ILedgerPayloadValidator
{
    /// <summary>
    /// Validates and optionally sanitizes the payload. Returns the payload to store (may be null, or a placeholder if oversized).
    /// Does not throw; invalid payloads result in a result with WasRejected or WasTruncated.
    /// </summary>
    LedgerPayloadValidationResult Validate(
        string? payloadSnapshot,
        string ledgerFamily,
        string eventType);
}
