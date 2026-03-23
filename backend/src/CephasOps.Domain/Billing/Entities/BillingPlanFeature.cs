namespace CephasOps.Domain.Billing.Entities;

/// <summary>Feature key enabled by a billing plan (e.g. Automation, Reports, MultiDepartment).</summary>
public class BillingPlanFeature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BillingPlanId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BillingPlan? BillingPlan { get; set; }
}
