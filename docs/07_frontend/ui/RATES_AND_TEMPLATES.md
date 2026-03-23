\# UI — Rates \\\& Templates







This document defines the UI for:







1\\. \\\*\\\*Rate Profiles\\\*\\\* (company-level rate cards)



2\\. \\\*\\\*Document Templates\\\*\\\* (invoices, dockets, work orders, email, WhatsApp)







The goal is:







\\- All rates \\\& templates are \\\*\\\*company-scoped\\\*\\\*



\\- UI \\\*\\\*never hardcodes\\\*\\\* RM values or text blocks



\\- React components display \\\& edit data via APIs



\\- Storybook stories are aligned with the backend structure







---







\\## 1. Scope \\\& Screens







\\### 1.1 Rate Profiles







\\- List of rate profiles per company



\\- Detail view of a single rate profile with its items



\\- Filtering by category (Activation, Assurance, Modification, Travel, etc.)







\\### 1.2 Document Templates







\\- List of templates per company



\\- Detail editor with:



\&nbsp; - Template body editor (e.g. textarea/Monaco)



\&nbsp; - Placeholder helper sidebar



\&nbsp; - Live preview (using sample JSON)







---







\\## 2. Rate Profiles UI







\\### 2.1 List View







Path suggestion: `/settings/rates`







Columns:







\\- Profile Name



\\- Code



\\- Effective From



\\- Effective To



\\- Currency



\\- Default? (Yes/No)



\\- Status (Active/Expired)







Top actions:







\\- Create New Rate Profile (if permitted)



\\- Filter by status (Active/Expired/All)



\\- Filter by usage (Installer Payout / Client Billing / Internal)







\\### 2.2 Detail View







Sections:







1\\. \\\*\\\*Profile Header\\\*\\\*



2\\. \\\*\\\*Rate Items Table\\\*\\\*



3\\. \\\*\\\*Metadata \\\& Audit\\\*\\\*







\\#### Profile Header







Fields:







\\- Display Name



\\- Code



\\- Currency



\\- Effective From / To



\\- Tags (e.g. `Installer Payout`, `TIME`, `Assurance`)



\\- Toggle: `isDefaultForCompany`







\\#### Rate Items Table







Columns:







\\- Category (ACTIVATION / ASSURANCE / MODIFICATION / TRAVEL / MATERIAL\\\_MARKUP)



\\- Sub-Category (FTTH / FTTO / AWO / TTKT / OUTDOOR\\\_RELO, etc.)



\\- UoM (JOB / METER / POINT)



\\- Base Rate



\\- Min Amount (optional)



\\- Max Amount (optional)



\\- Active (toggle)



\\- Notes (optional)







Inline editing is recommended:







\\- Click cell → edit → auto-save or “Save” button







\\### 2.3 API Contracts (Example)







