# Architecture Freeze Override Log

**Purpose:** Safety-critical platform files are *frozen*: CI fails when any of them change unless this file is **also modified in the same PR** with a **valid new table row** documenting the intentional change. This prevents accidental edits to guards, executor, tenant-safety CI, and core safety docs without explicit, auditable override.

**Override rule:** If your PR touches any [frozen file](ARCHITECTURE_FREEZE.md#frozen-paths), CI will fail unless you **add a row** to the **Entries** table below in this PR. The row must include: **Date** (YYYY-MM-DD), **Files / scope** (short description of what changed), and **Reason / PR** (non-empty justification or PR link). CI validates that this file was changed and that an added row has a date, scope, and reason.

---

## How to document an intentional override

1. In the **same PR** where you changed a frozen file, **add a row** to the **Entries** table below.
2. Set **Date** to YYYY-MM-DD, **Files / scope** to a short description of the changed file(s) or scope (e.g. "TenantSafetyGuard: clarify comment"), and **Reason / PR** to a justification or link (non-empty).
3. Commit the change. CI will pass only if it finds an **added** table row with date, scope, and reason; otherwise it fails.
4. Prefer platform owner review for frozen-file changes; see CODEOWNERS and [CEPHASOPS_PLATFORM_STANDARDS.md](../architecture/CEPHASOPS_PLATFORM_STANDARDS.md).

---

## Entries

| Date | Files / scope | Reason / PR |
|------|---------------|-------------|
| *(none yet)* | | |

---

*Do not remove this file. When a frozen file is modified, CI requires an added table row with date, files/scope, and non-empty reason.*
