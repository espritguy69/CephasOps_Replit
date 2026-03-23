namespace CephasOps.Application.Integration;

/// <summary>
/// No-op verifier when no verification is configured. CanVerify returns false so it never runs.
/// </summary>
public class NoOpInboundWebhookVerifier : IInboundWebhookVerifier
{
    public bool CanVerify(string connectorKey) => false;

    public Task<(bool IsValid, string? FailureReason)> VerifyAsync(
        string connectorKey,
        string? signatureHeader,
        string? timestampHeader,
        string requestBody,
        string? verificationConfigJson,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((true, (string?)null));
}
