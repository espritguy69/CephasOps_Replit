using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service interface for VIP email operations
/// </summary>
public interface IVipEmailService
{
    /// <summary>
    /// Get all VIP emails
    /// </summary>
    Task<List<VipEmailDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get VIP email by ID
    /// </summary>
    Task<VipEmailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get VIP email by email address (exact match)
    /// </summary>
    Task<VipEmailDto?> GetByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active VIP emails for evaluation
    /// </summary>
    Task<List<VipEmailDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new VIP email entry
    /// </summary>
    Task<VipEmailDto> CreateAsync(CreateVipEmailDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing VIP email entry
    /// </summary>
    Task<VipEmailDto> UpdateAsync(Guid id, UpdateVipEmailDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a VIP email entry
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

