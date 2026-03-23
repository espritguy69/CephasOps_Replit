using CephasOps.Application.Workflow.DTOs;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service interface for managing guard condition definitions
/// </summary>
public interface IGuardConditionDefinitionsService
{
    Task<List<GuardConditionDefinitionDto>> GetGuardConditionDefinitionsAsync(
        Guid companyId,
        string? entityType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<GuardConditionDefinitionDto?> GetGuardConditionDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<GuardConditionDefinitionDto> CreateGuardConditionDefinitionAsync(
        Guid companyId,
        CreateGuardConditionDefinitionDto dto,
        CancellationToken cancellationToken = default);

    Task<GuardConditionDefinitionDto> UpdateGuardConditionDefinitionAsync(
        Guid companyId,
        Guid id,
        UpdateGuardConditionDefinitionDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteGuardConditionDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);
}

