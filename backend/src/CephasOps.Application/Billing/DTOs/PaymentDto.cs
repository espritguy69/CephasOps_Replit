using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Application.Billing.DTOs;

/// <summary>
/// Payment DTO
/// </summary>
public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public string PaymentTypeName => PaymentType.ToString();
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public string PayerPayeeName { get; set; } = string.Empty;
    public string? BankAccount { get; set; }
    public string? BankReference { get; set; }
    public string? ChequeNumber { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public Guid? PnlTypeId { get; set; }
    public string? PnlTypeName { get; set; }
    public Guid? CostCentreId { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public bool IsVoided { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create Payment request DTO
/// </summary>
public class CreatePaymentDto
{
    /// <summary>
    /// Optional idempotency key. When provided, repeated requests with the same key (same company) return the existing payment instead of creating a duplicate.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    public PaymentType PaymentType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public string PayerPayeeName { get; set; } = string.Empty;
    public string? BankAccount { get; set; }
    public string? BankReference { get; set; }
    public string? ChequeNumber { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public Guid? PnlTypeId { get; set; }
    public Guid? CostCentreId { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
}

/// <summary>
/// Update Payment request DTO
/// </summary>
public class UpdatePaymentDto
{
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal? Amount { get; set; }
    public string? PayerPayeeName { get; set; }
    public string? BankAccount { get; set; }
    public string? BankReference { get; set; }
    public string? ChequeNumber { get; set; }
    public Guid? PnlTypeId { get; set; }
    public Guid? CostCentreId { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
}

/// <summary>
/// Void Payment request DTO
/// </summary>
public class VoidPaymentDto
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Payment summary for dashboard
/// </summary>
public class PaymentSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCashFlow { get; set; }
    public int TotalPayments { get; set; }
    public int UnreconciledPayments { get; set; }
    public decimal IncomeThisMonth { get; set; }
    public decimal ExpensesThisMonth { get; set; }
    public List<PaymentsByMethodDto> ByMethod { get; set; } = new();
    public List<MonthlyPaymentSummaryDto> MonthlyTrend { get; set; } = new();
}

/// <summary>
/// Payments grouped by method
/// </summary>
public class PaymentsByMethodDto
{
    public PaymentMethod Method { get; set; }
    public string MethodName => Method.ToString();
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Monthly payment summary for trends
/// </summary>
public class MonthlyPaymentSummaryDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Net { get; set; }
}

/// <summary>
/// Accounting Dashboard DTO
/// </summary>
public class AccountingDashboardDto
{
    public PaymentSummaryDto PaymentSummary { get; set; } = new();
    public SupplierInvoiceSummaryDto SupplierInvoiceSummary { get; set; } = new();
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public List<PaymentDto> RecentPayments { get; set; } = new();
    public List<SupplierInvoiceDto> OverdueInvoices { get; set; } = new();
}

