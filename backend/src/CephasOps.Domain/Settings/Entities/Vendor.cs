using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Vendor Entity
/// Represents suppliers and vendors
/// </summary>
public class Vendor : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Contact information
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    
    // Address
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
    public string? Country { get; set; }
    
    // Payment terms
    public string? PaymentTerms { get; set; }
    public int? PaymentDueDays { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    
    // Audit
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

