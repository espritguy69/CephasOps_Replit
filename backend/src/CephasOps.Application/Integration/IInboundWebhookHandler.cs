namespace CephasOps.Application.Integration;

/// <summary>
/// Handles a verified, idempotent inbound webhook by dispatching to application (command bus, service, etc.).
/// One handler per connector key or message type; registered in DI.
/// </summary>
public interface IInboundWebhookHandler
{
    /// <summary>Whether this handler handles the given connector key and optional message type.</summary>
    bool CanHandle(string connectorKey, string? messageType);

    /// <summary>Execute application logic. Throw to mark receipt as HandlerFailed.</summary>
    Task HandleAsync(IntegrationMessage message, Guid receiptId, CancellationToken cancellationToken = default);
}
