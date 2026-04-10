# Repair duplicate Assurance parent (Order Types)

## Root cause of why delete was blocked

Deletion is blocked in `OrderTypeService.DeleteOrderTypeAsync` when **any** of these tables reference the order type ID:

| Table | FK column | Checked in code |
|-------|-----------|------------------|
| **Orders** | OrderTypeId | Yes (previously the only check; message was "used by existing orders") |
| JobEarningRecords | OrderTypeId | Yes (now) |
| BuildingDefaultMaterials | OrderTypeId | Yes (now) |
| BillingRatecards | OrderTypeId | Yes (now) |
| GponPartnerJobRates | OrderTypeId | Yes (now) |
| GponSiJobRates | OrderTypeId | Yes (now) |
| GponSiCustomRates | OrderTypeId | Yes (now) |
| ParserTemplates | OrderTypeId | Yes (now) |
| OrderTypes (children) | ParentOrderTypeId | Yes (parent with subtypes cannot be deleted) |

The duplicate Assurance parent was reported as "used" because at least one of these (most likely **Orders**, or rates/mappings) had rows pointing at its Id. The UI message did not say which table.

## Tables that can reference an OrderType Id

- **Orders** — order type of the order  
- **JobEarningRecords** — payroll  
- **BuildingDefaultMaterials** — default materials by building + order type  
- **BillingRatecards** — billing rates  
- **GponPartnerJobRates** — partner job rates  
- **GponSiJobRates** — SI job rates  
- **GponSiCustomRates** — SI custom rates  
- **ParserTemplates** — parser template order type  
- **OrderTypes** — children (ParentOrderTypeId)

Workflows do **not** store an OrderType Id FK; they use OrderTypeCode (string). So workflows were not blocking delete.

## Inspect duplicate records (diagnostic)

Run the diagnostic script to list all top-level Assurance rows and their reference counts:

```bash
# From project root, with PostgreSQL running and credentials set
psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/diagnose-assurance-duplicates.sql
```

Interpretation:

- **Canonical** = the row we keep: prefer one with child subtypes, then most references (e.g. orders), then oldest CreatedAt.
- **Duplicate** = the extra Assurance parent with no (or fewer) subtypes that we will reassign and then soft-delete.

## Canonical selection rule (repair script)

The repair script picks **one canonical** Assurance parent per CompanyId:

1. Prefer the row with **most child subtypes** (OrderTypes.ParentOrderTypeId = this Id).
2. Then prefer the row with **most Orders** referencing it.
3. Then **DisplayOrder** ascending, then **CreatedAt** ascending.

All other top-level Assurance rows for that company are treated as duplicates.

## What the repair script does

1. **Build duplicate map**  
   For each company, find all top-level ASSURANCE rows; mark one as canonical (rule above), all others as duplicates and store (duplicate_id, canonical_id) in a temp table.

2. **Reassign references**  
   For each duplicate_id, set to canonical_id in:
   - OrderTypes (children: ParentOrderTypeId)
   - Orders
   - JobEarningRecords
   - BuildingDefaultMaterials
   - BillingRatecards
   - GponPartnerJobRates
   - GponSiJobRates
   - GponSiCustomRates
   - ParserTemplates

3. **Soft-delete duplicates**  
   Set IsDeleted = true, DeletedAt = NOW(), UpdatedAt = NOW() on the duplicate OrderTypes rows.

4. **Soft-delete orphan duplicates (cross-company)**  
   Any other top-level ASSURANCE with zero refs and zero children is soft-deleted if at least one other top-level ASSURANCE exists (handles the case where the duplicate was in a different CompanyId and had no references).

5. **Output**  
   Select from the temp table to show (duplicate_id, canonical_id) that were reassigned and soft-deleted.

## Which record was selected as canonical

Determined at run time by the rule above. After running the diagnostic you can confirm: the row that remains visible in the UI (and has the most subtypes or most references, then oldest) is the one the script chose as canonical.

## References updated

All of the tables listed in the “Tables that can reference an OrderType Id” section that had the duplicate’s Id are updated to point to the canonical Assurance Id. Company scoping is preserved because we only merge within the same CompanyId.

## Duplicate: hard delete or soft delete?

**Repair script:** Uses **hard delete** (DELETE) so duplicate rows are removed from the table. Step 3 deletes duplicates after reassigning refs; Step 4 deletes orphan duplicates with zero refs/children.

**One-off cleanup:** The duplicate that had been soft-deleted was later **hard-deleted** via `backend/scripts/hard-delete-soft-deleted-assurance.sql` (after reassigning its 1 child to the canonical Assurance).

## Migration / repair script / admin command

- **No EF migration added.** The fix is a one-off repair.
- **Repair script (run manually):**  
  `backend/scripts/repair-duplicate-assurance-parent.sql`
- **Diagnostic script (run first):**  
  `backend/scripts/diagnose-assurance-duplicates.sql`
- **Admin command:** None. Run the SQL scripts with psql (or your DB client).

**How to run (example):**

```bash
# 1) Diagnose (optional but recommended)
$env:PGPASSWORD = "YourPassword"
psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/diagnose-assurance-duplicates.sql

# 2) Repair
psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/repair-duplicate-assurance-parent.sql
```

## Prevention

- **Duplicate creation:** The unique index `IX_OrderTypes_CompanyId_Code_Parents` (CompanyId, Code WHERE ParentOrderTypeId IS NULL) prevents creating a second top-level Assurance for the same company.
- **Clearer delete message:** `OrderTypeService.DeleteOrderTypeAsync` now checks all FK tables above and returns a single message listing what references the record (e.g. “used by: 3 order(s), 2 billing ratecard(s)”) and suggests the repair script for merging duplicate parents.

## Manual verification

After running the repair script:

1. **Settings > GPON > Order Types**  
   - Only one Assurance parent row.  
   - No duplicate “Assurance” with no subtypes.

2. **Subtypes**  
   - Select Assurance; subtypes (e.g. Standard, Repull) still show and work.

3. **Create Order**  
   - Assurance and its subtypes still appear and can be selected.

4. **Data**  
   - Orders (and other entities) that previously pointed at the duplicate now point at the canonical Assurance; no broken FKs.

5. **Delete**  
   - The duplicate is soft-deleted (not visible in UI); the canonical Assurance can be deleted only if it has no references (and no subtypes, if applicable).

## Summary

| Item | Result |
|------|--------|
| **Root cause of block** | One or more of Orders, JobEarningRecords, BuildingDefaultMaterials, BillingRatecards, GponPartnerJobRates, GponSiJobRates, GponSiCustomRates, ParserTemplates (or child OrderTypes) referenced the duplicate Assurance Id. |
| **Tables that could reference duplicate** | All of the above (see list). |
| **Canonical** | Chosen per company by: most child subtypes → most Orders → DisplayOrder → CreatedAt. |
| **References updated** | All FKs listed above that pointed at the duplicate Id are updated to the canonical Id. |
| **Duplicate removal** | Soft-deleted (IsDeleted = true). |
| **Scripts** | `diagnose-assurance-duplicates.sql`, `repair-duplicate-assurance-parent.sql`. |
| **Code change** | Delete error message in OrderTypeService now lists which tables reference the order type and mentions the repair script. |
