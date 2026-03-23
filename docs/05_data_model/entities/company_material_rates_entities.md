\# COMPANY\_MATERIAL\_RATES\_ENTITIES.md

\## Company Material Rates – Data Model



This document defines the \*\*data-layer entities\*\* used to attach

company-specific pricing and costing to \*\*global material templates\*\*.



Global = `MaterialTemplate`  

Per Company = `CompanyMaterialRate` (+ optional `RateProfile`)



---



\## 1. MaterialTemplate (Global)



> Defined fully in the Material Template data model.  

> Shown here only to give context.



\- `MaterialTemplate`

&nbsp; - `Id` (PK, UUID)

&nbsp; - `Code` (string, unique, e.g. `ONT001`, `FIBER10M`)

&nbsp; - `Name` (string)

&nbsp; - `Category` (string; e.g. `ONT`, `CABLE`, `SPLITTER`, `ACCESSORY`)

&nbsp; - `UnitOfMeasure` (string; e.g. `PCS`, `MTR`, `SET`)

&nbsp; - `IsActive` (bool)

&nbsp; - `CreatedAt`

&nbsp; - `CreatedByUserId`

&nbsp; - `UpdatedAt`

&nbsp; - `UpdatedByUserId`



Constraints:



\- Unique index on `Code`.

\- `Name` required, `Code` required.



---



\## 2. CompanyMaterialRate



Represents the \*\*pricing/costing\*\* of a material \*\*per company\*\*, optionally

scoped by a \*\*rate profile\*\*.



\- `CompanyMaterialRate`

&nbsp; - `Id` (PK, UUID)

&nbsp; - `CompanyId` (FK → Company)

&nbsp; - `MaterialTemplateId` (FK → MaterialTemplate)

&nbsp; - `RateProfileCode` (nullable string; see RateProfile section)

&nbsp; - `ClientPrice` (decimal(18,2), nullable)

&nbsp; - `InternalCost` (decimal(18,2), nullable)

&nbsp; - `InstallerPayout` (decimal(18,2), nullable)

&nbsp; - `Taxable` (bool, default true)

&nbsp; - `IsActive` (bool, default true)

&nbsp; - `Notes` (nullable string)

&nbsp; - `CreatedAt`

&nbsp; - `CreatedByUserId`

&nbsp; - `UpdatedAt`

&nbsp; - `UpdatedByUserId`



Constraints:



\- Unique composite index on:

&nbsp; - `(CompanyId, MaterialTemplateId, RateProfileCode, IsActive WHERE IsActive = 1)`

\- `CompanyId` and `MaterialTemplateId` required.

\- `ClientPrice`, `InternalCost`, `InstallerPayout` must be ≥ 0 when not null.



---



\## 3. RateProfile (Optional)



If you want rate profiles as first-class records (recommended for clarity):



\- `RateProfile`

&nbsp; - `Id` (PK, UUID)

&nbsp; - `CompanyId` (FK → Company)

&nbsp; - `Code` (string; e.g. `DEFAULT`, `TIME\_FTTH`, `TIME\_ASSURANCE`, `KINGS\_MAN`)

&nbsp; - `Name` (string; display name)

&nbsp; - `Description` (nullable)

&nbsp; - `IsDefault` (bool)

&nbsp; - `IsActive` (bool)

&nbsp; - `CreatedAt`

&nbsp; - `CreatedByUserId`

&nbsp; - `UpdatedAt`

&nbsp; - `UpdatedByUserId`



Constraints:



\- Unique index on `(CompanyId, Code)`.

\- At most one `IsDefault = true` per company (`filtered unique index`).

\- `Code` required, `Name` required.



---



\## 4. Value Rules and Validation



While not enforced at DB level, these rules should be enforced at the

application layer:



\- `InstallerPayout <= ClientPrice` when both are set (for most business cases).

\- If `ClientPrice` is null and `InternalCost` is not null:

&nbsp; - ClientPrice can be computed using default margin

&nbsp;   (see `DefaultMaterialMarginPercent` in Global Settings).

\- If `MaterialRateStrictMode` is true and no record is found for a material

&nbsp; used in billing/payout:

&nbsp; - Operation should fail with a configuration error.



