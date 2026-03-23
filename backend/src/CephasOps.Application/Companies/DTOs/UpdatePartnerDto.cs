namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Update partner request DTO
/// </summary>
public class UpdatePartnerDto
{
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public string? Name { get; set; }
    /// <summary>Short code for derived labels (e.g. TIME, CELCOM). Optional.</summary>
    public string? Code { get; set; }
    public string? PartnerType { get; set; }
    public Guid? GroupId { get; set; }
    public string? BillingAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool? IsActive { get; set; }
}

