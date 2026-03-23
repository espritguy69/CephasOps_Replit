namespace CephasOps.Application.Integration;

/// <summary>
/// Verifies inbound webhook request (signature, timestamp). Per-connector; failure = no application action.
/// </summary>
public interface IInboundWebhookVerifier
{
    /// <summary>Whether this verifier handles the given connector key.</summary>
    bool CanVerify(string connectorKey);

    /// <summary>Verify request. Returns (true, null) if valid; (false, reason) if invalid.</summary>
    Task<(bool IsValid, string? FailureReason)> VerifyAsync(
        string connectorKey,
        string? signatureHeader,
        string? timestampHeader,
        string requestBody,
        string? verificationConfigJson,
        CancellationToken cancellationToken = default);
}
