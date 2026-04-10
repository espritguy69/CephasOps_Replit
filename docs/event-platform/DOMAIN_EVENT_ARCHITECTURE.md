# CephasOps — Domain Event Architecture

**Date:** 2026-03-09  
**Purpose:** Design domain events and event store for workflow evolution (Option B).  
**Depends on:** docs/WORKFLOW_ENGINE_EVOLUTION_STRATEGY.md.

---

## 1. DomainEvent Base (Conceptual)

Events are required for workflow evolution (emit on transition, optional handlers). Below is a design for a **base** event shape and storage; implementation can be in C# or another language.

### 1.1 Base Fields

| Field | Type | Description |
|-------|------|-------------|
| **EventId** | Guid | Unique id for this occurrence. |
| **EventType** | string | e.g. "OrderStatusChanged", "InvoiceSubmitted". |
| **OccurredAtUtc** | DateTime | When the event occurred (UTC). |
| **CorrelationId** | string? | Links to request, parent job, or trace. |
| **CompanyId** | Guid? | Tenant/company for multi-tenancy. |
| **TriggeredByUserId** | Guid? | User who triggered the action that led to the event (if any). |

### 1.2 Example: OrderStatusChanged Payload

Additional payload for OrderStatusChanged (beyond base):

- EntityType: "Order"
- EntityId: Guid (Order Id)
- FromStatus: string
- ToStatus: string
- WorkflowJobId: Guid (optional, for traceability)
- Payload: optional dictionary (reason, source, metadata)

Base + payload together form the full event envelope for publishing and storage.

### 1.3 C#-Style Base Class (Design)

```csharp
// Conceptual — do not implement until approved
public abstract class DomainEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
}
```

Specific events (e.g. OrderStatusChangedEvent) extend this and add EntityId, FromStatus, ToStatus, WorkflowJobId, etc.

---

## 2. Event Store Table (Design)

Persisting events enables audit, replay, and async processing with retry.

### 2.1 Schema

| Column | Type | Description |
|--------|------|-------------|
| **EventId** | uuid | PK; same as DomainEvent.EventId. |
| **EventType** | varchar | e.g. OrderStatusChanged. |
| **Payload** | jsonb / text | Full serialized event (base + payload). |
| **OccurredAtUtc** | timestamptz | When the event occurred. |
| **ProcessedAtUtc** | timestamptz? | When a consumer last processed it (if applicable). |
| **RetryCount** | int | Number of processing attempts (for consumers). |
| **Status** | varchar | e.g. Pending, Processed, Failed, DeadLetter. |
| **CompanyId** | uuid? | Tenant. |
| **CorrelationId** | varchar? | For tracing. |

Optional: Index on (CompanyId, EventType, OccurredAtUtc), (CorrelationId), (Status) for queries and replay.

### 2.2 Usage

- **Publish path:** After a successful workflow transition, write one row to EventStore (Status = Pending or Processed depending on whether handlers run synchronously).
- **Consumers:** Handlers can read by Status = Pending (or from a queue fed by the store), process, then set ProcessedAtUtc and Status = Processed (or increment RetryCount and set Failed/DeadLetter on failure).
- **Replay:** Select by EventType and time range; re-publish or re-run handlers (idempotency required).

---

## 3. When to Implement

- **Base event shape and in-memory publish:** With Phase 3 of the evolution strategy (event emission).
- **EventStore table and persistence:** With Phase 5 (optional event store) when audit or replay is required.
- **CorrelationId and TriggeredByUserId:** From the start, so workflow execution is traceable (Phase 2 correlation).

This document is design-only; no code or schema changes until the evolution rollout proceeds.
