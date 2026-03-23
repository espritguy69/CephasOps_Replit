using CephasOps.Application.Gpon.DTOs;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Gpon.Services;

/// <summary>
/// Service for GPON data deployment via Excel import/export
/// </summary>
public interface IGponDeploymentService
{
    /// <summary>
    /// Generate GPON deployment template Excel file
    /// </summary>
    Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate GPON deployment files without importing (dry-run)
    /// </summary>
    Task<GponDeploymentValidationResult> ValidateDeploymentAsync(
        IFormFileCollection files,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import GPON deployment from Excel files
    /// </summary>
    Task<GponDeploymentImportResult> ImportDeploymentAsync(
        IFormFileCollection files,
        GponDeploymentImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export existing GPON data to Excel
    /// </summary>
    Task<byte[]> ExportGponDataAsync(
        Guid? departmentId,
        CancellationToken cancellationToken = default);
}

