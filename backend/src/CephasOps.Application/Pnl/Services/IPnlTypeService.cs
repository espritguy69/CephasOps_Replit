using CephasOps.Application.Pnl.DTOs;
using CephasOps.Domain.Pnl.Enums;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// PnL Type service interface
/// </summary>
public interface IPnlTypeService
{
    /// <summary>
    /// Get all PnL types (flat list)
    /// </summary>
    Task<List<PnlTypeDto>> GetPnlTypesAsync(Guid? companyId, PnlTypeCategory? category = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get PnL types as a hierarchical tree
    /// </summary>
    Task<List<PnlTypeTreeDto>> GetPnlTypeTreeAsync(Guid? companyId, PnlTypeCategory? category = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single PnL type by ID
    /// </summary>
    Task<PnlTypeDto?> GetPnlTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new PnL type
    /// </summary>
    Task<PnlTypeDto> CreatePnlTypeAsync(CreatePnlTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing PnL type
    /// </summary>
    Task<PnlTypeDto> UpdatePnlTypeAsync(Guid id, UpdatePnlTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a PnL type
    /// </summary>
    Task DeletePnlTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get PnL types available for transactions (leaf nodes or transactional types)
    /// </summary>
    Task<List<PnlTypeDto>> GetTransactionalPnlTypesAsync(Guid? companyId, PnlTypeCategory? category = null, CancellationToken cancellationToken = default);
}

