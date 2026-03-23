Below is the FULL PRODUCTION VERSION of

multi\_company\_relationships.md — rewritten, expanded, and aligned with the ENTIRE CephasOps architecture:



Multi-company



Multi-branch



Multi-partner



Rates



Building \& Splitter



Document Templates



Orders



Email Parser routing



UI read-only consumption



This version is complete, not a placeholder, and fits directly into:



docs/02\_modules/ → multi\_company\_entities.md

docs/02\_modules/ → multi\_company\_relationships.md



multi\_company\_relationships.md

Full Production Version — CephasOps Multi-Company Relationships



The Multi-Company Engine enables CephasOps to operate multiple legal entities, each with their own:



Rates



Installers



Branches



Building databases



Document templates



Billing rules



Partner accounts



Parser logic



Branding



Languages



All operational modules must support scoping by companyId as a first-class design requirement.



1\. Top-Level Relationship Diagram

Company

│

├── CompanySettings

│

├── Branch (optional)

│

├── CompanyPartnerLink      → Partner accounts (TIME / Digi / Celcom)

│

├── RateProfile             → Activation rates, Assurance rates, Mod rates, travel, materials

│   └── RateProfileItem

│

├── DocumentTemplate        → Work Order PDFs, Dockets, Invoices, Emails

│

├── BuildingProfile         → Category-level building groups

│

├── Building                → Actual building DB

│   └── BuildingSplitter

│       └── BuildingSplitterPort

│

├── Order                   → All job types

│   └── BillingRecord

│   └── InstallerPayoutRecord

│

└── ParseSession            → Routed by CompanyPartnerLink





This ensures EVERYTHING in CephasOps flows through companyId.



2\. Company Relationships (Core)

2.1 Company → CompanySettings



A Company can have many settings versions, but only one active configuration.



Relationship:

Company 1 - N CompanySettings

Usage:

Defines branding, invoice prefix, default RateProfile, email settings, colours, language.



Example:



Cephas Sdn Bhd → defaultLanguage: en



Menorah Travel → primaryColor: blue



Kingsman Classic → invoicePrefix: KGC-



2.2 Company → Branch



Branches allow physical operations.



Relationship:

Company 1 - N Branch



Examples:



Kingsman Classic Services



Branch: Kelana Jaya



Branch: HICOM



Cephas Trading \& Services



Branch: HQ Subang



Branch: Warehouse



3\. Partner Relationships

3.1 Company → CompanyPartnerLink



Maps upstream providers to each Company.



Relationship:

Company 1 - N CompanyPartnerLink



Example:



Company	Partner	Vendor Code

Cephas Trading	TIME	TERA001

Cephas Trading	Digi	DIGI332

Cephas Trading	Celcom	CEL909

Kingsman Classic	TIME	KGC121



Purpose:



Email Parser determines which Company an inbound email belongs to.



Order creation scoping.



Partner-specific invoice/docket template selection.



Flow:

Email → Partner detection → CompanyPartnerLink → companyId → ParseSession



4\. Rate Relationships

4.1 Company → RateProfile



Each company may have multiple rate profiles:



Installer Payout Rates



Activation Rates



Assurance Rates



Travel Allowances



Material Markup Profiles



Relationship:

Company 1 - N RateProfile

RateProfile 1 - N RateProfileItem



Example:



Cephas Trading \& Services



TIME\_DEFAULT\_2025



CEPHAS\_ASSURANCE\_2025



INTERNAL\_INSTALLER\_PAYOUT\_2025



RateProfileItem Examples:



FTTH Activation: RM 60



FTTO Activation: RM 80



Outdoor Relocation: RM 120



Mileage: RM 0.50/km



ONU Replacement: RM 25



Order Flow:

Order → Determine companyId → Apply RateProfile → Calculate payout/billing



5\. Building \& Splitter Relationships

5.1 Company → BuildingProfile



BuildingProfile is a template for building categories.



Relationship:

Company 1 - N BuildingProfile



Examples:



FAMILY\_MART\_STD



RETAIL\_SMALL\_FORMAT



HIGHRISE\_CONDO\_STD



FACTORY\_PROFILE\_A



5.2 Company → Building



Each building belongs to exactly one company.



Relationship:

Company 1 - N Building



5.3 Building → Splitter System



Splitter templates and port details.



Relationship:

Building 1 - N BuildingSplitter

BuildingSplitter 1 - N BuildingSplitterPort



Used for:



FTTH planning



Installer job routing



Tracking active ports (used/free/faulty)



UI Behaviour:

The UI only reads building + splitter data, never creates new ones unless via Settings module.



6\. Document Template Relationships



Templates are always scoped by companyId and sometimes partnerCode.



6.1 Company → DocumentTemplate



Relationship:

Company 1 - N DocumentTemplate



Examples:



Work Order PDF template



Invoice PDF template



Docket template



Email (HTML) templates



WhatsApp templates



6.2 DocumentTemplate → Placeholder Definitions



This is a global repository but relates indirectly to each template.



Relationship:

DocumentPlaceholderDefinition N - N DocumentTemplate



Used by UI to show required/optional placeholders.



7\. Order \& Billing Relationships

7.1 Company → Order



Every Order belongs to a single Company.



Relationship:

Company 1 - N Order



7.2 Order → BillingRecord



Billing for the partner (TIME/Digi) or client.



Relationship:

Order 1 - N BillingRecord



7.3 Order → InstallerPayoutRecord



Installer earnings derived from RateProfile.



Relationship:

Order 1 - N InstallerPayoutRecord



Flow:

Order → companyId → RateProfile → RateProfileItem → PayoutRecord



8\. Parser Relationships

8.1 Email → ParseSession → Order



Parser output is always company-aware.



Relationship:



Email 

&nbsp;→ ParseSession (companyId from CompanyPartnerLink) 

&nbsp;→ Approve 

&nbsp;→ Order (companyId inherited)

&nbsp;→ SnapshotCleanupJob





This ensures:



Emails route to correct company



Orders cannot be cross-company



Rate profiles apply correctly



Billing is isolated per company



8.2 ParseSession → CompanyPartnerLink



Parser matches:



FROM email address



Subject keywords



Partner patterns



Excel formats



Service ID formats



To determine CompanyPartnerLink.



Relationship:

ParseSession N - 1 CompanyPartnerLink



9\. UI Relationships

9.1 UI → CompanySettings



UI must ONLY READ:



Company name, logo



Colours



Invoice prefix



Rate profiles



Document templates



Building/splitter data



Partner account information



No hardcoding allowed.



9.2 UI → Multi-Company Permissions



User can switch between companies only if allowed by role:



SuperAdmin → All companies



CompanyAdmin → Own company



Installer → Own company or branch



10\. Combined Master Diagram

Company

│

├── CompanySettings

├── Branch

├── CompanyPartnerLink

│

├── RateProfile ──┬─ RateProfileItem

│

├── DocumentTemplate

│

├── BuildingProfile

│   └── Building ──┬─ BuildingSplitter ──┬─ BuildingSplitterPort

│

├── ParseSession (via Email Parser → PartnerLink)

│

└── Order ──┬─ BillingRecord

&nbsp;           └─ InstallerPayoutRecord





Everything connects through companyId as the root.



11\. Summary (What You Wanted)



✔ Multi-company is enforced across ALL modules

✔ Company configs (rates, building, splitters, templates) live in backend

✔ UI is read-only consumer

✔ Email parser uses CompanyPartnerLink to route sessions

✔ Orders inherit companyId from ParseSession

✔ Everything is modular, scalable, multi-tenant ready

