using System.Collections.Generic;
using System.Diagnostics;
using CephasOps.Application.Events;
using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Metrics;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Integration;

/// <summary>
/// Outbound integration bus: publishes platform events to connector endpoints with delivery records, retry, and dead-letter.
/// </summary>
public class OutboundIntegrationBus : IOutboundIntegrationBus
{
    private readonly IConnectorRegistry _registry;
    private readonly IOutboundDeliveryStore _deliveryStore;
    private readonly IOutboundHttpDispatcher _httpDispatcher;
    private readonly IEnumerable<IIntegrationPayloadMapper> _mappers;
    private readonly IEnumerable<IOutboundSigner> _signers;
    private readonly ILogger<OutboundIntegrationBus> _logger;

    public OutboundIntegrationBus(
        IConnectorRegistry registry,
        IOutboundDeliveryStore deliveryStore,
        IOutboundHttpDispatcher httpDispatcher,
        IEnumerable<IIntegrationPayloadMapper> mappers,
        IEnumerable<IOutboundSigner> signers,
        ILogger<OutboundIntegrationBus> logger)
    {
        _registry = registry;
        _deliveryStore = deliveryStore;
        _httpDispatcher = httpDispatcher;
        _mappers = mappers;
        _signers = signers;
        _logger = logger;
    }

    public async Task PublishAsync(PlatformEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var endpoints = await _registry.GetOutboundEndpointsForEventAsync(envelope.EventName, envelope.CompanyId, cancellationToken);
        if (endpoints.Count == 0)
            return;

        var mapper = _mappers.FirstOrDefault(m => m.CanMap(envelope.EventName)) ?? _mappers.First(m => m.CanMap(envelope.EventName, null));
        var payloadJson = mapper.MapToPayload(envelope);

        foreach (var endpoint in endpoints)
        {
            // Tenant-safe: include CompanyId when present so the same event+endpoint in different tenants does not collide
            var deliveryCompanyId = endpoint.CompanyId ?? envelope.CompanyId;
            var idempotencyKey = (deliveryCompanyId.HasValue && deliveryCompanyId.Value != Guid.Empty)
                ? $"out-{deliveryCompanyId.Value:N}-{envelope.EventId:N}-{endpoint.Id:N}"
                : $"out-{envelope.EventId:N}-{endpoint.Id:N}";
            var existing = await _deliveryStore.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existing != null && (existing.Status == OutboundIntegrationDelivery.Statuses.Delivered || existing.Status == OutboundIntegrationDelivery.Statuses.Pending))
            {
                _logger.LogDebug("Outbound delivery already exists for {Key}", idempotencyKey);
                continue;
            }

            var delivery = new OutboundIntegrationDelivery
            {
                Id = Guid.NewGuid(),
                ConnectorEndpointId = endpoint.Id,
                CompanyId = endpoint.CompanyId ?? envelope.CompanyId,
                SourceEventId = envelope.EventId,
                EventType = envelope.EventName,
                CorrelationId = envelope.CorrelationId,
                RootEventId = envelope.RootEventId,
                IdempotencyKey = idempotencyKey,
                Status = OutboundIntegrationDelivery.Statuses.Pending,
                PayloadJson = payloadJson.Length > 100_000 ? payloadJson[..100_000] + "…" : payloadJson,
                AttemptCount = 0,
                MaxAttempts = Math.Max(1, endpoint.RetryCount + 1),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _deliveryStore.CreateDeliveryAsync(delivery, cancellationToken);
            _logger.LogInformation("Created outbound delivery {DeliveryId} for event {EventId} to endpoint {EndpointId}", delivery.Id, envelope.EventId, endpoint.Id);

            await DispatchDeliveryInternalAsync(delivery, endpoint, payloadJson, cancellationToken);
        }
    }

    public async Task<OutboundDispatchResult> DispatchDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await _deliveryStore.GetByIdAsync(deliveryId, cancellationToken);
        if (delivery == null)
            return new OutboundDispatchResult { Success = false, ErrorMessage = "Delivery not found." };

        var endpoint = await _registry.GetEndpointAsync(delivery.ConnectorEndpointId, cancellationToken);
        if (endpoint == null)
            return new OutboundDispatchResult { Success = false, ErrorMessage = "Endpoint not found." };

