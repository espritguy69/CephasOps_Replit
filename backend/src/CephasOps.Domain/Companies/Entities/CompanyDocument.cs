using CephasOps.Domain.Common;

namespace CephasOps.Domain.Companies.Entities;

/// <summary>
/// Company document entity - stores legal, banking, tax, contract documents per company
/// </summary>
public class CompanyDocument : CompanyScopedEntity
{
    /// <summary>
    /// Document category
    /// </summary>
    public string Category { get; set; } = string.Empty; // Legal, Banking, Tax, Contract, Tenancy, Insurance, Licence

    /// <summary>
    /// Document title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Document type (free text or lookup: "Bank Facility", "Tenancy Agreement", "SSM Certificate", etc.)
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// File ID reference to Files table
    /// </summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// Effective date (when document comes into effect)
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// Expiry date (critical for tenancy, insurance, licences)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Whether this is a critical document (things you NEVER want to lose)
    /// </summary>
    public bool IsCritical { get; set; } = false;

    /// <summary>
    /// Notes/description
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Related module (e.g., "Finance", "HR", "Solar", "ISP")
    /// </summary>
    public string? RelatedModule { get; set; }

    /// <summary>
    /// Related entity ID (polymorphic reference)
    /// e.g., link Vendor Agreement to Vendor, PPA to Project
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// User who created/uploaded this document
    /// </summary>
    public Guid CreatedByUserId { get; set; }
}

