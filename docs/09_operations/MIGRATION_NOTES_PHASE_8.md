# Migration Notes – Phase 8 Platform Event Bus

## Schema changes

- **EventStore** table: new columns (all nullable for backward compatibility):
  - RootEventId (uuid)
  - PartitionKey (varchar 500)
  - ReplayId (varchar 100)
  - SourceService (varchar 100)
  - SourceModule (varchar 100)
  - CapturedAtUtc (timestamptz)
  - IdempotencyKey (varchar 500)
  - TraceId (varchar 64)
  - SpanId (varchar 64)
  - Priority (varchar 50)
- New indexes: IX_EventStore_RootEventId, IX_EventStore_PartitionKey, IX_EventStore_ReplayId, IX_EventStore_PartitionKey_CreatedAtUtc_EventId (filtered where PartitionKey IS NOT NULL).

Migration: **20260309210000_AddEventStorePhase8PlatformEnvelope**.

## Operational impact

- **Deploy order**: apply migration before or with the new application version. New code expects these columns for RETURNING in claim; if migration is not applied, claim SQL will fail.
- **Existing rows**: all new columns are null; no data backfill required. New appends will get PartitionKey, RootEventId, SourceService, etc. when IPlatformEventEnvelopeBuilder is used.
- **Claim order**: claim query now orders by PartitionKey NULLS LAST, CreatedAtUtc, EventId. Existing events with null PartitionKey are still claimed; they sort together and then by CreatedAtUtc/EventId.
- **Backpressure**: default thresholds (e.g. Reduced at 1000 pending) may trigger under load; tune via EventBus:Backpressure or leave defaults and monitor.

## Rollout considerations

- Single-node and multi-node: no change to lease/node behaviour; partition ordering improves determinism when PartitionKey is set.
- Configuration: optional new sections `EventBus:PlatformEnvelope` (SourceService, SourceModule, DefaultPriority) and `EventBus:Backpressure` (thresholds and delays). Omitted => defaults.
- API: new endpoints GET events/{id}/lineage, GET lineage/by-root/{id}, GET lineage/by-correlation/{id}. Optional; 404 if IEventLineageService not registered.

## Backward compatibility

- **IEventStore.AppendAsync** and **AppendInCurrentTransaction** accept an optional **EventStoreEnvelopeMetadata?** parameter; callers that do not pass it continue to work; envelope fields on new rows will be null except RootEventId from IHasRootEvent when implemented.
- **IDomainEvent**: no breaking change. **DomainEvent** now implements **IHasRootEvent** with optional **RootEventId**; existing events without RootEventId remain valid.
- **EventStoreListItemDto** / **EventStoreDetailDto**: new optional properties (RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc). Old API clients ignore them.
- **MapReaderToEntry**: reader uses ordinal >= FieldCount for new columns so if a DB has fewer columns (e.g. migration not applied on a replica), extra columns are read as null and do not throw.

## Commands

- Apply migration (from repo root, PostgreSQL):
  - `dotnet ef migrations script --idempotent --output migrations.sql --project backend/src/CephasOps.Api`
  - Apply migrations.sql to target DB, or:
  - `dotnet ef database update --project backend/src/CephasOps.Api`
- Run API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/src/CephasOps.Api --urls http://0.0.0.0:5000`
- Run tests: `dotnet test backend/tests/CephasOps.Application.Tests`
