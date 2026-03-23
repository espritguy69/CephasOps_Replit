using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

public class Warehouse : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int BinCount { get; set; } = 0;
    public decimal Capacity { get; set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    public decimal UtilizationPercent => Capacity > 0 ? Math.Round((CurrentStock / Capacity) * 100, 2) : 0;
    public bool IsActive { get; set; } = true;
}

