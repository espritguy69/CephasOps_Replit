namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Partner DTO
/// </summary>
public class PartnerDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
    /// <summary>Short code for display and derived labels (e.g. TIME, CELCOM). Used with OrderCategory.Code for TIME-FTTH style labels.</summary>
    public string? Code { get; set; }
    public string PartnerType { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public string? BillingAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

