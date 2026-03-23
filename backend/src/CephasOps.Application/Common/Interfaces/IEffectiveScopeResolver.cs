using CephasOps.Application.Common.DTOs;

namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Resolves order scope context (PartnerId, DepartmentId, OrderTypeCode) for use in
/// workflow resolution and other scope-based lookups. OrderTypeCode uses parent code when subtype.
/// See docs/WORKFLOW_RESOLUTION_RULES.md and docs/ORDER_SCOPE_RESOLVER_ARCHITECTURE.md.
/// </summary>
public interface IEffectiveScopeResolver
{
    /// <summary>
    /// Resolve scope from an entity (e.g. Order). When entityType is "Order", loads the order
    /// and returns PartnerId, DepartmentId, and OrderTypeCode (parent code when subtype).
    /// For non-Order entity types or when entity is not found, returns null.
    /// </summary>
    Task<EffectiveOrderScope?> ResolveFromEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve OrderTypeCode for scope: parent's Code when OrderType is a subtype, else own Code.
    /// Returns null when order type is not found.
    /// </summary>
    Task<string?> GetOrderTypeCodeForScopeAsync(Guid orderTypeId, CancellationToken cancellationToken = default);
}
