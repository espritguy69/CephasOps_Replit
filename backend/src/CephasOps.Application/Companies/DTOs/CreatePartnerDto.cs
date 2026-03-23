namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Create partner request DTO
/// </summary>
public class CreatePartnerDto
{
    public Guid? CompanyId { get; set; } // Optional: if not provided, use from user context
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public string Name { get; set; } = string.Empty;
    /// <summary>Short code for derived labels (e.g. TIME, CELCOM). Optional.</summary>
    public string? Code { get; set; }
    public string PartnerType { get; set; } = string.Empty; // Telco, Customer, Vendor, Landlord
    public Guid? GroupId { get; set; }
    public string? BillingAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}

