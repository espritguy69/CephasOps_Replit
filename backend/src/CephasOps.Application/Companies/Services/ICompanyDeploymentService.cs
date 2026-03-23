using CephasOps.Application.Companies.DTOs;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Service for company deployment via Excel import/export
/// </summary>
public interface ICompanyDeploymentService
{
    /// <summary>
    /// Generate deployment template Excel file
    /// </summary>
    Task<byte[]> GenerateTemplateAsync(string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate deployment files without importing (dry-run)
    /// </summary>
    Task<DeploymentValidationResult> ValidateDeploymentAsync(
        IFormFileCollection files,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import company deployment from Excel files
    /// </summary>
    Task<DeploymentImportResult> ImportDeploymentAsync(
        IFormFileCollection files,
        DeploymentImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export existing company data to Excel
    /// </summary>
    Task<byte[]> ExportCompanyAsync(
        Guid? companyId,
        string format,
        CancellationToken cancellationToken = default);
}

