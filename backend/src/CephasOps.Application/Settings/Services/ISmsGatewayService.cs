using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// SMS Gateway service interface
/// </summary>
public interface ISmsGatewayService
{
    /// <summary>
    /// Register or update an SMS Gateway
    /// Ensures only one active gateway at a time
    /// </summary>
    Task<Guid> RegisterGatewayAsync(RegisterSmsGatewayRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the currently active SMS Gateway
    /// </summary>
    Task<SmsGatewayDto?> GetActiveGatewayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all SMS Gateways
    /// </summary>
    Task<List<SmsGatewayDto>> GetAllGatewaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a gateway
    /// </summary>
    Task<bool> DeactivateGatewayAsync(Guid id, CancellationToken cancellationToken = default);
}

