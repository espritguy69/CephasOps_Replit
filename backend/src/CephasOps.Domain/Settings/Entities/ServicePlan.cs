using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

public class ServicePlan : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProductTypeId { get; set; }
    public string? ProductTypeName { get; set; }
    public int SpeedMbps { get; set; } = 0;
    public decimal MonthlyPrice { get; set; }
    public decimal SetupFee { get; set; } = 0;
    public int ContractMonths { get; set; } = 24;
    public bool IsActive { get; set; } = true;
}

