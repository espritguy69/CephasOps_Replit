\# SETTINGS\_MATERIAL\_TEMPLATES\_MODULE.md

Default Material Templates – Full Backend Specification



---



\## 1. Purpose



Provide \*\*default material “kits”\*\* per:



\- Order type

\- Building type

\- Partner



So that for typical jobs, materials are pre-suggested instead of manually selected.



---



\## 2. Data Model



\### 2.1 MaterialTemplate



Represents a kit template.



Fields:



\- `Id`

\- `CompanyId`

\- `Name` (e.g. "TIME Prelaid High-Rise kit")

\- `OrderType` (Activation, Assurance, FTTR, FTTC, SDU, RDFPole, etc.)

\- `BuildingTypeId` (nullable)

\- `PartnerId` (nullable)

\- `IsDefault` (one default per `(CompanyId, OrderType, BuildingTypeId)` if no partner-specific)

\- `IsActive`

\- `CreatedAt`, `CreatedByUserId`

\- `UpdatedAt`, `UpdatedByUserId`



Indexes:



\- `(CompanyId, OrderType, BuildingTypeId, PartnerId)`



---



\### 2.2 MaterialTemplateItem



Fields:



\- `Id`

\- `MaterialTemplateId`

\- `MaterialId`

\- `Quantity` (decimal)

\- `UnitOfMeasure`

\- `IsSerialised` (mirror of Material)

\- `Notes`



---



\## 3. Integration with Orders \& Inventory



\### 3.1 Apply Template to Order



When order is created (or building chosen), system:



1\. Resolves template by:

&nbsp;  - `CompanyId`, `PartnerId`, `OrderType`, `BuildingTypeId`

&nbsp;  - Fallback to default template per OrderType + BuildingType

2\. Creates `OrderMaterial` rows (planned usage) based on `MaterialTemplateItem`.

3\. Inventory still controls actual stock movement and usage.



---



\## 4. Services



\### 4.1 MaterialTemplateService



Methods:



\- `Task<MaterialTemplate?> GetEffectiveTemplateAsync(companyId, partnerId, orderType, buildingTypeId)`

\- `Task<IEnumerable<MaterialTemplate>> ListTemplatesAsync(companyId, filters...)`

\- `Task<MaterialTemplate> CreateTemplateAsync(...)`

\- `Task<MaterialTemplate> UpdateTemplateAsync(...)`



\### 4.2 OrderMaterialTemplateApplier



\- Called from Orders module when:

&nbsp; - New order created

&nbsp; - Building assigned/changed

\- Creates OrderMaterial rows for planned materials.



---



\## 5. API



Base path: `/api/material-templates`



\- `GET /api/material-templates`

\- `GET /api/material-templates/{id}`

\- `POST /api/material-templates`

\- `PUT /api/material-templates/{id}`

\- `POST /api/material-templates/{id}/set-default`



---



\## 6. Security



\- Admin / Warehouse roles manage templates.

\- Orders API uses templates internally; SI App does not edit them.





