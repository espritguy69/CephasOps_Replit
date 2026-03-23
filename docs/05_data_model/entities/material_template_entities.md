# Material Templates – Entities & Relationships (Full Production)

## 1. Overview
Material templates define:
- Standard materials for FTTH/FTTB installs
- Router models per ISP
- Preconfigured quantities
- Material bundles per job type

Used during:
- Order creation
- Work order planning
- Installer material issuance

---

## 2. Entities

### 2.1 `MaterialTemplate`
Top-level template.

Fields:
- `id` (PK)
- `tenant_id`, `company_id`
- `template_code` (e.g. FTTH_STD)
- `name`
- `description`
- `job_type` (FTTH / FTTC / FTTB / Modification / Assurance)
- `is_active`
- `created_at`, `updated_at`

Relationships:
- 1 `MaterialTemplate` → many `MaterialTemplateItem`

---

### 2.2 `MaterialTemplateItem`
Defines each item under a template.

Fields:
- `id` (PK)
- `material_template_id` (FK)
- `material_item_id` (FK)
- `default_quantity`
- `is_optional`
- `notes`
- `created_at`, `updated_at`

---

### 2.3 `MaterialAssignmentPreset`
Used when auto-assigning materials to job types.

Fields:
- `id` (PK)
- `tenant_id`, `company_id`
- `job_type`
- `material_template_id` (FK)
- `effective_from`
- `effective_to`
- `created_at`, `updated_at`

