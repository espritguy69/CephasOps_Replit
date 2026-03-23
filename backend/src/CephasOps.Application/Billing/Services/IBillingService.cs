using CephasOps.Application.Billing.DTOs;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Billing service interface
/// </summary>
public interface IBillingService
{
    Task<List<InvoiceDto>> GetInvoicesAsync(Guid? companyId, string? status = null, Guid? partnerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteInvoiceAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    /// <summary>Deprecated: PDF generation now uses DocumentGenerationService via BillingController.</summary>
    Task<byte[]> GenerateInvoicePdfAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    /// <summary>Returns the invoice's CompanyId for SuperAdmin when company context is null.</summary>
    Task<Guid?> GetInvoiceCompanyIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a single invoice line (UnitPrice, Description, Quantity=1) from an order using BillingRatecard.
    /// Priority: exact partner rate → partner group rate → department rate → company default.
    /// </summary>
    Task<ResolvedInvoiceLineDto?> ResolveInvoiceLineFromOrderAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Build suggested invoice line items from orders using BillingRatecard resolution.
    /// Use when generating invoice lines automatically. Does not change CreateInvoiceDto behaviour.
    /// </summary>
    Task<BuildInvoiceLinesResult> BuildInvoiceLinesFromOrdersAsync(IReadOnlyList<Guid> orderIds, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default);
}
