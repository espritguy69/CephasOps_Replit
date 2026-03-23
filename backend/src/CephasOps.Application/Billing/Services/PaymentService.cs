using System.Text.Json;
using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Commands;
using CephasOps.Application.Common;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Payment service implementation
/// </summary>
public class PaymentService : IPaymentService
{
    private const string IdempotencyCommandType = "CreatePayment";
    private static readonly JsonSerializerOptions IdempotencyJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ApplicationDbContext _context;
    private readonly ISupplierInvoiceService _supplierInvoiceService;
    private readonly ICommandProcessingLogStore _idempotencyStore;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        ISupplierInvoiceService supplierInvoiceService,
        ICommandProcessingLogStore idempotencyStore,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _supplierInvoiceService = supplierInvoiceService;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    public async Task<List<PaymentDto>> GetPaymentsAsync(Guid? companyId, PaymentType? paymentType = null, DateTime? fromDate = null, DateTime? toDate = null, bool? isReconciled = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.SupplierInvoice)
            .Where(p => !p.IsVoided);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        if (paymentType.HasValue)
        {
            query = query.Where(p => p.PaymentType == paymentType.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= toDate.Value);
        }

        if (isReconciled.HasValue)
        {
            query = query.Where(p => p.IsReconciled == isReconciled.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.SupplierInvoice)
            .Where(p => p.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        var payment = await query.FirstOrDefaultAsync(cancellationToken);
        return payment != null ? MapToDto(payment) : null;
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("CreatePayment");
        FinancialIsolationGuard.RequireCompany(companyId, "CreatePayment");

        var idempotencyKey = BuildPaymentIdempotencyKey(dto.IdempotencyKey, companyId);
        if (idempotencyKey != null)
        {
            var existing = await _idempotencyStore.TryGetCompletedResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
            if (existing?.ResultJson != null)
            {
                var paymentId = DeserializePaymentIdFromResult(existing.ResultJson);
                if (paymentId.HasValue)
                {
                    var cached = await GetPaymentByIdAsync(paymentId.Value, companyId, cancellationToken).ConfigureAwait(false);
                    if (cached != null)
                    {
                        _logger.LogInformation("CreatePayment idempotency reuse. key={Key}, paymentId={PaymentId}", idempotencyKey, paymentId);
                        return cached;
                    }
                }
            }

            var executionId = Guid.NewGuid();
            var claimed = await _idempotencyStore.TryClaimAsync(executionId, idempotencyKey, IdempotencyCommandType, null, null, cancellationToken).ConfigureAwait(false);
            if (!claimed)
            {
                var retryExisting = await _idempotencyStore.TryGetCompletedResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
                if (retryExisting?.ResultJson != null)
                {
                    var paymentId = DeserializePaymentIdFromResult(retryExisting.ResultJson);
                    if (paymentId.HasValue)
                    {
                        var cached = await GetPaymentByIdAsync(paymentId.Value, companyId, cancellationToken).ConfigureAwait(false);
                        if (cached != null)
                        {
                            _logger.LogInformation("CreatePayment idempotency reuse (after claim conflict). key={Key}, paymentId={PaymentId}", idempotencyKey, paymentId);
                            return cached;
                        }
                    }
                }
                throw new InvalidOperationException("CreatePayment: Another request with the same idempotency key is in progress or failed. Retry later.");
            }

            try
            {
                var payment = await CreatePaymentCoreAsync(dto, companyId, userId, cancellationToken).ConfigureAwait(false);
                var resultJson = JsonSerializer.Serialize(new { PaymentId = payment.Id }, IdempotencyJsonOptions);
                await _idempotencyStore.MarkCompletedAsync(executionId, resultJson, cancellationToken).ConfigureAwait(false);
                return payment;
            }
            catch (Exception ex)
            {
                await _idempotencyStore.MarkFailedAsync(executionId, ex.Message, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        return await CreatePaymentCoreAsync(dto, companyId, userId, cancellationToken).ConfigureAwait(false);
    }

    private static string? BuildPaymentIdempotencyKey(string? clientKey, Guid? companyId)
    {
        if (string.IsNullOrWhiteSpace(clientKey) || !companyId.HasValue || companyId.Value == Guid.Empty)
            return null;
        return $"{companyId.Value:N}:{IdempotencyCommandType}:{clientKey.Trim()}";
    }

    private static Guid? DeserializePaymentIdFromResult(string resultJson)
    {
        try
        {
            var doc = JsonDocument.Parse(resultJson);
            if (doc.RootElement.TryGetProperty("paymentId", out var prop) && prop.TryGetGuid(out var id))
                return id;
        }
        catch { /* ignore */ }
        return null;
    }

    private async Task<PaymentDto> CreatePaymentCoreAsync(CreatePaymentDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var paymentNumber = await GeneratePaymentNumberAsync(companyId, dto.PaymentType, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            PaymentNumber = paymentNumber,
            PaymentType = dto.PaymentType,
            PaymentMethod = dto.PaymentMethod,
            PaymentDate = dto.PaymentDate,
            Amount = dto.Amount,
            Currency = dto.Currency,
            PayerPayeeName = dto.PayerPayeeName.Trim(),
            BankAccount = dto.BankAccount,
            BankReference = dto.BankReference,
            ChequeNumber = dto.ChequeNumber,
            InvoiceId = dto.InvoiceId,
            SupplierInvoiceId = dto.SupplierInvoiceId,
            PnlTypeId = dto.PnlTypeId,
            CostCentreId = dto.CostCentreId,
            Description = dto.Description,
            Notes = dto.Notes,
            AttachmentPath = dto.AttachmentPath,
            IsReconciled = false,
            CreatedByUserId = userId,
            IsVoided = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        // Update linked invoice if applicable (tenant-safe: scope by companyId)
        if (dto.SupplierInvoiceId.HasValue && dto.PaymentType == PaymentType.Expense)
        {
            var supplierInvoice = await _context.SupplierInvoices
                .FirstOrDefaultAsync(i => i.Id == dto.SupplierInvoiceId.Value && i.CompanyId == companyId, cancellationToken);
            if (supplierInvoice != null)
            {
                supplierInvoice.AmountPaid += dto.Amount;
                supplierInvoice.OutstandingAmount = supplierInvoice.TotalAmount - supplierInvoice.AmountPaid;
                if (supplierInvoice.OutstandingAmount <= 0)
                {
                    supplierInvoice.Status = SupplierInvoiceStatus.Paid;
                    supplierInvoice.PaidAt = DateTime.UtcNow;
                }
                else if (supplierInvoice.AmountPaid > 0)
                {
                    supplierInvoice.Status = SupplierInvoiceStatus.PartiallyPaid;
                }
                supplierInvoice.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (dto.InvoiceId.HasValue && dto.PaymentType == PaymentType.Income)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId.Value && i.CompanyId == companyId, cancellationToken);
            if (invoice != null)
            {
                // Update customer invoice paid amount (assuming there's a field for this)
                invoice.PaidAt = DateTime.UtcNow;
                invoice.Status = "Paid";
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment created. tenantId={TenantId}, paymentId={PaymentId}, operation=CreatePayment, success=true", companyId, payment.Id);

        return MapToDto(payment);
    }

    public async Task<PaymentDto> UpdatePaymentAsync(Guid id, UpdatePaymentDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.SupplierInvoice)
            .Where(p => p.Id == id && !p.IsVoided);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        var payment = await query.FirstOrDefaultAsync(cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {id} not found");
        }

        if (payment.IsReconciled)
        {
            throw new InvalidOperationException("Cannot update a reconciled payment.");
        }

        if (dto.PaymentMethod.HasValue) payment.PaymentMethod = dto.PaymentMethod.Value;
        if (dto.PaymentDate.HasValue) payment.PaymentDate = dto.PaymentDate.Value;
        if (dto.Amount.HasValue) payment.Amount = dto.Amount.Value;
        if (!string.IsNullOrEmpty(dto.PayerPayeeName)) payment.PayerPayeeName = dto.PayerPayeeName.Trim();
        if (dto.BankAccount != null) payment.BankAccount = dto.BankAccount;
        if (dto.BankReference != null) payment.BankReference = dto.BankReference;
        if (dto.ChequeNumber != null) payment.ChequeNumber = dto.ChequeNumber;
        if (dto.PnlTypeId.HasValue) payment.PnlTypeId = dto.PnlTypeId;
        if (dto.CostCentreId.HasValue) payment.CostCentreId = dto.CostCentreId;
        if (dto.Description != null) payment.Description = dto.Description;
        if (dto.Notes != null) payment.Notes = dto.Notes;
        if (dto.AttachmentPath != null) payment.AttachmentPath = dto.AttachmentPath;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment updated: {PaymentId}", id);

        return MapToDto(payment);
    }

    public async Task DeletePaymentAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.Where(p => p.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        var payment = await query.FirstOrDefaultAsync(cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {id} not found");
        }

        if (payment.IsReconciled)
        {
            throw new InvalidOperationException("Cannot delete a reconciled payment. Void it instead.");
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment deleted. tenantId={TenantId}, paymentId={PaymentId}, operation=DeletePayment, success=true", companyId, id);
    }

    public async Task<PaymentDto> VoidPaymentAsync(Guid id, VoidPaymentDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.SupplierInvoice)
            .Where(p => p.Id == id && !p.IsVoided);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        var payment = await query.FirstOrDefaultAsync(cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {id} not found or already voided");
        }

        payment.IsVoided = true;
        payment.VoidReason = dto.Reason;
        payment.VoidedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        // Reverse invoice payment if linked
        if (payment.SupplierInvoiceId.HasValue && payment.SupplierInvoice != null)
        {
            payment.SupplierInvoice.AmountPaid -= payment.Amount;
            payment.SupplierInvoice.OutstandingAmount = payment.SupplierInvoice.TotalAmount - payment.SupplierInvoice.AmountPaid;
            if (payment.SupplierInvoice.OutstandingAmount > 0)
            {
                payment.SupplierInvoice.Status = payment.SupplierInvoice.AmountPaid > 0
                    ? SupplierInvoiceStatus.PartiallyPaid
                    : SupplierInvoiceStatus.Approved;
                payment.SupplierInvoice.PaidAt = null;
            }
            payment.SupplierInvoice.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment voided: {PaymentId}, Reason: {Reason}", id, dto.Reason);

        return MapToDto(payment);
    }

    public async Task<PaymentDto> ReconcilePaymentAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.SupplierInvoice)
            .Where(p => p.Id == id && !p.IsVoided);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        var payment = await query.FirstOrDefaultAsync(cancellationToken);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {id} not found");
        }

        payment.IsReconciled = true;
        payment.ReconciledAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment reconciled. tenantId={TenantId}, paymentId={PaymentId}, operation=ReconcilePayment, success=true", companyId, id);

        return MapToDto(payment);
    }

    public async Task<PaymentSummaryDto> GetPaymentSummaryAsync(Guid? companyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.Where(p => !p.IsVoided);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= toDate.Value);
        }

        var payments = await query.ToListAsync(cancellationToken);

        var thisMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var lastSixMonths = Enumerable.Range(0, 6)
            .Select(i => DateTime.UtcNow.AddMonths(-i).ToString("yyyy-MM"))
            .Reverse()
            .ToList();

        return new PaymentSummaryDto
        {
            TotalIncome = payments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount),
            TotalExpenses = payments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount),
            NetCashFlow = payments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount) -
                          payments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount),
            TotalPayments = payments.Count,
            UnreconciledPayments = payments.Count(p => !p.IsReconciled),
            IncomeThisMonth = payments.Where(p => p.PaymentType == PaymentType.Income && p.PaymentDate.ToString("yyyy-MM") == thisMonth).Sum(p => p.Amount),
            ExpensesThisMonth = payments.Where(p => p.PaymentType == PaymentType.Expense && p.PaymentDate.ToString("yyyy-MM") == thisMonth).Sum(p => p.Amount),
            ByMethod = payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new PaymentsByMethodDto
                {
                    Method = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList(),
            MonthlyTrend = lastSixMonths.Select(period =>
            {
                var monthPayments = payments.Where(p => p.PaymentDate.ToString("yyyy-MM") == period).ToList();
                return new MonthlyPaymentSummaryDto
                {
                    Period = period,
                    Income = monthPayments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount),
                    Expenses = monthPayments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount),
                    Net = monthPayments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount) -
                          monthPayments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount)
                };
            }).ToList()
        };
    }

    public async Task<AccountingDashboardDto> GetAccountingDashboardAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        var paymentSummary = await GetPaymentSummaryAsync(companyId, cancellationToken: cancellationToken);
        var supplierInvoiceSummary = await _supplierInvoiceService.GetSupplierInvoiceSummaryAsync(companyId, cancellationToken);
        var overdueInvoices = await _supplierInvoiceService.GetOverdueInvoicesAsync(companyId, cancellationToken);

        // Get receivables (outstanding customer invoices)
        var receivablesQuery = _context.Invoices
            .Where(i => i.Status != "Paid" && i.Status != "Cancelled");
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            receivablesQuery = receivablesQuery.Where(i => i.CompanyId == companyId.Value);
        }
        var totalReceivables = await receivablesQuery.SumAsync(i => i.TotalAmount, cancellationToken);

        // Get recent payments
        var recentPaymentsQuery = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.SupplierInvoice)
            .Where(p => !p.IsVoided)
            .OrderByDescending(p => p.PaymentDate)
            .Take(10);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            recentPaymentsQuery = recentPaymentsQuery.Where(p => p.CompanyId == companyId.Value).OrderByDescending(p => p.PaymentDate).Take(10);
        }
        var recentPayments = await recentPaymentsQuery.ToListAsync(cancellationToken);

        return new AccountingDashboardDto
        {
            PaymentSummary = paymentSummary,
            SupplierInvoiceSummary = supplierInvoiceSummary,
            TotalReceivables = totalReceivables,
            TotalPayables = supplierInvoiceSummary.TotalOutstanding,
            RecentPayments = recentPayments.Select(MapToDto).ToList(),
            OverdueInvoices = overdueInvoices.Take(5).ToList()
        };
    }

    public async Task<string> GeneratePaymentNumberAsync(Guid? companyId, PaymentType paymentType, CancellationToken cancellationToken = default)
    {
        var prefix = paymentType == PaymentType.Income ? "REC" : "PAY";
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");

        var query = _context.Payments
            .Where(p => p.PaymentNumber.StartsWith($"{prefix}-{yearMonth}"));

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        var lastPayment = await query
            .OrderByDescending(p => p.PaymentNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (lastPayment != null)
        {
            var parts = lastPayment.PaymentNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{yearMonth}-{nextNumber:D4}";
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            CompanyId = payment.CompanyId,
            PaymentNumber = payment.PaymentNumber,
            PaymentType = payment.PaymentType,
            PaymentMethod = payment.PaymentMethod,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PayerPayeeName = payment.PayerPayeeName,
            BankAccount = payment.BankAccount,
            BankReference = payment.BankReference,
            ChequeNumber = payment.ChequeNumber,
            InvoiceId = payment.InvoiceId,
            InvoiceNumber = payment.Invoice?.InvoiceNumber,
            SupplierInvoiceId = payment.SupplierInvoiceId,
            SupplierInvoiceNumber = payment.SupplierInvoice?.InvoiceNumber,
            PnlTypeId = payment.PnlTypeId,
            CostCentreId = payment.CostCentreId,
            Description = payment.Description,
            Notes = payment.Notes,
            AttachmentPath = payment.AttachmentPath,
            IsReconciled = payment.IsReconciled,
            ReconciledAt = payment.ReconciledAt,
            CreatedByUserId = payment.CreatedByUserId,
            IsVoided = payment.IsVoided,
            VoidReason = payment.VoidReason,
            VoidedAt = payment.VoidedAt,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}

