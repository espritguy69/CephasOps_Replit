using CephasOps.Application.Departments.DTOs;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Departments.Services;

/// <summary>
/// Service for department deployment via Excel import/export
/// Supports GPON, CWO, NWO, and future departments
/// </summary>
public interface IDepartmentDeploymentService
{
    /// <summary>
    /// Get available department deployment configurations
    /// </summary>
    Task<List<DepartmentDeploymentConfig>> GetAvailableConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get deployment configuration for a specific department
    /// </summary>
    Task<DepartmentDeploymentConfig?> GetConfigurationAsync(string departmentCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate department deployment template Excel file
    /// </summary>
    Task<byte[]> GenerateTemplateAsync(string departmentCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate department deployment files without importing (dry-run)
    /// </summary>
    Task<DepartmentDeploymentValidationResult> ValidateDeploymentAsync(
        IFormFileCollection files,
        string departmentCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import department deployment from Excel files
    /// </summary>
    Task<DepartmentDeploymentImportResult> ImportDeploymentAsync(
        IFormFileCollection files,
        DepartmentDeploymentImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export existing department data to Excel
    /// </summary>
    Task<byte[]> ExportDepartmentDataAsync(
        string departmentCode,
        Guid? departmentId,
        CancellationToken cancellationToken = default);
}

