using CephasOps.Domain.Integration.Entities;

namespace CephasOps.Application.Integration;

/// <summary>
/// Persistence for inbound webhook receipts. Used by webhook runtime and operator queries.
/// </summary>
public interface IInboundWebhookReceiptStore
{
    Task<InboundWebhookReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<InboundWebhookReceipt> Items, int TotalCount)> ListAsync(
        string? connectorKey,
        Guid? companyId,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task CreateAsync(InboundWebhookReceipt receipt, CancellationToken cancellationToken = default);
    Task UpdateAsync(InboundWebhookReceipt receipt, CancellationToken cancellationToken = default);
}
