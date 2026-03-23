using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// SMS Gateway service implementation
/// </summary>
public class SmsGatewayService : ISmsGatewayService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SmsGatewayService> _logger;

    public SmsGatewayService(
        ApplicationDbContext context,
        ILogger<SmsGatewayService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> RegisterGatewayAsync(RegisterSmsGatewayRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering SMS Gateway: {DeviceName} at {BaseUrl}", request.DeviceName, request.BaseUrl);

        // Get all currently active gateways
        var activeGateways = await _context.SmsGateways
            .Where(g => g.IsActive)
            .ToListAsync(cancellationToken);

        SmsGateway gateway;

        if (activeGateways.Count == 0)
        {
            // No active gateway exists - create new one
            gateway = new SmsGateway
            {
                Id = Guid.NewGuid(),
                DeviceName = request.DeviceName,
                BaseUrl = request.BaseUrl,
                ApiKey = request.ApiKey,
                AdditionalInfo = request.AdditionalInfo,
                IsActive = true,
                LastSeenAtUtc = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SmsGateways.Add(gateway);
            _logger.LogInformation("Created new SMS Gateway: {GatewayId}", gateway.Id);
        }
        else
        {
            // Update the first active gateway and deactivate others
            gateway = activeGateways[0];
            gateway.Update(request.DeviceName, request.BaseUrl, request.ApiKey, request.AdditionalInfo);
            gateway.Activate();

            // Deactivate all other active gateways
            for (int i = 1; i < activeGateways.Count; i++)
            {
                activeGateways[i].Deactivate();
            }

            _logger.LogInformation("Updated SMS Gateway: {GatewayId}, deactivated {Count} other gateways", 
                gateway.Id, activeGateways.Count - 1);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return gateway.Id;
    }

    public async Task<SmsGatewayDto?> GetActiveGatewayAsync(CancellationToken cancellationToken = default)
    {
        var gateway = await _context.SmsGateways
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.LastSeenAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return gateway != null ? MapToDto(gateway) : null;
    }

    public async Task<List<SmsGatewayDto>> GetAllGatewaysAsync(CancellationToken cancellationToken = default)
    {
        var gateways = await _context.SmsGateways
            .OrderByDescending(g => g.LastSeenAtUtc)
            .ToListAsync(cancellationToken);

        return gateways.Select(MapToDto).ToList();
    }

    public async Task<bool> DeactivateGatewayAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var gateway = await _context.SmsGateways
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (gateway == null)
        {
            return false;
        }

        gateway.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated SMS Gateway: {GatewayId}", id);
        return true;
    }

    private static SmsGatewayDto MapToDto(SmsGateway gateway)
    {
        return new SmsGatewayDto
        {
            Id = gateway.Id,
            DeviceName = gateway.DeviceName,
            BaseUrl = gateway.BaseUrl,
            ApiKey = gateway.ApiKey,
            LastSeenAtUtc = gateway.LastSeenAtUtc,
            IsActive = gateway.IsActive,
            AdditionalInfo = gateway.AdditionalInfo,
            CreatedAt = gateway.CreatedAt,
            UpdatedAt = gateway.UpdatedAt
        };
    }
}

