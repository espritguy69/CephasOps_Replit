using CephasOps.Domain.Events;
using CephasOps.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Defense-in-depth guard for EventStore appends. Validates event metadata completeness,
/// company consistency when entity context is present, parent/root linkage, and stream
/// consistency (same aggregate type and company for the same entity stream). Call before
/// adding the entry to the context or before SaveChanges.
/// </summary>
public static class EventStoreConsistencyGuard
{
    /// <summary>
    /// Throws if neither a valid tenant context nor an approved platform bypass is active.
    /// Call at the start of EventStore append paths so appends fail fast when invoked without
    /// tenant scope or explicit platform bypass (e.g. dispatcher or replay running under executor).
    /// </summary>
    /// <param name="operationName">Short name for the exception message (e.g. "Append", "AppendInCurrentTransaction").</param>
    public static void RequireTenantOrBypassForAppend(string operationName)
    {
        if (TenantSafetyGuard.IsPlatformBypassActive)
            return;
        var tenantId = TenantScope.CurrentTenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            return;
        var message = "EventStore append requires a valid tenant context (TenantScope.CurrentTenantId) or an approved platform bypass. Missing or empty tenant context.";
        PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", operationName, message);
        throw new InvalidOperationException(
            $"EventStoreConsistencyGuard: {operationName}: {message}");
    }

    /// <summary>
    /// When the new event has a parent or root reference, validates that parent and root events
    /// (when loaded and when they have CompanyId) belong to the same company as the new event.
    /// Prevents cross-tenant event linkage in a chain.
    /// </summary>
    /// <param name="entry">The event being appended.</param>
    /// <param name="parentEntry">The parent event if entry.ParentEventId was set; null if not loaded or not found.</param>
    /// <param name="rootEntry">The root event if entry.RootEventId was set and differs from entry.EventId; null if not loaded or not found.</param>
    public static void RequireParentRootCompanyMatch(EventStoreEntry entry, EventStoreEntry? parentEntry, EventStoreEntry? rootEntry)
    {
        if (entry == null) return;
        if (!entry.CompanyId.HasValue || entry.CompanyId.Value == Guid.Empty) return;

        if (parentEntry != null && parentEntry.CompanyId.HasValue && parentEntry.CompanyId.Value != Guid.Empty
            && parentEntry.CompanyId.Value != entry.CompanyId.Value)
        {
            PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "Parent event company does not match new event company.", companyId: entry.CompanyId, entityType: entry.EntityType, entityId: entry.EntityId, eventId: entry.EventId);
            throw new InvalidOperationException(
                "EventStoreConsistencyGuard: Parent event must belong to the same company as the new event. " +
                $"EventId={entry.EventId}, EventType={entry.EventType}, CompanyId={entry.CompanyId}, ParentEventId={entry.ParentEventId}, ParentCompanyId={parentEntry.CompanyId}. Operation: Append.");
        }

