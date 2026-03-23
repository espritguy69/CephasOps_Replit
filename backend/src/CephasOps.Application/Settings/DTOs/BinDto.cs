namespace CephasOps.Application.Settings.DTOs;

public class BinDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string Section { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Level { get; set; }
    public decimal Capacity { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal UtilizationPercent { get; set; }
    public bool IsActive { get; set; }
}

