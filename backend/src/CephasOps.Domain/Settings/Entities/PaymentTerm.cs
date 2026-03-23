using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

public class PaymentTerm : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DueDays { get; set; } = 30;
    public decimal DiscountPercent { get; set; } = 0;
    public int DiscountDays { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

