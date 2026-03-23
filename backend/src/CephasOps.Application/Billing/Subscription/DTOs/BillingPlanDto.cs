using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Application.Billing.Subscription.DTOs;

public class BillingPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public BillingCycle BillingCycle { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MYR";
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
