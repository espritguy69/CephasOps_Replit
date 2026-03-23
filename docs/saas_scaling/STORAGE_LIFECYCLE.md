# Tenant Storage Lifecycle

**Purpose:** Prepare for large file storage growth with tiering and lifecycle rules. File metadata supports access tracking and storage tier; a background service applies tier transitions.

---

## File metadata (extended)

| Field | Type | Description |
|-------|------|-------------|
| **LastAccessedAtUtc** | DateTime? | Last time the file was read or updated. Update in your file download/serve path when access tracking is enabled. |
| **StorageTier** | string | **Hot** \| **Warm** \| **Cold** \| **Archive**. Default: Hot. Used for retention and future physical tiering. |
| **CreatedAt** | DateTime | From base entity; used when LastAccessedAtUtc is null for lifecycle age. |

---

## StorageLifecycleService

Background hosted service that runs on a configurable interval (default: 24 hours):

- Runs **per tenant** via **TenantScopeExecutor.RunWithTenantScopeAsync(companyId, ...)**.
- For each tenant, selects files by current tier and age (using **LastAccessedAtUtc ?? CreatedAt**):
  - **Hot → Warm:** age &gt; WarmAfterDays (default 90).
  - **Warm → Cold:** age &gt; ColdAfterDays (default 180).
  - **Warm/Cold → Archive:** age &gt; ArchiveAfterDays (default 365).
- Updates **StorageTier** only (no physical move in this phase). Downstream processes can use tier for moving blobs to cold/archive storage.

---

## Configuration: SaaS:StorageLifecycle

| Option | Default | Description |
|--------|---------|-------------|
| **Enabled** | true | Enable or disable the lifecycle service. |
| **Interval** | 24:00:00 | Time between runs. |
| **WarmAfterDays** | 90 | Days without access → Warm. 0 = skip. |
| **ColdAfterDays** | 180 | Days without access → Cold. 0 = skip. |
| **ArchiveAfterDays** | 365 | Days without access → Archive. 0 = skip. |
| **MaxFilesPerTenantPerRun** | 500 | Max file tier updates per tenant per run. |

---

## Lifecycle rules (summary)

- **Hot:** Default; recent or frequently accessed.
- **Warm:** Not accessed for WarmAfterDays (e.g. 90).
- **Cold:** Not accessed for ColdAfterDays (e.g. 180).
- **Archive:** Not accessed for ArchiveAfterDays (e.g. 365); candidate for archive storage or retention policies.

Physical movement of blobs to cold/archive storage is not implemented in this service; implement in your storage layer based on **StorageTier**.

---

## Access tracking

To make **LastAccessedAtUtc** meaningful, update it when files are read (e.g. in download or preview endpoints). If you do not update it, lifecycle uses **CreatedAt** only.
