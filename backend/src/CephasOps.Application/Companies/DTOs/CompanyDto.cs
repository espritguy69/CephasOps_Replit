namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Represents a company record exposed via the API layer.
/// </summary>
public class CompanyDto
{
    public Guid Id { get; set; }
    /// <summary>Phase 11: Parent tenant ID when company is tenant-scoped.</summary>
    public Guid? TenantId { get; set; }
    /// <summary>Phase 11: Tenant slug for display.</summary>
    public string? TenantSlug { get; set; }
    /// <summary>SaaS: Lifecycle state (Active, Suspended, Trial, etc.).</summary>
    public string Status { get; set; } = "Active";
    /// <summary>SaaS: Unique company/tenant code.</summary>
    public string? Code { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Vertical { get; set; } = string.Empty;
    public string? RegistrationNo { get; set; }
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Locale Settings
    public string DefaultTimezone { get; set; } = "Asia/Kuala_Lumpur";
    public string DefaultDateFormat { get; set; } = "dd/MM/yyyy";
    public string DefaultTimeFormat { get; set; } = "hh:mm a";
    public string DefaultCurrency { get; set; } = "MYR";
    public string DefaultLocale { get; set; } = "en-MY";
}


