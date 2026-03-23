namespace CephasOps.Application.Events.Ledger;

/// <summary>Result of validating a ledger payload snapshot before write.</summary>
public sealed class LedgerPayloadValidationResult
{
    /// <summary>The payload to store (validated, or placeholder if truncated, or null if rejected).</summary>
    public string? PayloadToStore { get; init; }

    /// <summary>True if the payload was invalid (e.g. malformed JSON) and should not be stored.</summary>
    public bool WasRejected { get; init; }

    /// <summary>True if the payload exceeded max size and was replaced with a placeholder.</summary>
    public bool WasTruncated { get; init; }

    /// <summary>Reason for rejection when WasRejected is true.</summary>
    public string? RejectionReason { get; init; }

    /// <summary>Original payload size in bytes (for logging).</summary>
    public int? OriginalSizeBytes { get; init; }

    public static LedgerPayloadValidationResult Accept(string? payload) => new()
    {
        PayloadToStore = payload,
        WasRejected = false,
        WasTruncated = false
    };

    public static LedgerPayloadValidationResult Reject(string reason, int? originalSize = null) => new()
    {
        PayloadToStore = null,
        WasRejected = true,
        RejectionReason = reason,
        OriginalSizeBytes = originalSize
    };

    public static LedgerPayloadValidationResult Truncated(string placeholderJson, int originalSize) => new()
    {
        PayloadToStore = placeholderJson,
        WasRejected = false,
        WasTruncated = true,
        OriginalSizeBytes = originalSize
    };
}