        if (rootEntry != null && rootEntry.CompanyId.HasValue && rootEntry.CompanyId.Value != Guid.Empty
            && rootEntry.CompanyId.Value != entry.CompanyId.Value)
        {
            PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "Root event company does not match new event company.", companyId: entry.CompanyId, entityType: entry.EntityType, entityId: entry.EntityId, eventId: entry.EventId);
            throw new InvalidOperationException(
                "EventStoreConsistencyGuard: Root event must belong to the same company as the new event. " +
                $"EventId={entry.EventId}, EventType={entry.EventType}, CompanyId={entry.CompanyId}, RootEventId={entry.RootEventId}, RootCompanyId={rootEntry.CompanyId}. Operation: Append.");
        }
    }

    /// <summary>
    /// Throws if required event metadata is missing. EventId must be non-empty; EventType must be non-null and non-empty.
    /// </summary>
    public static void RequireEventMetadata(EventStoreEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));
        if (entry.EventId == Guid.Empty)
        {
            PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "EventId is required and cannot be empty.", eventId: entry.EventId);
            throw new InvalidOperationException(
                "EventStoreConsistencyGuard: EventId is required and cannot be empty. Operation: Append.");
        }
        if (string.IsNullOrWhiteSpace(entry.EventType))
        {
            PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "EventType is required and cannot be null or empty.", entityType: entry.EntityType, entityId: entry.EntityId, eventId: entry.EventId, companyId: entry.CompanyId);
            throw new InvalidOperationException(
                "EventStoreConsistencyGuard: EventType is required and cannot be null or empty. " +
                $"EventId={entry.EventId}. Operation: Append.");
        }
    }

    /// <summary>
    /// When the event has entity context (EntityType or EntityId set), CompanyId must be present.
    /// Prevents company-scoped aggregate events from being stored without company identity.
    /// </summary>
    public static void RequireCompanyWhenEntityContext(EventStoreEntry entry)
    {
        if (entry == null) return;
        var hasEntityContext = !string.IsNullOrWhiteSpace(entry.EntityType) || entry.EntityId.HasValue;
        if (!hasEntityContext) return;
        if (!entry.CompanyId.HasValue || entry.CompanyId.Value == Guid.Empty)
        {
            PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "CompanyId is required when the event has entity context.", entityType: entry.EntityType, entityId: entry.EntityId, eventId: entry.EventId);
            throw new InvalidOperationException(
                "EventStoreConsistencyGuard: CompanyId is required when the event has entity context (EntityType or EntityId). " +
                $"EventId={entry.EventId}, EventType={entry.EventType}, EntityType={entry.EntityType}, EntityId={entry.EntityId}. Operation: Append.");
        }
    }

    /// <summary>
    /// Validates parent/root linkage: no self-reference (EventId != ParentEventId); when both ParentEventId and RootEventId
    /// are set, RootEventId must not be empty.
    /// </summary>
    public static void RequireValidParentRootLinkage(EventStoreEntry entry)
    {
        if (entry == null) return;
        if (entry.ParentEventId.HasValue && entry.ParentEventId.Value == entry.EventId)
        {
            PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "ParentEventId cannot equal EventId (self-reference).", entityType: entry.EventType, eventId: entry.EventId);
            throw new InvalidOperationException(
                "EventStoreConsistencyGuard: ParentEventId cannot equal EventId (self-reference). " +
                $"EventId={entry.EventId}, EventType={entry.EventType}. Operation: Append.");
        }
        if (entry.ParentEventId.HasValue && entry.RootEventId.HasValue && entry.RootEventId.Value == Guid.Empty)
        {
            PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "RootEventId cannot be empty when ParentEventId is set.", entityType: entry.EventType, eventId: entry.EventId);
            throw new InvalidOperationException(
                "EventStoreConsistencyGuard: RootEventId cannot be empty when ParentEventId is set. " +
                $"EventId={entry.EventId}, EventType={entry.EventType}, ParentEventId={entry.ParentEventId}. Operation: Append.");
        }
    }

    /// <summary>
    /// Throws when append is attempted for an EventId that already exists in the store (duplicate append).
    /// Call after checking store for existing EventId so duplicate processing is prevented and a clear exception is thrown.
    /// </summary>
    /// <param name="eventId">The event id that already exists.</param>
    /// <param name="companyId">Optional company for log context.</param>
    /// <param name="logger">Optional repository logger for consistency warning.</param>
    public static void RequireDuplicateAppendRejected(Guid eventId, Guid? companyId, ILogger? logger = null)
    {
        PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "Duplicate event append: EventId already exists in store.", companyId: companyId, eventId: eventId);
        logger?.LogWarning(
            "EventStore consistency: duplicate append rejected. EventId={EventId}, CompanyId={CompanyId}, Operation=Append, GuardReason=DuplicateEventId",
            eventId, companyId);
        throw new InvalidOperationException(
            $"EventStoreConsistencyGuard: Duplicate event append. EventId={eventId} already exists in the store. Operation: Append.");
    }

    /// <summary>
    /// When prior events exist for the same entity stream (same EntityType + EntityId), the new event must have
    /// the same CompanyId and EntityType. Prevents cross-company or mixed aggregate-type contamination of a stream.
    /// </summary>
    public static void RequireStreamConsistency(EventStoreEntry entry, IReadOnlyList<EventStoreEntry> priorEventsInStream)
    {
        if (entry == null) return;
        if (priorEventsInStream == null || priorEventsInStream.Count == 0) return;

        foreach (var prior in priorEventsInStream)
        {
            if (prior.CompanyId.HasValue && prior.CompanyId.Value != Guid.Empty)
            {
                    if (!entry.CompanyId.HasValue || entry.CompanyId.Value != prior.CompanyId.Value)
                {
                    PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "Company mismatch in event stream.", companyId: entry.CompanyId, entityType: entry.EntityType, entityId: entry.EntityId, eventId: entry.EventId);
                    throw new InvalidOperationException(
                        "EventStoreConsistencyGuard: Company mismatch in event stream. New event CompanyId must match prior events for the same entity. " +
                        $"EventId={entry.EventId}, EventType={entry.EventType}, EntityType={entry.EntityType}, EntityId={entry.EntityId}, " +
                        $"NewCompanyId={entry.CompanyId}, PriorCompanyId={prior.CompanyId}. Operation: Append.");
                }
            }
            if (!string.IsNullOrWhiteSpace(prior.EntityType) && prior.EntityType != entry.EntityType)
            {
                PlatformGuardLogger.LogViolation("EventStoreConsistencyGuard", "Append", "EntityType mismatch in event stream.", entityType: entry.EntityType, entityId: entry.EntityId, eventId: entry.EventId);
                throw new InvalidOperationException(
                    "EventStoreConsistencyGuard: EntityType mismatch in event stream. New event EntityType must match prior events for the same EntityId. " +
                    $"EventId={entry.EventId}, EventType={entry.EventType}, EntityId={entry.EntityId}, NewEntityType={entry.EntityType}, PriorEntityType={prior.EntityType}. Operation: Append.");
            }
        }
    }
}
