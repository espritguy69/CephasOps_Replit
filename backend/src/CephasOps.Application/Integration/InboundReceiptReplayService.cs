using System.Text.Json;
using CephasOps.Domain.Integration.Entities;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Integration;

/// <summary>
/// Re-runs the inbound webhook handler for a receipt in HandlerFailed status. Builds IntegrationMessage from stored receipt and invokes the registered handler.
/// </summary>
public sealed class InboundReceiptReplayService : IInboundReceiptReplayService
{
    private readonly IInboundWebhookReceiptStore _receiptStore;
    private readonly IEnumerable<IInboundWebhookHandler> _handlers;
    private readonly ILogger<InboundReceiptReplayService> _logger;

    public InboundReceiptReplayService(
        IInboundWebhookReceiptStore receiptStore,
        IEnumerable<IInboundWebhookHandler> handlers,
        ILogger<InboundReceiptReplayService> logger)
    {
        _receiptStore = receiptStore;
        _handlers = handlers;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<InboundReceiptReplayResult> ReplayAsync(Guid receiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receiptStore.GetByIdAsync(receiptId, cancellationToken);
        if (receipt == null)
        {
            _logger.LogWarning("Inbound receipt replay: receipt {ReceiptId} not found", receiptId);
            return new InboundReceiptReplayResult { Success = false, ReceiptNotFound = true, ErrorMessage = "Receipt not found." };
        }

        if (receipt.Status != InboundWebhookReceipt.Statuses.HandlerFailed)
        {
            _logger.LogWarning("Inbound receipt replay: receipt {ReceiptId} has status {Status}; only HandlerFailed can be replayed", receiptId, receipt.Status);
            return new InboundReceiptReplayResult
            {
                Success = false,
                InvalidStatus = true,
                ErrorMessage = $"Receipt status is {receipt.Status}. Only HandlerFailed receipts can be replayed."
            };
        }

        var message = BuildMessageFromReceipt(receipt);
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(receipt.ConnectorKey, receipt.MessageType));
        if (handler == null)
        {
            _logger.LogWarning("Inbound receipt replay: no handler for {ConnectorKey} / {MessageType}", receipt.ConnectorKey, receipt.MessageType);
            return new InboundReceiptReplayResult
            {
                Success = false,
                NoHandler = true,
                ErrorMessage = $"No handler registered for connector {receipt.ConnectorKey} and message type {receipt.MessageType}."
            };
        }

        receipt.Status = InboundWebhookReceipt.Statuses.Processing;
        receipt.HandlerAttemptCount = (receipt.HandlerAttemptCount ?? 0) + 1;
        receipt.HandlerErrorMessage = null;
        receipt.UpdatedAtUtc = DateTime.UtcNow;
        await _receiptStore.UpdateAsync(receipt, cancellationToken);

        try
        {
            await handler.HandleAsync(message, receipt.Id, cancellationToken);
            receipt.Status = InboundWebhookReceipt.Statuses.Processed;
            receipt.ProcessedAtUtc = DateTime.UtcNow;
            receipt.HandlerErrorMessage = null;
            receipt.UpdatedAtUtc = DateTime.UtcNow;
            await _receiptStore.UpdateAsync(receipt, cancellationToken);
            _logger.LogInformation("Inbound receipt replay succeeded for receipt {ReceiptId}", receiptId);
            return new InboundReceiptReplayResult { Success = true };
        }
        catch (Exception ex)
        {
            receipt.Status = InboundWebhookReceipt.Statuses.HandlerFailed;
            receipt.HandlerErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            receipt.UpdatedAtUtc = DateTime.UtcNow;
            await _receiptStore.UpdateAsync(receipt, cancellationToken);
            _logger.LogError(ex, "Inbound receipt replay handler failed for receipt {ReceiptId}", receiptId);
            return new InboundReceiptReplayResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static IntegrationMessage BuildMessageFromReceipt(InboundWebhookReceipt receipt)
    {
        var messageId = Guid.NewGuid();
        var occurredAt = receipt.ReceivedAtUtc;
        if (!string.IsNullOrEmpty(receipt.PayloadJson))
        {
            try
            {
                var doc = JsonDocument.Parse(receipt.PayloadJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("messageId", out var mi) && mi.TryGetGuid(out var g))
                    messageId = g;
                else if (root.TryGetProperty("eventId", out var ei) && ei.TryGetGuid(out var g2))
                    messageId = g2;
                if (root.TryGetProperty("occurredAtUtc", out var oa) && oa.TryGetDateTime(out var dt))
                    occurredAt = dt;
                else if (root.TryGetProperty("occurred_at", out var oa2) && oa2.TryGetDateTime(out var dt2))
                    occurredAt = dt2;
            }
            catch
            {
                // use defaults
            }
        }

        return new IntegrationMessage
        {
            MessageId = messageId,
            EventType = receipt.MessageType ?? "Unknown",
            OccurredAtUtc = occurredAt,
            CompanyId = receipt.CompanyId,
            PayloadJson = receipt.PayloadJson ?? "{}",
            Headers = new Dictionary<string, string>()
        };
    }
}
