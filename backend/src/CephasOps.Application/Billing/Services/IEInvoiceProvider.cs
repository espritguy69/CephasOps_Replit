namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Interface for e-invoice providers (MyInvois, etc.)
/// Moved to Domain - this is now just a re-export for backward compatibility.
/// All methods are inherited from CephasOps.Domain.Billing.IEInvoiceProvider.
/// </summary>
public interface IEInvoiceProvider : CephasOps.Domain.Billing.IEInvoiceProvider
{
    // All methods are inherited from the base interface in Domain
}