```http



GET /api/companies/{companyId}/rate-profiles



→ RateProfileView\\\[]







GET /api/rate-profiles/{id}



→ RateProfileView + RateProfileItem\\\[]







POST /api/companies/{companyId}/rate-profiles



PUT /api/rate-profiles/{id}



PUT /api/rate-profile-items/{id}











RateProfileView aligns with backend RateProfile entity.







2.4 Behaviour with Orders







Read-Only in Order UI:







When viewing an Order, the UI may show:







“Rate Profile: TIME\\\_DEFAULT\\\_2025”







“Job Value: RM X”







The calculation comes from backend via an OrderPricing DTO.



The UI does not recalculate rates; it only displays results.







3\\. Document Templates UI



3.1 List View (receipt-management style)







Path: `/settings/document-templates`







View modes (toggle in header):







- **Cards** (default) — grid of template cards; each card shows type icon, name, document type badge, engine, partner (if any), Active/Inactive status, last updated, and action buttons (Activate/Deactivate, Duplicate, Edit, Delete). Clicking the card opens the editor.

- **Table** — same data in a table with sortable columns; row click opens editor.







Type filter (chips):







- **All** plus one chip per document type: Invoice, Job Docket, RMA Form, Purchase Order, Quotation, BOQ, Delivery Order, Receipt. Each chip has an icon and colour; selected type is highlighted.







Search: text search over template name and document type.







Top actions:







- **New Template** — create new template

- **Duplicate** — from list (per template) or from editor; uses `POST /api/document-templates/{id}/duplicate`, then navigates to new template’s editor

- **Activate / Deactivate** — set as default for that type/partner

- **Edit** — open template editor

- **Delete** — remove template (with confirmation)







Columns (table view): Template Name, Type, Partner, Engine, Version, Status (Active/Inactive), Updated, Actions.







3.2 Detail View — Template Editor







Layout: Two-pane (or three-pane) editor:







Left: Template metadata







Middle: Template body editor







Right: Placeholder helper + preview







Metadata Section







Fields:







Name / Display Name







Type (dropdown)







Partner (optional dropdown)







Language







Version (auto or manual)







Default toggle







Template Body Editor







Textarea or Monaco editor







Syntax: based on templateEngine (Handlebars/Mustache)







Supports placeholders like:







{{company.displayName}}







{{order.serviceId}}







{{customer.name}}







{{appointment.date}}







{{installer.name}}







Placeholder Helper Sidebar







List of allowed placeholders for the selected documentType:







Grouped by section:







Company







Customer







Order







Appointment







Installer







Billing







For each:







Key: {{customer.name}}







Description: “Customer’s full name”







Example: “TAN AHMAD”







Data source: DocumentPlaceholderDefinition.







API Example:



GET /api/document-templates/{id}



→ { template: DocumentTemplateView, placeholders: DocumentPlaceholderDefinition\\\[] }







GET /api/document-placeholders?documentType=INVOICE\\\_PDF



→ DocumentPlaceholderDefinition\\\[]







Live Preview Panel







Uses DocumentTemplate.previewSampleJson as fake data.







Calls a preview endpoint:







POST /api/document-templates/{id}/preview



Body: { sampleJsonOverride?: object }



→ { html: string } for HTML/Email



→ { pdfUrl: string } for PDF preview (optional)











OR locally renders via frontend template engine if agreed.







4\\. Template Types \\\& Behaviour



4.1 Work Order PDF







Used for internal work order PDFs for installers/clients.







Type: WORK\\\_ORDER\\\_PDF







It will be rendered when:







New job is created







User requests “Generate Work Order PDF”







4.2 Installer Docket PDF







Used as the manual docket attached to jobs.







Type: INSTALLER\\\_DOCKET\\\_PDF







CephasOps should always use the latest default template for the company.







4.3 Invoice PDF







Used for billing to partners/clients.







Type: INVOICE\\\_PDF







Fields normally include:







{{company.registrationNo}}







{{company.taxNo}}







{{invoice.number}}







{{invoice.date}}







{{invoice.lineItems}}







4.4 Email HTML Templates







Used for:







Installer notifications







Customer appointment confirmations







Internal ops updates







Type: EMAIL\\\_HTML







4.5 WhatsApp Text Templates







Simple text setups:







Type: WHATSAPP\\\_TEXT







Rendered server-side and passed to WhatsApp gateway.







5\\. Permissions \\\& Versioning







Only ROLE\\\_SUPER\\\_ADMIN / ROLE\\\_COMPANY\\\_ADMIN may:







Create / edit rate profiles







Create / edit document templates







Other roles:







Read-only access







Optional: keep version history:







version field in templates







UI can display “Last updated by X on Y”







6\\. Storybook Guidelines







Create stories for:







6.1 Rate Profiles







<RateProfilesList />







Empty state







Multiple profiles







Filter by active/expired







<RateProfileEditor />







With sample TIME rates







Editing rows in table







Validation errors (e.g. negative rate)







6.2 Document Templates







<TemplatesList />







Mixed types: Invoice, Work Order, Email







Filter by type and partner







<TemplateEditor />







Invoice template with placeholders







Email template with HTML preview







Error state (invalid placeholder)







Storybook mock data must match:







DocumentTemplateView







DocumentPlaceholderDefinition







RateProfileView + RateProfileItem







7\\. Non-Goals







UI does not determine which template to use per event; that is backend logic.







UI does not execute billing or payout calculations.







UI does not own any RM values; they come from backend rate profiles.







The roles for the UI:







Display, edit, and preview company-scoped configuration for rates and templates — nothing else.





