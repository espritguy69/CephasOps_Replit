# Building Import – Implementation Report

## 1. Inspection findings

### 1.1 Building entity fields used

- **Entity:** `CephasOps.Domain.Buildings.Entities.Building` (CompanyScopedEntity).
- **Fields used in import (CreateBuildingDto):** Name, Code, AddressLine1, AddressLine2, City, State, Postcode, Area, BuildingTypeId, InstallationMethodId, PropertyType (obsolete), DepartmentId, Notes, IsActive. CompanyId from authenticated user.
- **Required for create:** Name, AddressLine1, City, State, Postcode (non-empty strings). InstallationMethodId optional at entity level but the **existing CSV import** resolves InstallationMethod by name and throws if name is provided and not found.

### 1.2 Type/classification model used

- **InstallationMethod** (table `InstallationMethods`) is used for site/installation type, not BuildingType.
- **Names in DB (from backup/seed):** `Prelaid`, `Non-Prelaid`, `SDU`, `RDF POLE`.
- **BuildingType** (Condominium, Apartment, etc.) was not used for this import; source “Type” (Prelaid / Non Prelaid / SDU / RDF POLE) maps to **InstallationMethod** only. PropertyType left empty.

### 1.3 Import path chosen

- **Existing Buildings import API:** `POST /api/buildings/import` (BuildingsController), accepting multipart form with a single file.
- Uses `ICsvService.ImportFromCsv<BuildingCsvDto>(stream)`, then for each record resolves Department, BuildingType (by PropertyType), InstallationMethod (by InstallationMethodName), builds CreateBuildingDto, and calls `IBuildingService.CreateBuildingAsync(dto, companyId, cancellationToken)`.
- **Change made:** Idempotent behaviour. When `CreateBuildingAsync` throws `InvalidOperationException` with message containing “already exists”, the controller catches it, records the row in `result.Errors` with message “Skipped (duplicate): …”, and continues. No SuccessCount or ErrorCount change for that row.

### 1.4 Company-scoping approach

- Company is taken from **current user** via `_currentUserService.CompanyId` in the controller. No explicit CompanyId in the CSV; the user must be logged in and the token must carry company context. Imported buildings are created for that company only.

---

## 2. Mapping used

| Source column   | CephasOps field / behaviour |
|-----------------|-----------------------------|
| **Building Name** | `Name` (Building + CreateBuildingDto). Trimmed. |
| **Location**      | Split into address and city/state: `AddressLine1` = location text (or city when location is a city name); `City` = normalized city; `State` = normalized state (e.g. Wilayah Persekutuan Kuala Lumpur, Selangor). |
| **Type**          | `InstallationMethodName` in CSV → resolved to `InstallationMethodId` via lookup by Name (Prelaid, Non-Prelaid, SDU, RDF POLE). |

- **Postcode:** Set to `00000` when not known from source.
- **Code, PropertyType, DepartmentName:** Left empty.
- **IsActive:** `true` for all rows.

---

## 3. Normalization rules applied

### 3.1 Location normalization

- **Kuala Lumpur variants** (Kuala Lumpur, Kula Lumpur, Kula lumpur, Kualalumpur, KUALA LUMPUR) → **City:** Kuala Lumpur; **State:** Wilayah Persekutuan Kuala Lumpur.
- **Shah Alam** (Shah Alam, SHAH ALAM, shah alam) → City: Shah Alam; State: Selangor.
- **Petaling Jaya** (PETALING JAYA, Petaling Jaya) → City: Petaling Jaya; State: Selangor.
- **Glenmarie / GLEMARIE** → City: Glenmarie or Shah Alam as appropriate; State: Selangor.
- **Sepang, Puchong, Ampang, Klang, Kajang, Setia Alam, Dengkil, Puncak Alam, Subang Jaya, Sentul, Rawang, Kota Damansara, Kota Kemuning, Bangi, Bukit Jalil, etc.** → Canonical city name; State Selangor except for KL area (Wilayah Persekutuan Kuala Lumpur).
- **Selangor** (standalone) → City: Selangor; State: Selangor.
- Full addresses (e.g. “Jalan X, 12345 Kuala Lumpur”) → AddressLine1 set from location text; City/State from parsing or fallback (e.g. Kuala Lumpur / Wilayah Persekutuan Kuala Lumpur).

### 3.2 Type normalization

- **Prelaid** → InstallationMethodName: `Prelaid`.
- **Non Prelaid** (with space) → InstallationMethodName: `Non-Prelaid`.
- **SDU** → InstallationMethodName: `SDU`.
- **RDF POLE** → InstallationMethodName: `RDF POLE`.

### 3.3 Casing / typo cleanup

- City and state normalized to consistent casing (e.g. “Kuala Lumpur”, “Shah Alam”, “Selangor”).
- Building names kept as in source except where needed for deduplication (e.g. second “WISMA IJM” for Petaling Jaya named “WISMA IJM PETALING JAYA” to distinguish from KL).

