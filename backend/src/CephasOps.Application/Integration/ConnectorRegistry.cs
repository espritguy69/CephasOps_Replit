using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Integration;

/// <summary>
/// Resolves connector definitions and endpoints from the database.
/// </summary>
public class ConnectorRegistry : IConnectorRegistry
{
    private readonly ApplicationDbContext _context;

    public ConnectorRegistry(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ConnectorDefinition?> GetDefinitionByKeyAsync(string connectorKey, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectorDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ConnectorKey == connectorKey && e.IsActive, cancellationToken);
    }

    public async Task<ConnectorEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectorEndpoints
            .AsNoTracking()
            .Include(e => e.ConnectorDefinition)
            .FirstOrDefaultAsync(e => e.Id == endpointId && e.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<ConnectorEndpoint>> GetOutboundEndpointsForEventAsync(string eventType, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var endpoints = await _context.ConnectorEndpoints
            .AsNoTracking()
            .Include(e => e.ConnectorDefinition)
            .Where(e => e.IsActive && !e.IsPaused && e.ConnectorDefinition != null &&
                        e.ConnectorDefinition.Direction == "Outbound" && e.ConnectorDefinition.IsActive)
            .ToListAsync(cancellationToken);

        var result = new List<ConnectorEndpoint>();
        foreach (var ep in endpoints)
        {
            if (ep.CompanyId.HasValue && companyId.HasValue && ep.CompanyId != companyId)
                continue;
            if (ep.CompanyId.HasValue && !companyId.HasValue)
                continue; // global endpoint only when no company scope
            if (!string.IsNullOrWhiteSpace(ep.AllowedEventTypes))
            {
                var allowed = ep.AllowedEventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (!allowed.Contains(eventType, StringComparer.OrdinalIgnoreCase))
                    continue;
            }
            result.Add(ep);
        }
        return result;
    }

    public async Task<ConnectorEndpoint?> GetInboundEndpointAsync(string connectorKey, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var def = await GetDefinitionByKeyAsync(connectorKey, cancellationToken);
        if (def == null) return null;

        var q = _context.ConnectorEndpoints
            .AsNoTracking()
            .Include(e => e.ConnectorDefinition)
            .Where(e => e.ConnectorDefinitionId == def.Id && e.IsActive);
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == null || e.CompanyId == companyId.Value);
        else
            q = q.Where(e => e.CompanyId == null);

        return await q.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConnectorDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConnectorDefinitions
            .AsNoTracking()
            .OrderBy(e => e.ConnectorKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ConnectorEndpoint> Items, int TotalCount)> ListEndpointsAsync(Guid? connectorDefinitionId, Guid? companyId, int skip, int take, CancellationToken cancellationToken = default)
    {
        IQueryable<ConnectorEndpoint> q = _context.ConnectorEndpoints.AsNoTracking().Include(e => e.ConnectorDefinition);
        if (connectorDefinitionId.HasValue)
            q = q.Where(e => e.ConnectorDefinitionId == connectorDefinitionId.Value);
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(e => e.ConnectorDefinition!.ConnectorKey).ThenBy(e => e.CompanyId).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }
}
