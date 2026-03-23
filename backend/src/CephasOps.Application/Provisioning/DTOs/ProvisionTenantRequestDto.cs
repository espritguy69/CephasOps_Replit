using System.ComponentModel.DataAnnotations;

namespace CephasOps.Application.Provisioning.DTOs;

/// <summary>Request to provision a new tenant (company + subscription + defaults + admin user).</summary>
public class ProvisionTenantRequestDto
{
    [Required]
    [MaxLength(500)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CompanyCode { get; set; } = string.Empty;

    /// <summary>URL-safe slug for the tenant (e.g. for subdomains). Derived from CompanyCode if not set.</summary>
    [MaxLength(100)]
    public string? Slug { get; set; }

    [Required]
    [MaxLength(200)]
    public string AdminFullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Initial password for tenant admin. If empty, a temporary password can be generated (MustChangePassword = true).</summary>
    [MaxLength(500)]
    public string? AdminPassword { get; set; }

    /// <summary>Billing plan slug (e.g. trial, starter). Optional; if not set a default trial subscription is created.</summary>
    [MaxLength(100)]
    public string? PlanSlug { get; set; }

    /// <summary>Trial period in days when creating a trial subscription. Default 14.</summary>
    public int? TrialDays { get; set; }

    [MaxLength(100)]
    public string? DefaultTimezone { get; set; }

    [MaxLength(20)]
    public string? DefaultLocale { get; set; }

    /// <summary>Initial company status. Default Active; use Trial for trial tenants.</summary>
    public string? InitialStatus { get; set; }
}
