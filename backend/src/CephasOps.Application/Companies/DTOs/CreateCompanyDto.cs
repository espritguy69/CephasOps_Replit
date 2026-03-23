using System.ComponentModel.DataAnnotations;

namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Request payload for creating a new company.
/// </summary>
public class CreateCompanyDto
{
    [Required]
    [MaxLength(500)]
    public string LegalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ShortName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Vertical { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RegistrationNo { get; set; }

    [MaxLength(100)]
    public string? TaxId { get; set; }

    [MaxLength(2000)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    // Locale Settings (with Malaysian defaults)
    [MaxLength(100)]
    public string DefaultTimezone { get; set; } = "Asia/Kuala_Lumpur";

    [MaxLength(50)]
    public string DefaultDateFormat { get; set; } = "dd/MM/yyyy";

    [MaxLength(50)]
    public string DefaultTimeFormat { get; set; } = "hh:mm a";

    [MaxLength(10)]
    public string DefaultCurrency { get; set; } = "MYR";

    [MaxLength(20)]
    public string DefaultLocale { get; set; } = "en-MY";
}


