using CephasOps.Application.Billing.DTOs;
using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Supplier Invoice service interface
/// </summary>
public interface ISupplierInvoiceService
{
    Task<List<SupplierInvoiceDto>> GetSupplierInvoicesAsync(Guid? companyId, SupplierInvoiceStatus? status = null, string? supplierName = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<SupplierInvoiceDto?> GetSupplierInvoiceByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<SupplierInvoiceDto> CreateSupplierInvoiceAsync(CreateSupplierInvoiceDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<SupplierInvoiceDto> UpdateSupplierInvoiceAsync(Guid id, UpdateSupplierInvoiceDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteSupplierInvoiceAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<SupplierInvoiceDto> ApproveSupplierInvoiceAsync(Guid id, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<SupplierInvoiceSummaryDto> GetSupplierInvoiceSummaryAsync(Guid? companyId, CancellationToken cancellationToken = default);
    Task<List<SupplierInvoiceDto>> GetOverdueInvoicesAsync(Guid? companyId, CancellationToken cancellationToken = default);
}