---

## 4. Duplicate-handling rules applied

### 4.1 Within source data

- **Name + location:** Rows with same normalized (Name, City) treated as one; first kept, later duplicates not added again in the normalized CSV.
- **Explicit duplicate:** “SRI INTAN 1” appeared twice (same name, same city); kept once.
- **Same name, different location:** “WISMA IJM” (Kuala Lumpur, Non-Prelaid) and “WISMA IJM” (Petaling Jaya, Prelaid) → kept both; second one named “WISMA IJM PETALING JAYA” in CSV to avoid name clash in DB (BuildingService rejects duplicate by name within company).

### 4.2 Against existing DB

- Handled by **BuildingService.CreateBuildingAsync**: duplicate by **name** (case-insensitive, same company) or by **address** (AddressLine1 + City + Postcode) throws `InvalidOperationException` with “already exists”.
- **Import controller:** Catches that exception, does not increment ErrorCount or SuccessCount, adds an error entry with “Skipped (duplicate): …”. Import continues and is idempotent on re-run.

### 4.3 Ambiguous / manual review

- Rows with very generic names (e.g. “SDU”, “Jalan Besar”) or multiple possible locations were kept in the CSV with best-effort city/state. If such a building already exists with a different city, the import will skip the new row (duplicate name). No separate “manual review” list was excluded from the CSV; operator can review import result errors for “Skipped (duplicate)” and merge or adjust in UI if needed.

---

## 5. Import result summary

- **Total source rows (normalized CSV):** 212 data rows (excluding header).
- **Import execution:** Not run in this session (API was not running). To run: start the API, then use **Option A** (UI upload of `buildings_import_normalized.csv`) or **Option B** (`Run-BuildingImport.ps1`). After running:
  - **Inserted:** number of new buildings created.
  - **Updated:** N/A (import only creates).
  - **Skipped as duplicates:** count of errors where message starts with “Skipped (duplicate)”.
  - **Flagged for manual review:** none automatically; review any “Skipped (duplicate)” or validation errors in the import result.
  - **Failed rows:** ErrorCount from the import result (e.g. missing InstallationMethod name or invalid data).

---

## 6. Manual review list

- **WISMA IJM** – Two buildings: one KL (Non-Prelaid), one Petaling Jaya (Prelaid). Second named “WISMA IJM PETALING JAYA” in CSV to avoid duplicate name. Confirm in UI after import if both are desired.
- **CROWN PLAZA** vs **CROWN PLAZA CITY CENTER** – Both in source; kept as separate rows. No change.
- **Generic / road names** (e.g. “Jalan Cassia”, “Jalan station”, “Jalan Selar”) – Imported with best-effort city/state; if duplicates by name exist, they will be skipped.
- No rows were removed as “ambiguous” without inclusion; all 212 normalized rows are in the CSV.

---

## 7. Safety confirmation

- **Buildings CRUD:** Unchanged; import uses existing `CreateBuildingAsync` only (no delete/update).
- **Merge flow:** Unchanged; merge endpoints and logic not modified.
- **Order building selection:** Unchanged; new buildings become available in building list/dropdown as before.
- **Parser building matching:** Unchanged; matching uses name/postcode/city; normalized City/State/Name improve match quality.
- **Default materials / rules:** Unchanged; import does not create BuildingDefaultMaterials or BuildingRules; those remain configurable per building in the UI.

---

## 8. Files touched

| File | Change |
|------|--------|
| `backend/src/CephasOps.Api/Controllers/BuildingsController.cs` | Catch `InvalidOperationException` when message contains “already exists”; add error with “Skipped (duplicate): …” and continue (idempotent import). |
| `backend/scripts/buildings_import_normalized.csv` | New; 212 rows, normalized and deduped; BuildingCsvDto format. |
| `backend/scripts/Run-BuildingImport.ps1` | New; PowerShell script to login and POST CSV to `/api/buildings/import`. |
| `backend/scripts/BUILDING_IMPORT_RUNBOOK.md` | New; runbook for UI and script import. |
| `backend/scripts/BUILDING_IMPORT_IMPLEMENTATION_REPORT.md` | This report. |

---

## 9. How to run the import

1. Ensure **InstallationMethod** records exist with Names: `Prelaid`, `Non-Prelaid`, `SDU`, `RDF POLE` (from seed or backup).
2. Start the API (e.g. `cd backend/src/CephasOps.Api && dotnet run`).
3. Either:
   - **UI:** Log in → Settings → Buildings → Import → upload `backend/scripts/buildings_import_normalized.csv`.
   - **Script:** From `backend/scripts`:  
     `.\Run-BuildingImport.ps1 -ApiBaseUrl "http://localhost:5000" -Email "simon@cephas.com.my" -Password "J@saw007"`.
4. Check the import result (success count, error count, skipped duplicates) and fix any validation errors if needed.
