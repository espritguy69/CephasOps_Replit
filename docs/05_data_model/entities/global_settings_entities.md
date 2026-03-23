# Global Settings – Entities & Relationships (Full Production)

## 1. Overview
Stores tenant-level and system-wide configurations:
- SLA rules
- Daily cut-off times
- Docket rules
- Auto-assignment rules
- Branding
- API keys
- Email parser settings
- Scheduler settings

---

## 2. Entities

### 2.1 `GlobalSettingGroup`
Represents category of settings.

Fields:
- `id` (PK)
- `tenant_id` (FK, nullable → NULL = system-wide)
- `code` (e.g. SLA, BILLING, DISPATCH)
- `name`
- `description`
- `created_at`, `updated_at`

Relationships:
- 1 `GlobalSettingGroup` → many `GlobalSetting`

---

### 2.2 `GlobalSetting`
Actual configuration key/value.

Fields:
- `id` (PK)
- `tenant_id` (FK, nullable)
- `group_id` (FK → GlobalSettingGroup)
- `key` (unique per tenant, e.g. DAILY_DOCKET_CUTOFF)
- `value_type` (String / Number / Bool / JSON / Time / Cron)
- `value`
- `description`
- `is_editable`
- `created_at`, `updated_at`

---

### 2.3 `ApiCredential`
Stores secure keys (encrypted).

Fields:
- `id` (PK)
- `tenant_id` (FK)
- `provider` (SMTP / IMAP / AWS / OCR provider / PDF parser)
- `credential_name`
- `encrypted_value`
- `created_at`, `updated_at`

---

## 3. Usage Examples

- `SLA_INSTALLATION = {"hours": 48}`  
- `DAILY_DOCKET_CUTOFF = "17:00"`  
- `AUTO_ASSIGNMENT_ENABLED = true`  
- `EMAIL_PARSER_RULESET = {…}`  
