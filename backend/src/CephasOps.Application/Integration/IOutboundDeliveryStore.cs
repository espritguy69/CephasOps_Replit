using CephasOps.Domain.Integration.Entities;

namespace CephasOps.Application.Integration;

/// <summary>
/// Persistence for outbound deliveries and attempts. Used by the outbound bus and operator queries.
/// </summary>
public interface IOutboundDeliveryStore
{
    Task<OutboundIntegrationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboundIntegrationDelivery>> GetPendingOrRetryAsync(int maxCount, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<OutboundIntegrationDelivery> Items, int TotalCount)> ListAsync(
        Guid? connectorEndpointId,
        Guid? companyId,
        string? eventType,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<OutboundIntegrationDelivery?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task CreateDeliveryAsync(OutboundIntegrationDelivery delivery, CancellationToken cancellationToken = default);
    Task UpdateDeliveryAsync(OutboundIntegrationDelivery delivery, CancellationToken cancellationToken = default);
    Task AddAttemptAsync(OutboundIntegrationAttempt attempt, CancellationToken cancellationToken = default);
}