        return await DispatchDeliveryInternalAsync(delivery, endpoint, delivery.PayloadJson, cancellationToken);
    }

    private async Task<OutboundDispatchResult> DispatchDeliveryInternalAsync(
        OutboundIntegrationDelivery delivery,
        ConnectorEndpoint endpoint,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "application/json",
            ["X-Integration-Delivery-Id"] = delivery.Id.ToString("N"),
            ["X-Integration-Event-Type"] = delivery.EventType,
            ["X-Integration-Event-Id"] = delivery.SourceEventId.ToString("N")
        };
        if (!string.IsNullOrEmpty(delivery.CorrelationId))
            headers["X-Correlation-Id"] = delivery.CorrelationId;

        var signer = _signers.FirstOrDefault(s => s.CanSign(endpoint.ConnectorDefinition?.ConnectorType ?? "", endpoint.ConnectorDefinition?.ConnectorKey));
        if (signer != null)
        {
            signer.Sign(payloadJson, endpoint.SigningConfigJson, headers);
            if (headers.TryGetValue("X-Signature", out var sig))
                delivery.SignatureHeaderValue = sig;
        }

        var sw = Stopwatch.StartNew();
        var httpResult = await _httpDispatcher.SendAsync(
            endpoint.EndpointUrl,
            endpoint.HttpMethod,
            payloadJson,
            headers,
            endpoint.TimeoutSeconds,
            cancellationToken);
        sw.Stop();

        var attempt = new OutboundIntegrationAttempt
        {
            Id = Guid.NewGuid(),
            OutboundDeliveryId = delivery.Id,
            AttemptNumber = delivery.AttemptCount + 1,
            StartedAtUtc = DateTime.UtcNow.AddMilliseconds(-sw.ElapsedMilliseconds),
            CompletedAtUtc = DateTime.UtcNow,
            Success = httpResult.Success,
            HttpStatusCode = httpResult.HttpStatusCode,
            ResponseBodySnippet = httpResult.ResponseBodySnippet != null ? httpResult.ResponseBodySnippet.Length > 2000 ? httpResult.ResponseBodySnippet[..2000] : httpResult.ResponseBodySnippet : null,
            ErrorMessage = httpResult.ErrorMessage,
            DurationMs = httpResult.DurationMs
        };
        await _deliveryStore.AddAttemptAsync(attempt, cancellationToken);

        delivery.AttemptCount = attempt.AttemptNumber;
        delivery.LastErrorMessage = httpResult.ErrorMessage;
        delivery.LastHttpStatusCode = httpResult.HttpStatusCode;
        delivery.UpdatedAtUtc = DateTime.UtcNow;

        if (httpResult.Success)
        {
            delivery.Status = OutboundIntegrationDelivery.Statuses.Delivered;
            delivery.DeliveredAtUtc = DateTime.UtcNow;
            delivery.NextRetryAtUtc = null;
            await _deliveryStore.UpdateDeliveryAsync(delivery, cancellationToken);
            _logger.LogInformation("Outbound delivery {DeliveryId}. tenantId={TenantId}, operation=IntegrationDelivery, durationMs={DurationMs}, success=true", delivery.Id, delivery.CompanyId, attempt.DurationMs);
            TenantOperationalMetrics.RecordIntegrationDelivery(delivery.CompanyId, true);
            return new OutboundDispatchResult { Success = true, HttpStatusCode = httpResult.HttpStatusCode };
        }

        TenantOperationalMetrics.RecordIntegrationDelivery(delivery.CompanyId, false);
        if (delivery.AttemptCount >= delivery.MaxAttempts)
        {
            delivery.Status = OutboundIntegrationDelivery.Statuses.DeadLetter;
            delivery.NextRetryAtUtc = null;
            await _deliveryStore.UpdateDeliveryAsync(delivery, cancellationToken);
            _logger.LogWarning("Outbound delivery {DeliveryId} moved to dead-letter after {Attempts} attempts. tenantId={TenantId}, operation=IntegrationDelivery, durationMs={DurationMs}, success=false", delivery.Id, delivery.AttemptCount, delivery.CompanyId, attempt.DurationMs);
            return new OutboundDispatchResult { Success = false, ErrorMessage = httpResult.ErrorMessage, MovedToDeadLetter = true };
        }

        delivery.Status = OutboundIntegrationDelivery.Statuses.Failed;
        delivery.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(Math.Pow(2, delivery.AttemptCount));
        await _deliveryStore.UpdateDeliveryAsync(delivery, cancellationToken);
        _logger.LogWarning("Outbound delivery {DeliveryId} failed (will retry). tenantId={TenantId}, operation=IntegrationDelivery, durationMs={DurationMs}, success=false", delivery.Id, delivery.CompanyId, attempt.DurationMs);
        return new OutboundDispatchResult { Success = false, ErrorMessage = httpResult.ErrorMessage, HttpStatusCode = httpResult.HttpStatusCode };
    }

    public async Task<ReplayOutboundResult> ReplayAsync(ReplayOutboundRequest request, CancellationToken cancellationToken = default)
    {
        var (items, _) = await _deliveryStore.ListAsync(
            request.ConnectorEndpointId,
            request.CompanyId,
            request.EventType,
            null,
            request.FromUtc,
            request.ToUtc,
            0,
            request.MaxCount,
            cancellationToken);

        var list = items
            .Where(d => d.Status == OutboundIntegrationDelivery.Statuses.Failed || d.Status == OutboundIntegrationDelivery.Statuses.DeadLetter)
            .ToList();
        var errors = new List<string>();
        var dispatched = 0;
        var failed = 0;

        foreach (var d in list)
        {
            d.Status = OutboundIntegrationDelivery.Statuses.Replaying;
            d.IsReplay = true;
            await _deliveryStore.UpdateDeliveryAsync(d, cancellationToken);

            var result = await DispatchDeliveryAsync(d.Id, cancellationToken);
            if (result.Success)
                dispatched++;
            else
            {
                failed++;
                errors.Add($"{d.Id}: {result.ErrorMessage}");
            }
        }

        return new ReplayOutboundResult { Dispatched = dispatched, Failed = failed, Errors = errors };
    }
}
