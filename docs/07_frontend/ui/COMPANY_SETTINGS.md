\# UI — Company Settings



This document defines the \*\*Company Settings UI\*\* for CephasOps.



The goal is to make the UI a \*\*thin, read-only consumer\*\* of backend configuration for:



\- Company identity \& branding

\- Default operational settings

\- Multi-company switching

\- Partner link visibility (TIME / Digi / Celcom / etc.)

\- Snapshot \& parser config (display only)



All write operations go through typed APIs; \*\*no hard-coded values\*\* in React.



---



\## 1. Scope \& Screens



The Company Settings module includes:



1\. \*\*Company Switcher (Header)\*\*

2\. \*\*Company Profile \& Branding\*\*

3\. \*\*Operational Defaults\*\*

4\. \*\*Partner Links Overview\*\*

5\. \*\*Email \& Parser Settings (Read-Only)\*\*

6\. \*\*Snapshot \& Retention Settings (Read-Only)\*\*



Path suggestions:



\- `/settings/company`

\- `/settings/company/partners`

\- `/settings/company/email`



---



\## 2. Entities (Frontend View Models)



The UI works with the following backend entities:



\- `Company`

\- `CompanySettings` (active)

\- `Branch\[]` (listing only)

\- `CompanyPartnerLink\[]`

\- Global read-only settings related to email/snapshot



Example view model:



```ts

type CompanyView = {

&nbsp; id: string;

&nbsp; code: string;

&nbsp; displayName: string;

&nbsp; registrationNo?: string;

&nbsp; taxNo?: string;

&nbsp; mainAddress: string; // formatted

&nbsp; isActive: boolean;

};



type CompanySettingsView = {

&nbsp; companyId: string;

&nbsp; defaultTimezone: string;

&nbsp; defaultCurrency: string;

&nbsp; logoUrl?: string;

&nbsp; primaryColor?: string;

&nbsp; secondaryColor?: string;

&nbsp; invoicePrefix?: string;

&nbsp; workOrderPrefix?: string;

&nbsp; billingDayOfMonth?: number;

&nbsp; defaultLanguage: string;

&nbsp; isMultiPartnerEnabled: boolean;

&nbsp; isMultiBranchEnabled: boolean;

&nbsp; defaultRateProfileId?: string;

};



type CompanyPartnerLinkView = {

&nbsp; id: string;

&nbsp; partnerCode: string;          // TIME, DIGI, CELCOM, etc.

&nbsp; partnerDisplayName: string;

&nbsp; upstreamAccountNo?: string;   // vendor code

&nbsp; billingEmail?: string;

&nbsp; supportEmail?: string;

&nbsp; isActive: boolean;

};



3\. Company Switcher (Global)



Purpose: allow users to switch between companies they have access to.



3.1 Behaviour



Lives in the top-right header (or sidebar).



Displays:



Current Company.displayName



Optional Company.code badge



Optional logo (from CompanySettings.logoUrl)



On click:



Opens a dropdown with a search/filter list of companies.



Only shows companies where the user has permission.



3.2 API Contract (Example)

GET /api/me/companies

→ returns CompanyView\[]



PATCH /api/me/active-company

Body: { companyId: string }

→ sets active company for current session





The active companyId is then used in all other API calls (rate profiles, templates, orders, etc.).



4\. Company Profile \& Branding Screen

4.1 Layout



Sections:



Basic Info



Branding



Operational Defaults



Basic Info



Fields (read-only or editable depending on permission):



Company Name



Company Code



Registration No



Tax No



Address



Country



Branding



Company Logo upload (if permitted)



Primary Color (color picker)



Secondary Color (color picker)



Preview card with logo + colours



Operational Defaults



Default Timezone



Default Currency



Default Language



Invoice Prefix (e.g. CEPH-)



Work Order Prefix



Billing Day of Month



4.2 API Contract (Example)

GET /api/companies/{companyId}

GET /api/companies/{companyId}/settings



PUT /api/companies/{companyId}/settings

Body: CompanySettingsView (partial)



4.3 Permissions



ROLE\_SUPER\_ADMIN → can edit all fields



ROLE\_COMPANY\_ADMIN → can edit most fields, except legal identifiers (registrationNo, taxNo)



Other roles → read-only



5\. Partner Links Overview



This screen shows how the current company is linked to upstream partners.



5.1 UI Layout



Table:



Partner	Vendor Code	Billing Email	Support Email	Status



Optional actions:



Add partner link



Edit existing partner link



Toggle active/inactive



5.2 API Contracts (Example)

GET /api/companies/{companyId}/partners

→ CompanyPartnerLinkView\[]



POST /api/companies/{companyId}/partners

PUT /api/company-partners/{id}



5.3 Email Parser Hint



Display a small info panel:



Email Parser Routing



Incoming emails from TIME / Digi / Celcom are mapped to this Company using:



Partner FROM address



Service ID patterns



CompanyPartnerLink.vendor code

This mapping is defined in backend configuration (Email Parser module).



No parser logic in UI; just information.



6\. Email \& Parser Settings (Read-Only in UI)



This tab shows visibility into parser-related settings for the current company, without allowing front-end modifications (only backend admins should change them).



6.1 Fields (Read-Only)



Drawn from Global Settings + CompanySettings:



IMAP Host (masked)



IMAP Port



SSL Enabled (yes/no)



Inbound Folder (e.g. INBOX/TIME)



Processed Folder



Error Folder



Polling Interval (seconds)



Snapshot Retention Days



6.2 API Contract (Example)

GET /api/companies/{companyId}/email-settings

→ merged view of global + company overrides





The UI must not show passwords or tokens.



7\. Snapshot \& Retention (Read-Only)



Small panel in the same Email tab:



Snapshot retention: email.snapshotRetentionDays (e.g. 7 days)



Last SnapshotCleanupJob run timestamp



Number of snapshots cleaned in last run (optional)



This ties visually to the backend flow:



Email → ParseSession → Approve → Order → SnapshotCleanupJob



8\. Branch Listing (Optional)



In Company Settings, include a “Branches” subsection:



Simple table with:



Branch Name



Code



Address



Status



This is primarily informational here; detailed branch management may live in a separate module.



9\. Storybook Guidelines



Create Storybook stories for:



<CompanySwitcher />



Default



Multiple companies



Single company only (no dropdown)



Loading state



<CompanySettingsForm />



Read-only mode



Editable (admin) mode



Validation errors



<PartnerLinksTable />



No partners



TIME + Digi + Celcom active



Some inactive



<EmailSettingsPanel />



Show different snapshot retention days (e.g. 7, 30)



All stories should use mock data that matches our backend models.



10\. Non-Goals



UI must NOT:



Edit parser rules



Edit global IMAP passwords



Edit internal snapshot paths



These belong to backend-only admin tools or infra configuration.



The UI is informational + company-level settings only.



