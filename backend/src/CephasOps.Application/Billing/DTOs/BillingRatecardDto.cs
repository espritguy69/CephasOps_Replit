namespace CephasOps.Application.Billing.DTOs;

/// <summary>
/// Billing ratecard DTO - defines what partner pays us
/// Per PARTNER_MODULE.md: Rate lookup uses partnerGroupId first, then partnerId override
/// </summary>
public class BillingRatecardDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? PartnerGroupId { get; set; }
    public string? PartnerGroupName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? OrderTypeId { get; set; }
    public string? OrderTypeName { get; set; }
    public string? ServiceCategory { get; set; } // FTTH, FTTO, FTTR, FTTC
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; } // Prelaid, Non-Prelaid, SDU, RDF Pole
    public string? BuildingType { get; set; } // Legacy
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create billing ratecard request DTO
/// </summary>
public class CreateBillingRatecardDto
{
    public Guid? DepartmentId { get; set; }
    public Guid? PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OrderTypeId { get; set; }
    public string? ServiceCategory { get; set; } // FTTH, FTTO, FTTR, FTTC
    public Guid? InstallationMethodId { get; set; }
    public string? BuildingType { get; set; } // Legacy
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// Update billing ratecard request DTO
/// </summary>
public class UpdateBillingRatecardDto
{
    public Guid? DepartmentId { get; set; }
    public Guid? PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OrderTypeId { get; set; }
    public string? ServiceCategory { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string? BuildingType { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public decimal? TaxRate { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
