using CephasOps.Domain.Common;

namespace CephasOps.Domain.Companies.Entities;

/// <summary>
/// Partner entity - represents partners like TIME, Celcom, etc.
/// </summary>
public class Partner : CompanyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Short code for display and derived labels (e.g. TIME, CELCOM).
    /// Used with OrderCategory.Code to form derivedPartnerCategoryLabel (e.g. TIME-FTTH). Display-only, not persisted as composite.
    /// </summary>
    public string? Code { get; set; }
    public string PartnerType { get; set; } = string.Empty; // Telco, Customer, Vendor, Landlord
    public Guid? GroupId { get; set; }
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public string? BillingAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}

