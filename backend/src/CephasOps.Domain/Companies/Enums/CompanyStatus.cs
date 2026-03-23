namespace CephasOps.Domain.Companies.Enums;

/// <summary>SaaS: Lifecycle state for a company (tenant).</summary>
public enum CompanyStatus
{
    /// <summary>Provisioning in progress or not yet activated.</summary>
    PendingProvisioning = 0,

    /// <summary>Active and operational.</summary>
    Active = 1,

    /// <summary>Temporarily suspended (e.g. non-payment).</summary>
    Suspended = 2,

    /// <summary>Permanently disabled.</summary>
    Disabled = 3,

    /// <summary>Trial period.</summary>
    Trial = 4,

    /// <summary>Archived for record-keeping.</summary>
    Archived = 5
}
