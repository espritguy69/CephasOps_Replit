using CephasOps.Application.Companies.DTOs;
using CephasOps.Domain.Companies.Enums;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Contract for company management operations.
/// </summary>
public interface ICompanyService
{
    Task<CompanyDto?> SetCompanyStatusAsync(Guid companyId, CompanyStatus status, CancellationToken cancellationToken = default);

    Task<List<CompanyDto>> GetCompaniesAsync(
        bool? isActive = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CompanyDto> CreateCompanyAsync(
        CreateCompanyDto dto,
        CancellationToken cancellationToken = default);

    Task<CompanyDto> UpdateCompanyAsync(
        Guid id,
        UpdateCompanyDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteCompanyAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}


