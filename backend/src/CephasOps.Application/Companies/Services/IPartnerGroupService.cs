using CephasOps.Application.Companies.DTOs;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Partner group service interface
/// </summary>
public interface IPartnerGroupService
{
    /// <summary>
    /// Get all partner groups
    /// </summary>
    Task<List<PartnerGroupDto>> GetPartnerGroupsAsync(Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get partner group by ID
    /// </summary>
    Task<PartnerGroupDto?> GetPartnerGroupByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new partner group
    /// </summary>
    Task<PartnerGroupDto> CreatePartnerGroupAsync(CreatePartnerGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing partner group
    /// </summary>
    Task<PartnerGroupDto> UpdatePartnerGroupAsync(Guid id, UpdatePartnerGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a partner group
    /// </summary>
    Task DeletePartnerGroupAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

