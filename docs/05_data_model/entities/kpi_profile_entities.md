# KPI Profiles – Entities & Relationships (Full Production)

## 1. Overview
KPI Profiles define performance rules for installers and subcontractors:
- Job duration targets
- Docket submission SLA
- Customer satisfaction targets
- Revisit thresholds
- Penalties & bonuses

---

## 2. Entities

### 2.1 `KpiProfile`
Defines a set of KPI rules.

Fields:
- `id` (PK)
- `tenant_id` (FK)
- `company_id` (FK)
- `name`
- `description`
- `effective_from`
- `effective_to` (nullable)
- `created_at`, `updated_at`

Relationships:
- 1 `KpiProfile` → many `KpiRule`
- 1 `KpiProfile` → many `InstallerKpiAssignment`

---

### 2.2 `KpiRule`
Individual KPI rule under a profile.

Fields:
- `id` (PK)
- `kpi_profile_id` (FK)
- `rule_type` (JobDuration / DocketSLA / RevisitRate / CustomerRating)
- `threshold_value`
- `operator` (<=, >=, between)
- `target_value`
- `penalty_points`
- `bonus_points`
- `metadata_json`
- `created_at`, `updated_at`

---

### 2.3 `InstallerKpiAssignment`
Links installer to KPI profile.

Fields:
- `id` (PK)
- `installer_profile_id` (FK)
- `kpi_profile_id` (FK)
- `assigned_at`
- `unassigned_at` (nullable)

Relationships:
- 1 Installer → many KPI profiles over time
