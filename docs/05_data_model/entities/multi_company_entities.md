\# MULTI\_COMPANY\_ENTITIES.md



CephasOps is designed as a \*\*multi-company, multi-partner\*\* platform.



A "Company" in CephasOps can be:



\- A primary operator (e.g. Cephas Sdn Bhd)

\- A sub-company (e.g. Menorah Travel \& Tours, Kingsman Classic Services)

\- An ISP-facing legal entity

\- A white-labelled client that uses CephasOps as their internal system



All operational modules (Orders, Stock, Installers, Billing, KPI, etc.) must be scoped by `companyId`.



---



\## 1. Core Entities



\### 1.1 Company



Represents a legal or operational entity.



\- `Company`

&nbsp; - `id` (UUID)

&nbsp; - `code` (short code: `CEPHAS`, `MENORAH`, `KINGS`)

&nbsp; - `displayName`

&nbsp; - `registrationNo`

&nbsp; - `taxNo` (optional)

&nbsp; - `addressLine1`

&nbsp; - `addressLine2`

&nbsp; - `city`

&nbsp; - `state`

&nbsp; - `postcode`

&nbsp; - `country`

&nbsp; - `contactPhone`

&nbsp; - `contactEmail`

&nbsp; - `isActive`

&nbsp; - `createdAt`

&nbsp; - `updatedAt`



All other entities that are "owned" by a company MUST have a `companyId` FK.



---



\### 1.2 CompanySettings



Company-specific configurable behaviour (NOT global defaults).



\- `CompanySettings`

&nbsp; - `id`

&nbsp; - `companyId` (FK → Company)

&nbsp; - `defaultTimezone`

&nbsp; - `defaultCurrency` (e.g. MYR)

&nbsp; - `logoUrl`

&nbsp; - `primaryColor`

&nbsp; - `secondaryColor`

&nbsp; - `invoicePrefix` (e.g. `CEPH-`)

&nbsp; - `workOrderPrefix`

&nbsp; - `billingDayOfMonth` (optional)

&nbsp; - `defaultRateProfileId` (FK → `RateProfile`)

&nbsp; - `defaultBuildingProfileId` (optional, FK → `BuildingProfile`)

&nbsp; - `defaultLanguage` (e.g. `en`, `ms`)

&nbsp; - `isMultiPartnerEnabled` (bool)

&nbsp; - `isMultiBranchEnabled` (bool)



---



\### 1.3 Branch



Physical branches under a Company (e.g. Kingsman Kelana Jaya, Kingsman HICOM).



\- `Branch`

&nbsp; - `id`

&nbsp; - `companyId`

&nbsp; - `code`

&nbsp; - `displayName`

&nbsp; - `addressLine1..country`

&nbsp; - `lat` / `lng` (optional)

&nbsp; - `isActive`

&nbsp; - `createdAt`

&nbsp; - `updatedAt`



Orders, stock, and installer assignments can optionally be scoped by `branchId`.



---



\### 1.4 RateProfile



A \*\*rate card\*\* that can be shared across companies or dedicated to one company.



\- `RateProfile`

&nbsp; - `id`

&nbsp; - `companyId` (nullable; if null → reusable template)

&nbsp; - `code` (e.g. `TIME\_DEFAULT\_2025`, `CEPHAS\_INT\_2025`)

&nbsp; - `displayName`

&nbsp; - `effectiveFrom`

&nbsp; - `effectiveTo` (nullable)

&nbsp; - `currency`

&nbsp; - `isDefaultForCompany` (bool)

&nbsp; - `createdAt`

&nbsp; - `updatedAt`



\- `RateProfileItem`

&nbsp; - `id`

&nbsp; - `rateProfileId`

&nbsp; - `category` (e.g. `ACTIVATION`, `ASSURANCE`, `MODIFICATION`, `TRAVEL`, `MATERIAL\_MARKUP`)

&nbsp; - `subCategory` (e.g. `FTTH`, `FTTO`, `AWO`, `TTKT`, `OUTDOOR\_RELO`)

&nbsp; - `uom` (e.g. `JOB`, `METER`, `POINT`)

&nbsp; - `baseRate`

&nbsp; - `minAmount` (optional)

&nbsp; - `maxAmount` (optional)

&nbsp; - `isActive`



\*\*Important:\*\*



\- Installer payouts and client billing \*\*must never hardcode rates in code\*\*.

\- All pricing logic reads from `RateProfile` + `RateProfileItem` for the current `companyId`.



---



\### 1.5 BuildingProfile



A high-level template representing groups of buildings for a company.



\- `BuildingProfile`

&nbsp; - `id`

&nbsp; - `companyId`

&nbsp; - `code` (e.g. `FAMILY\_MART\_STD`, `CONDO\_HIGHRISE`)

&nbsp; - `displayName`

&nbsp; - `description`

&nbsp; - `typicalStoreAreaSqft` (optional)

&nbsp; - `defaultSplitterTemplateId` (FK → `SplitterTemplate`)

&nbsp; - `defaultVisitDurationMinutes` (optional)

&nbsp; - `createdAt`

&nbsp; - `updatedAt`



---



\### 1.6 CompanyPartnerLink



Links a Company to upstream partners (TIME, Digi, Celcom, etc.) and their codes.



\- `CompanyPartnerLink`

&nbsp; - `id`

&nbsp; - `companyId`

&nbsp; - `partnerCode` (e.g. `TIME`, `DIGI`, `CELCOM`)

&nbsp; - `partnerDisplayName`

&nbsp; - `upstreamAccountNo` (e.g. vendor code)

&nbsp; - `billingEmail`

&nbsp; - `supportEmail`

&nbsp; - `isActive`



The Email Parser uses `CompanyPartnerLink` + Global Settings to route incoming emails to the correct `companyId`.



---



\## 2. Relationships



\- `Company 1 - N Branch`

\- `Company 1 - N CompanySettings` (latest/active used)

\- `Company 1 - N RateProfile`

\- `RateProfile 1 - N RateProfileItem`

\- `Company 1 - N BuildingProfile`

\- `BuildingProfile 1 - N Building` (detailed in `splitter\_entities.md`)

\- `Company 1 - N CompanyPartnerLink`



All Orders, Materials, Installers, and Templates must include:



\- `companyId`

\- Optional `branchId`

\- Optional `buildingId`



