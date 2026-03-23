using CephasOps.Domain.Companies.Entities;

namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Vertical DTO
/// </summary>
public class VerticalDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }

    public static VerticalDto FromEntity(Vertical vertical)
    {
        return new VerticalDto
        {
            Id = vertical.Id,
            CompanyId = vertical.CompanyId,
            Name = vertical.Name,
            Code = vertical.Code,
            Description = vertical.Description,
            IsActive = vertical.IsActive,
            DisplayOrder = vertical.DisplayOrder
        };
    }
}

/// <summary>
/// Create vertical request DTO
/// </summary>
public class CreateVerticalDto
{
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update vertical request DTO
/// </summary>
public class UpdateVerticalDto
{
    public Guid? CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
}


