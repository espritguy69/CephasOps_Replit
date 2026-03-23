namespace CephasOps.Application.Settings.DTOs;

public class ServicePlanDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProductTypeId { get; set; }
    public string? ProductTypeName { get; set; }
    public int SpeedMbps { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal SetupFee { get; set; }
    public int ContractMonths { get; set; }
    public bool IsActive { get; set; }
}

