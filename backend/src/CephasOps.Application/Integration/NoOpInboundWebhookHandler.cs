namespace CephasOps.Application.Integration;

/// <summary>
/// No-op handler when no specific handler is registered. CanHandle returns false so it never runs.
/// Ensures the handlers collection is non-empty; actual handlers should be registered per connector.
/// </summary>
public class NoOpInboundWebhookHandler : IInboundWebhookHandler
{
    public bool CanHandle(string connectorKey, string? messageType) => false;

    public Task HandleAsync(IntegrationMessage message, Guid receiptId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
