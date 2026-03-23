using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

public class Bin : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string Section { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Level { get; set; }
    public decimal Capacity { get; set; }
    public decimal CurrentStock { get; set; } = 0;
    public decimal UtilizationPercent => Capacity > 0 ? Math.Round((CurrentStock / Capacity) * 100, 2) : 0;
    public bool IsActive { get; set; } = true;
}

