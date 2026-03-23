using CephasOps.Application.Workflow.DTOs;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service interface for managing side effect definitions
/// </summary>
public interface ISideEffectDefinitionsService
{
    Task<List<SideEffectDefinitionDto>> GetSideEffectDefinitionsAsync(
        Guid companyId,
        string? entityType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<SideEffectDefinitionDto?> GetSideEffectDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SideEffectDefinitionDto> CreateSideEffectDefinitionAsync(
        Guid companyId,
        CreateSideEffectDefinitionDto dto,
        CancellationToken cancellationToken = default);

    Task<SideEffectDefinitionDto> UpdateSideEffectDefinitionAsync(
        Guid companyId,
        Guid id,
        UpdateSideEffectDefinitionDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteSideEffectDefinitionAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);
}

