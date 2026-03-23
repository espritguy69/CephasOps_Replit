using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service interface for VIP group operations
/// </summary>
public interface IVipGroupService
{
    /// <summary>
    /// Get all VIP groups
    /// </summary>
    Task<List<VipGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get VIP group by ID
    /// </summary>
    Task<VipGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get VIP group by code
    /// </summary>
    Task<VipGroupDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active VIP groups
    /// </summary>
    Task<List<VipGroupDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new VIP group
    /// </summary>
    Task<VipGroupDto> CreateAsync(CreateVipGroupDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing VIP group
    /// </summary>
    Task<VipGroupDto> UpdateAsync(Guid id, UpdateVipGroupDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a VIP group
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

