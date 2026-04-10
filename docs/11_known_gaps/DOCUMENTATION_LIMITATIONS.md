# Documentation Limitations

**Date:** April 2026  
**Status:** Active — these are known documentation coverage gaps

---

## DL-1: API Documentation Coverage (~50%)

**Description:** Only about 60 of 132 backend controllers are documented in `docs/04_api/`.

**Undocumented controller groups:**
- Platform Analytics (`PlatformAnalyticsController`, `OperationalInsightsController`)
- Observability (`ObservabilityController`)
- Messaging (`SmsController`, `WhatsAppController`, `MessagingController`)
- Master Data (`SkillsController`, `TeamsController`, `WarehousesController`, `VendorsController`, `VerticalsController`, `TaxCodesController`)
- GPON Rates (`GponBaseWorkRatesController`)
- Financial Alerts (`FinancialAlertsController`)
- Excel-to-PDF (`ExcelToPdfController`)
- Many entity-specific CRUD controllers (`AssetTypesController`, `BrandsController`, `BinsController`, `OrderCategoriesController`, etc.)

**Priority:** Expand docs for Analytics, Messaging, and Master Data first — these are the most frequently used undocumented groups.

---

## DL-2: Data Model Missing 12+ Entities

**Description:** The following entities exist in code but are not documented in `docs/05_data_model/`:
- `Tenant`, `TenantActivityEvent`, `TenantOnboardingProgress`
- `OperationalInsight`, `MigrationAudit`
- `LedgerBalanceCache`, `StockByLocationSnapshot`, `MaterialPartner`
- `BaseWorkRate`, `GponSiJobRate`
- `OrderFinancialAlert`
- `ExternalIdempotencyRecord`

Some of these may be referenced in module-specific or phase docs, but they lack dedicated entity documentation.

---

## DL-3: Infrastructure Fields Not Documented

**Description:** Most `CompanyScopedEntity` subclasses now include:
- `IsDeleted` (bool) — soft delete flag
- `DeletedAt` (DateTime?) — when deleted
- `DeletedByUserId` (Guid?) — who deleted
- `RowVersion` (byte[]) — concurrency token

These standard fields are not mentioned in any entity documentation in `05_data_model/entities/`.

**Note:** Not all entities have these fields — entities inheriting from `BaseEntity` (like `Tenant`) may not include soft-delete columns.

---

## DL-4: SI App Dual Implementation Not Clarified

**Description:** Two separate codebases serve the Service Installer user:
- `frontend-si/` — React PWA (web-based, production-ready)
- `si-mobile/` — Expo/React Native (native mobile app)

Documentation doesn't clearly explain why both exist, which one is primary, or what the long-term strategy is.

---

## DL-5: VPS Deployment Guide

**Description:** `docs/08_infrastructure/` provides high-level deployment guidance but does not include a detailed step-by-step VPS deployment guide matching the actual `deploy-vps-native.sh` script.

**Status:** A VPS deployment guide has been added in this remediation pass. See `docs/08_infrastructure/VPS_DEPLOYMENT_GUIDE.md`.

---

## DL-6: Naming Inconsistencies in Docs vs Code

| Doc Term | Code Term | Files Affected |
|----------|-----------|----------------|
| `RmaTicket` | `RmaRequest` | `inventory_entities.md`, `document_templates_relationships.md` |
| `RmaItem` | `RmaRequestItem` | `inventory_entities.md` |
| `SettingsController` | `GlobalSettingsController` + `IntegrationSettingsController` | `API_CONTRACTS_SUMMARY.md` |
| `EmailController` | `EmailsController` / `EmailSendingController` | API docs |

---

## DL-7: Root-Level Documentation Sprawl

**Description:** 154 markdown files were at the `/docs/` root level before this remediation. Most were phase summaries, audit reports, and transient deliverables that belong in subdirectories.

**Status:** Remediated in this pass — files moved to `archive/`, `02_modules/`, `operations/`, and other appropriate locations.
