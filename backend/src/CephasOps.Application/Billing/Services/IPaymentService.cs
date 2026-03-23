using CephasOps.Application.Billing.DTOs;
using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Payment service interface
/// </summary>
public interface IPaymentService
{
    Task<List<PaymentDto>> GetPaymentsAsync(Guid? companyId, PaymentType? paymentType = null, DateTime? fromDate = null, DateTime? toDate = null, bool? isReconciled = null, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<PaymentDto> UpdatePaymentAsync(Guid id, UpdatePaymentDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeletePaymentAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PaymentDto> VoidPaymentAsync(Guid id, VoidPaymentDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PaymentDto> ReconcilePaymentAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PaymentSummaryDto> GetPaymentSummaryAsync(Guid? companyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<AccountingDashboardDto> GetAccountingDashboardAsync(Guid? companyId, CancellationToken cancellationToken = default);
    Task<string> GeneratePaymentNumberAsync(Guid? companyId, PaymentType paymentType, CancellationToken cancellationToken = default);
}

