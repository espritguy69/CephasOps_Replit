using CephasOps.Domain.Companies.Enums;

namespace CephasOps.Domain.Companies.Entities;

/// <summary>
/// Company entity - represents a legal entity using CephasOps. Belongs to a Tenant (Phase 11).
/// </summary>
public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent tenant for SaaS isolation; null for legacy/on-prem.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>SaaS: Lifecycle state (Active, Suspended, Trial, etc.).</summary>
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    /// <summary>Navigation to tenant (Phase 11).</summary>
    public Domain.Tenants.Entities.Tenant? Tenant { get; set; }

    public string LegalName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? RegistrationNo { get; set; }
    public string? TaxId { get; set; }
    public string Vertical { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>SaaS: Unique code for the tenant/company (e.g. CEPHAS). Used for default tenant resolution.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>SaaS: Optional link to tenant subscription for billing.</summary>
    public Guid? SubscriptionId { get; set; }

    // Locale Settings
    /// <summary>
    /// Default timezone for the company (IANA timezone identifier, e.g., "Asia/Kuala_Lumpur")
    /// </summary>
    public string DefaultTimezone { get; set; } = "Asia/Kuala_Lumpur";

    /// <summary>
    /// Default date format for display (e.g., "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd")
    /// </summary>
    public string DefaultDateFormat { get; set; } = "dd/MM/yyyy";

    /// <summary>
    /// Default time format (e.g., "HH:mm" for 24-hour, "hh:mm a" for 12-hour)
    /// </summary>
    public string DefaultTimeFormat { get; set; } = "hh:mm a";

    /// <summary>
    /// Default currency code (ISO 4217, e.g., "MYR", "USD")
    /// </summary>
    public string DefaultCurrency { get; set; } = "MYR";

    /// <summary>
    /// Default locale for number and currency formatting (e.g., "en-MY", "en-US")
    /// </summary>
    public string DefaultLocale { get; set; } = "en-MY";
}

