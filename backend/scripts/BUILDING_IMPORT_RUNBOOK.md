# Building list import – runbook

One-time import of the normalized building list into CephasOps using the **existing Buildings import API** (idempotent: duplicates are skipped).

---

## 1. Prerequisites

- Backend API running (e.g. `cd backend/src/CephasOps.Api && dotnet run` or `dotnet watch`).
- **Database up to date** with all migrations applied (e.g. idempotent script or `dotnet ef database update`). If the DB is missing tables/columns (e.g. `JobRuns`, `BackgroundJobs.RetriedFromJobRunId`), login and other requests may return 500.
- Admin credentials (e.g. from AGENTS.md: `simon@cephas.com.my` / `J@saw007`).
- Database has **InstallationMethod** records (from seed or backup). The import accepts both canonical seed names and common aliases (see § InstallationMethod lookup below).

---

## 2. Option A – Upload via UI

1. Log in to the app as an admin user (company context is applied automatically).
2. Go to **Settings → Buildings** (or the Buildings module).
3. Use **Import** and upload:  
   `backend/scripts/buildings_import_normalized.csv`
4. Confirm success/error/skip counts in the import result.

---

## 3. Option B – Run import via script (PowerShell)

From repo root, with the API already running on the configured base URL:

```powershell
cd backend/scripts
.\Run-BuildingImport.ps1 -ApiBaseUrl "http://localhost:5000" -Email "simon@cephas.com.my" -Password "J@saw007"
```

- `ApiBaseUrl`: base URL of the API (no trailing slash).
- `Email` / `Password`: admin user for login.  
- Script logs in, then POSTs the CSV to `POST /api/buildings/import` and prints the JSON result.

---

## 4. CSV format (BuildingCsvDto)

| Column                 | Usage |
|------------------------|--------|
| Name                   | Building name (required). |
| Code                   | Optional. |
| PropertyType           | Optional; maps to BuildingType by name if present. |
| InstallationMethodName | **Required**. Matched by canonical name or alias: `Prelaid`; `Non-Prelaid` / `Non Prelaid` → Non-prelaid; `SDU` / `RDF POLE` / `RDF Pole` → SDU/RDF. |
| DepartmentName         | Optional. |
| AddressLine1           | Required (location/address line). |
| AddressLine2           | Optional. |
| City                   | Required. |
| State                  | Required. |
| Postcode               | Required (use `00000` if unknown). |
| Area, Notes            | Optional. |
| IsActive               | `true` / `false`. |

---

## 4b. InstallationMethod lookup

- The API loads `InstallationMethod` by company and builds a case-insensitive lookup by **Name**.
- Seeded names are e.g. `Prelaid`, `Non-prelaid (MDU / old building)`, `SDU / RDF Pole` (Codes: PRELAID, NON_PRELAID, SDU_RDF).
- The import adds **aliases** so CSV values resolve to the same records: `Non-Prelaid`, `Non Prelaid` → NON_PRELAID; `SDU`, `RDF POLE`, `RDF Pole` → SDU_RDF. No seed or CSV changes required.

---

## 5. Duplicate handling (idempotent)

- The import uses `BuildingService.CreateBuildingAsync`, which throws if a building with the **same name** (case-insensitive, same company) or **same address** (AddressLine1 + City + Postcode) already exists.
- The controller catches that and records a **skip** (duplicate) instead of failing the whole import.
- Re-running the same CSV will **skip** existing buildings and only insert new ones.

---

## 6. Company scoping

- Company is taken from the **current user** (JWT) when using the API.
- All created buildings are created for that user’s company.

---

## 7. Files

- **Normalized CSV:** `backend/scripts/buildings_import_normalized.csv`
- **Import script:** `backend/scripts/Run-BuildingImport.ps1`
- **Controller:** `BuildingsController.cs` – `POST /api/buildings/import` (idempotent duplicate handling).
