namespace CephasOps.Application.Settings.DTOs;

public class CostCentreDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? MonthlyBudget { get; set; }
    public decimal CurrentSpend { get; set; }
    public int DepartmentCount { get; set; }
    public decimal UtilizationPercent { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCostCentreDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? MonthlyBudget { get; set; }
}

public class UpdateCostCentreDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? MonthlyBudget { get; set; }
    public bool IsActive { get; set; }
}

