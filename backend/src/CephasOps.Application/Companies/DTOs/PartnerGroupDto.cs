namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Partner Group DTO
/// </summary>
public class PartnerGroupDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

