namespace CephasOps.Application.Settings.DTOs;

public class BrandDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public int MaterialCount { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBrandDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
}

public class UpdateBrandDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
}

