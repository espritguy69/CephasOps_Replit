using System.ComponentModel.DataAnnotations;

namespace CephasOps.Application.Companies.DTOs;

/// <summary>
/// Request payload for updating an existing company.
/// </summary>
public class UpdateCompanyDto
{
    [MaxLength(500)]
    public string? LegalName { get; set; }

    [MaxLength(100)]
    public string? ShortName { get; set; }

    [MaxLength(50)]
    public string? Vertical { get; set; }

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

    public bool? IsActive { get; set; }

    // Locale Settings
    [MaxLength(100)]
    public string? DefaultTimezone { get; set; }

    [MaxLength(50)]
    public string? DefaultDateFormat { get; set; }

    [MaxLength(50)]
    public string? DefaultTimeFormat { get; set; }

    [MaxLength(10)]
    public string? DefaultCurrency { get; set; }

    [MaxLength(20)]
    public string? DefaultLocale { get; set; }
}


