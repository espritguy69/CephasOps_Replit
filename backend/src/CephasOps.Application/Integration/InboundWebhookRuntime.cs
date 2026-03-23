using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Integration;

/// <summary>
/// Inbound webhook runtime: verify → persist receipt → idempotency → normalize → dispatch to handler.
/// </summary>
public class InboundWebhookRuntime : IInboundWebhookRuntime
{
    private readonly IConnectorRegistry _registry;
    private readonly IInboundWebhookReceiptStore _receiptStore;
    private readonly IExternalIdempotencyStore _idempotencyStore;
    private readonly IEnumerable<IInboundWebhookVerifier> _verifiers;
    private readonly IEnumerable<IInboundWebhookHandler> _handlers;
    private readonly ILogger<InboundWebhookRuntime> _logger;

    public InboundWebhookRuntime(
        IConnectorRegistry registry,
        IInboundWebhookReceiptStore receiptStore,
        IExternalIdempotencyStore idempotencyStore,
        IEnumerable<IInboundWebhookVerifier> verifiers,
        IEnumerable<IInboundWebhookHandler> handlers,
        ILogger<InboundWebhookRuntime> logger)
    {
        _registry = registry;
        _receiptStore = receiptStore;
        _idempotencyStore = idempotencyStore;
        _verifiers = verifiers;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task<InboundWebhookResult> ProcessAsync(InboundWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var endpoint = await _registry.GetInboundEndpointAsync(request.ConnectorKey, request.CompanyId, cancellationToken);
        if (endpoint == null)
        {
            _logger.LogWarning("Inbound webhook: no endpoint for connector {ConnectorKey}", request.ConnectorKey);
            return new InboundWebhookResult { Accepted = false, FailureReason = "Connector not configured", SuggestedHttpStatusCode = 404 };
        }

        // InboundWebhookReceipt is tenant-scoped; run under tenant scope or platform bypass and restore in finally
        return await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(
            request.CompanyId,
            (ct) => ProcessCoreAsync(request, endpoint, ct),
            cancellationToken);
    }

    private async Task<InboundWebhookResult> ProcessCoreAsync(InboundWebhookRequest request, ConnectorEndpoint endpoint, CancellationToken cancellationToken)
    {
        var receiptCompanyId = request.CompanyId ?? endpoint.CompanyId;
        if (!receiptCompanyId.HasValue || receiptCompanyId.Value == Guid.Empty)
            _logger.LogWarning("Inbound webhook: receipt will have null CompanyId (request and endpoint both missing company). ConnectorKey={ConnectorKey}, EndpointId={EndpointId}", request.ConnectorKey, endpoint.Id);

        var receipt = new InboundWebhookReceipt
        {
            Id = Guid.NewGuid(),
            ConnectorEndpointId = endpoint.Id,
            CompanyId = receiptCompanyId,
            ExternalIdempotencyKey = ComputeIdempotencyKey(request),
            ExternalEventId = request.ExternalEventId,
            ConnectorKey = request.ConnectorKey,
            Status = InboundWebhookReceipt.Statuses.Received,
            PayloadJson = request.RequestBody.Length > 100_000 ? request.RequestBody[..100_000] + "…" : request.RequestBody,
            ReceivedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await _receiptStore.CreateAsync(receipt, cancellationToken);

        var verifier = _verifiers.FirstOrDefault(v => v.CanVerify(request.ConnectorKey));
        if (verifier != null)
        {
            var (isValid, failureReason) = await verifier.VerifyAsync(
                request.ConnectorKey,
                request.SignatureHeader,
                request.TimestampHeader,
                request.RequestBody,
                endpoint.AuthConfigJson,
                cancellationToken);
            receipt.VerificationPassed = isValid;
            receipt.VerificationFailureReason = failureReason;
            if (!isValid)
            {
                receipt.Status = InboundWebhookReceipt.Statuses.VerificationFailed;
                receipt.UpdatedAtUtc = DateTime.UtcNow;
                await _receiptStore.UpdateAsync(receipt, cancellationToken);
                _logger.LogWarning("Inbound webhook verification failed for {ConnectorKey}: {Reason}", request.ConnectorKey, failureReason);
                return new InboundWebhookResult
                {
                    ReceiptId = receipt.Id,
                    Accepted = false,
                    VerificationPassed = false,
                    FailureReason = failureReason,
                    SuggestedHttpStatusCode = 401
                };
            }
        }
        else
        {
            receipt.VerificationPassed = true;
        }

        receipt.Status = InboundWebhookReceipt.Statuses.Verified;
        await _receiptStore.UpdateAsync(receipt, cancellationToken);

        if (await _idempotencyStore.IsCompletedAsync(receipt.ExternalIdempotencyKey, request.ConnectorKey, request.CompanyId, cancellationToken))
        {
            _logger.LogInformation("Inbound webhook duplicate suppressed: {Key}", receipt.ExternalIdempotencyKey);
            return new InboundWebhookResult
            {
                ReceiptId = receipt.Id,
                Accepted = true,
                VerificationPassed = true,
                IdempotencyReused = true,
                SuggestedHttpStatusCode = 200
            };
        }

        var claimed = await _idempotencyStore.TryClaimAsync(receipt.ExternalIdempotencyKey, request.ConnectorKey, request.CompanyId, receipt.Id, cancellationToken);
        if (!claimed)
        {
            return new InboundWebhookResult
            {
                ReceiptId = receipt.Id,
                Accepted = true,
                VerificationPassed = true,
                IdempotencyReused = true,
                SuggestedHttpStatusCode = 200
            };
        }

        IntegrationMessage? message = null;
        try
        {
            message = NormalizePayload(request.RequestBody, request.CompanyId);
            receipt.MessageType = message.EventType;
            receipt.Status = InboundWebhookReceipt.Statuses.Processing;
            receipt.HandlerAttemptCount = 1;
            await _receiptStore.UpdateAsync(receipt, cancellationToken);

            var handler = _handlers.FirstOrDefault(h => h.CanHandle(request.ConnectorKey, message.EventType));
            if (handler != null)
            {
                await handler.HandleAsync(message, receipt.Id, cancellationToken);
            }
            else
            {
                _logger.LogInformation("No handler for inbound {ConnectorKey} / {MessageType}; receipt stored", request.ConnectorKey, message.EventType);
            }

            receipt.Status = InboundWebhookReceipt.Statuses.Processed;
            receipt.ProcessedAtUtc = DateTime.UtcNow;
            await _idempotencyStore.MarkCompletedAsync(receipt.ExternalIdempotencyKey, request.ConnectorKey, request.CompanyId, receipt.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            receipt.Status = InboundWebhookReceipt.Statuses.HandlerFailed;
            receipt.HandlerErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _logger.LogError(ex, "Inbound webhook handler failed for receipt {ReceiptId}", receipt.Id);
        }

        receipt.UpdatedAtUtc = DateTime.UtcNow;
        await _receiptStore.UpdateAsync(receipt, cancellationToken);

        return new InboundWebhookResult
        {
            ReceiptId = receipt.Id,
            Accepted = true,
            VerificationPassed = receipt.VerificationPassed,
            IdempotencyReused = false,
            SuggestedHttpStatusCode = receipt.Status == InboundWebhookReceipt.Statuses.Processed ? 200 : 500
        };
    }

    private static string ComputeIdempotencyKey(InboundWebhookRequest request)
    {
        if (!string.IsNullOrEmpty(request.ExternalEventId))
            return $"in-{request.ConnectorKey}-{request.ExternalEventId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(request.RequestBody));
        return $"in-{request.ConnectorKey}-{Convert.ToHexString(hash)[..32]}";
    }

    private static IntegrationMessage NormalizePayload(string requestBody, Guid? companyId)
    {
        try
        {
            var doc = JsonDocument.Parse(requestBody);
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() ?? "Unknown" :
                root.TryGetProperty("event_type", out var et2) ? et2.GetString() ?? "Unknown" : "Unknown";
            var messageId = root.TryGetProperty("messageId", out var mi) && mi.TryGetGuid(out var g) ? g :
                root.TryGetProperty("eventId", out var ei) && ei.TryGetGuid(out var g2) ? g2 : Guid.NewGuid();
            var occurredAt = root.TryGetProperty("occurredAtUtc", out var oa) && oa.TryGetDateTime(out var dt) ? dt :
                root.TryGetProperty("occurred_at", out var oa2) && oa2.TryGetDateTime(out var dt2) ? dt2 : DateTime.UtcNow;

            return new IntegrationMessage
            {
                MessageId = messageId,
                EventType = eventType,
                OccurredAtUtc = occurredAt,
                CompanyId = companyId,
                PayloadJson = requestBody,
                Headers = new Dictionary<string, string>()
            };
        }
        catch
        {
            return new IntegrationMessage
            {
                MessageId = Guid.NewGuid(),
                EventType = "Unknown",
                OccurredAtUtc = DateTime.UtcNow,
                CompanyId = companyId,
                PayloadJson = requestBody,
                Headers = new Dictionary<string, string>()
            };
        }
    }
}
