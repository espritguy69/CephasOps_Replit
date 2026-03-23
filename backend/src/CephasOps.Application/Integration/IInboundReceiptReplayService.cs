namespace CephasOps.Application.Integration;

/// <summary>
/// Re-runs the handler for an inbound webhook receipt that is in HandlerFailed status.
/// Idempotency key is already claimed; handler must be idempotent by business key.
/// </summary>
public interface IInboundReceiptReplayService
{
    /// <summary>
    /// Replay (re-run handler) for the given receipt. Only receipts in HandlerFailed status can be replayed.
    /// Returns success and optional error message.
    /// </summary>
    Task<InboundReceiptReplayResult> ReplayAsync(Guid receiptId, CancellationToken cancellationToken = default);
}

/// <summary>Result of replaying one inbound receipt.</summary>
public sealed class InboundReceiptReplayResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ReceiptNotFound { get; set; }
    public bool InvalidStatus { get; set; }
    public bool NoHandler { get; set; }
}
