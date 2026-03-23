namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Configuration for the operational event ledger (payload validation, size limits).
/// </summary>
public class LedgerOptions
{
    public const string SectionName = "Ledger";

    /// <summary>Maximum allowed payload snapshot size in bytes. Default 64 KB. Oversized payloads are replaced with a metadata placeholder.</summary>
    public int MaxPayloadSizeBytes { get; set; } = 64 * 1024;

    /// <summary>When true, payload must be valid JSON (object or array). Invalid JSON is rejected and not stored. Default true.</summary>
    public bool ValidateJsonPayload { get; set; } = true;
}
