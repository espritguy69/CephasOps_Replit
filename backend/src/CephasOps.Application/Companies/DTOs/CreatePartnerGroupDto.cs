namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// DTO for creating a new partner group
/// </summary>
public class CreatePartnerGroupDto
{
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
}

