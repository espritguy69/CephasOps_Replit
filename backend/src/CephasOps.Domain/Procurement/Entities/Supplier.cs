using CephasOps.Domain.Common;

namespace CephasOps.Domain.Procurement.Entities;

/// <summary>
/// Supplier entity for procurement
/// </summary>
public class Supplier : CompanyScopedEntity
{
    /// <summary>
    /// Supplier code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Business registration number
    /// </summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Tax identification number
    /// </summary>
    public string? TaxNumber { get; set; }

    /// <summary>
    /// Contact person name
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Contact email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Fax number
    /// </summary>
    public string? Fax { get; set; }

    /// <summary>
    /// Address line 1
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postcode
    /// </summary>
    public string? Postcode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; } = "Malaysia";

    /// <summary>
    /// Bank name
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Bank account number
    /// </summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Bank account name
    /// </summary>
    public string? BankAccountName { get; set; }

    /// <summary>
    /// Default payment terms
    /// </summary>
    public string? PaymentTerms { get; set; }

    /// <summary>
    /// Credit limit
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Whether supplier is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User ID who created this supplier
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

