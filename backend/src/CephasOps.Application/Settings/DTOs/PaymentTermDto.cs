namespace CephasOps.Application.Settings.DTOs;

public class PaymentTermDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DueDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public int DiscountDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePaymentTermDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DueDays { get; set; } = 30;
    public decimal DiscountPercent { get; set; } = 0;
    public int DiscountDays { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdatePaymentTermDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? DueDays { get; set; }
    public decimal? DiscountPercent { get; set; }
    public int? DiscountDays { get; set; }
    public bool? IsActive { get; set; }
}
