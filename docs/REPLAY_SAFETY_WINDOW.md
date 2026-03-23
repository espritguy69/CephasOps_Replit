# Replay Safety Window

## Purpose

The replay safety window prevents replay execution from processing very recent events that may still be part of active live processing. This reduces:

- **Replay/live overlap** — replay and live handlers both acting on the same recent events
- **Race conditions** with ongoing event ingestion
- **Replay acting on unstable near-real-time state**
- **Confusing operator results** when very fresh events are still arriving

The safety window is enforced as a **real execution rule**, not a UI-only warning.

## Default rule

- **Cutoff:** `now - safety window`
- **Default window:** **5 minutes**
- Events with `OccurredAtUtc` **newer** than the cutoff are **excluded** from replay (preview and execution).

Example: at 14:00 UTC, cutoff is 13:55 UTC; only events with `OccurredAtUtc <= 13:55` are replayed.

## Where it is enforced

The same cutoff logic is applied wherever replay candidate events are resolved:

1. **New replay execution** — `ExecuteAsync`: cutoff computed, passed to `GetEventsForReplayAsync`, only events with `OccurredAtUtc <= cutoff` are loaded and replayed.
2. **Replay by operation id** — `ExecuteByOperationIdAsync`: same cutoff applied when loading events for the run.
3. **Rerun-failed** — `ExecuteRerunFailedAsync`: failed events are filtered by **event occurrence time** (`OccurredAtUtc`). Only failed events with `OccurredAtUtc <= cutoff` are replayed. If all failed events are newer than the cutoff, the operation returns an error and no replay is run.
4. **Preview** — `ReplayPreviewService.PreviewAsync`: same cutoff is passed to `GetEventsForReplayAsync`, so preview counts and sample reflect exactly what execution would process. A limitation is added to the preview when the request’s `ToOccurredAtUtc` is after the cutoff.

## Rerun-failed behavior

- **Safety basis:** **Event occurrence time** (`OccurredAtUtc`), not failed-event processing time.
- Failed events whose `OccurredAtUtc` is **after** the cutoff are excluded from the rerun.
- If **all** failed events are excluded, the API returns an error explaining that events must be at least 5 minutes old.
- If **some** are excluded, only the eligible (older) events are replayed; the count and logs make this clear.

## Operator visibility

- **Preview:** `ReplayPreviewResultDto` includes `SafetyWindowApplied`, `SafetyCutoffOccurredAtUtc`, `SafetyWindowMinutes`. When the request range extends past the cutoff, a limitation message is added.
- **Execution result:** `OperationalReplayExecutionResultDto` includes `SafetyWindowApplied`, `SafetyCutoffOccurredAtUtc`, `SafetyWindowMinutes` for every run.
- **Replay operation (list/detail):** `ReplayOperationListItemDto` and `ReplayOperationDetailDto` expose `SafetyCutoffOccurredAtUtc` and `SafetyWindowMinutes` so the UI can show the cutoff used for that run.

## Stored replay metadata

On `ReplayOperation`:

- `SafetyCutoffOccurredAtUtc` — effective cutoff used for the run
- `SafetyWindowMinutes` — window in minutes (e.g. 5)

Set at the start of execution and persisted with the operation.

## Logging

- **Replay safety cutoff applied:** structured log when starting a run (ExecuteAsync / ExecuteByOperationIdAsync) with cutoff timestamp and window minutes.
- **Rerun-failed:** when some failed events are excluded by the window, a log records how many were excluded and how many are being replayed; when all are excluded, a warning is logged and the API returns an error.

## Override

There is **no override** in the current implementation. The safety window is **mandatory** for all replay (new run, by operation id, rerun-failed) and preview. If an admin override is added later, it must be explicit, admin-only, and clearly surfaced in replay records and logs.

## Limitations

- Excluded event counts are **not** persisted (not cheap to compute in all paths); operators see that the window was applied and the cutoff time.
- The window is fixed at 5 minutes (configurable only by code constant `ReplaySafetyWindow.DefaultWindowMinutes`).
- Rerun-failed does one event-store lookup per failed event to get `OccurredAtUtc`; for very large failed sets this could be optimized with a batch query if needed.

## Consistency

Preview and execution use the **same** cutoff: both call `GetEventsForReplayAsync` with the same `safetyCutoffOccurredAtUtc` (computed as `ReplaySafetyWindow.GetCutoffUtc()`). What you see in preview is what execution will process (subject to eligibility policy and resume state).
