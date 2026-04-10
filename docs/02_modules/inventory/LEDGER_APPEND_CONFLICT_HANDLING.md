# Ledger Append Conflict Handling

## Purpose

Under concurrency, two writers can both pass the “check exists” and both attempt to insert a ledger row for the same idempotency key `(SourceEventId, LedgerFamily)` or `(ReplayOperationId, LedgerFamily)`. One insert succeeds; the other hits the unique constraint and would throw. Without handling, that exception can bubble to the handler and cause the event to be marked failed or retried.

This hardening makes **unique constraint violations on append** a **no-op success**: the second writer treats the conflict as idempotent and does not fail.

## How It Works

- **LedgerWriter** still does a pre-check (`AnyAsync`) and returns early if an entry already exists.
- When it proceeds to **Add** and **SaveChangesAsync**, it may race with another writer. If **SaveChangesAsync** throws **DbUpdateException** due to a **unique constraint violation** (e.g. PostgreSQL `23505`), the writer:
  1. Detaches the failed entries from the context so the context is left clean.
  2. Logs at **Debug** that a conflict was treated as success.
  3. Returns without rethrowing.

- Detection is provider-friendly: the code checks the exception (and inner exception) message for:
  - PostgreSQL unique violation code **23505**, or
  - The phrases **"unique constraint"** or **"duplicate key"** (case-insensitive).

So the second caller effectively gets the same outcome as the first: one row exists, and no error is reported.

## Code Changes

- **LedgerWriter** (Application):
  - **SaveChangesAndHandleConflictAsync**: wraps `SaveChangesAsync` in try/catch; on `DbUpdateException` that matches **IsUniqueConstraintViolation**, detaches the failed entries and returns.
  - **IsUniqueConstraintViolation**: returns true when the exception message (or inner message) indicates a unique constraint / duplicate key (e.g. 23505).
  - **AppendFromEventAsync** and **AppendFromReplayOperationAsync** call **SaveChangesAndHandleConflictAsync** instead of **SaveChangesAsync** directly.

## Result

- Handlers that call the ledger writer no longer see a failure when they lose the TOCTOU race; the append is treated as idempotent success.
- Event processing is not marked failed or retried solely due to this concurrent append conflict.
- Existing idempotency (one row per key) and replay safety are unchanged.
