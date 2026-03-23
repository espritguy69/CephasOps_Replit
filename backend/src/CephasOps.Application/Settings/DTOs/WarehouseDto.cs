namespace CephasOps.Application.Settings.DTOs;

public class WarehouseDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int BinCount { get; set; }
    public decimal Capacity { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal UtilizationPercent { get; set; }
    public bool IsActive { get; set; }
}

