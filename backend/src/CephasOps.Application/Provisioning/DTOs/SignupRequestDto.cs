using System.ComponentModel.DataAnnotations;

namespace CephasOps.Application.Provisioning.DTOs;

/// <summary>Public self-service signup request. Validated for email, company code and slug uniqueness.</summary>
public class SignupRequestDto
{
    [Required]
    [MaxLength(500)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CompanyCode { get; set; } = string.Empty;

    /// <summary>URL-safe tenant slug. Defaults from CompanyCode if not set.</summary>
    [MaxLength(100)]
    public string? Slug { get; set; }

    [Required]
    [MaxLength(200)]
    public string AdminFullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(500)]
    public string AdminPassword { get; set; } = string.Empty;
}
