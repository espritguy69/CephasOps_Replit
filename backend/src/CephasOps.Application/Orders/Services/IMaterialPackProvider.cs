using CephasOps.Application.Orders.DTOs;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Provides material pack for an order. Used by event handlers and API.
/// </summary>
public interface IMaterialPackProvider
{
    Task<MaterialPackDto> GetMaterialPackAsync(Guid orderId, Guid? companyId = null, CancellationToken cancellationToken = default);
}
