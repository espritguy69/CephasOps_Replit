\# DOCUMENT\_TEMPLATES\_MODULE.md

CephasOps Document Templates \& PDF Generation – Full Backend Specification



---



\## 1. Purpose



The Document Templates module generates \*\*consistent, branded documents\*\* from system data:



\- Invoices (for partners and internal records)

\- Job dockets / work orders

\- RMA forms

\- Payment receipts

\- P\&L exports and summaries

\- Letters / notices



It provides:



\- Versioned \*\*HTML templates\*\* per document type + partner

\- A \*\*rendering pipeline\*\* (merge data → HTML → PDF)

\- A \*\*GeneratedDocument\*\* store that other modules can reference



---



\## 2. Scope



\### 2.1 In Scope



\- Template management (CRUD)

\- Template versioning and activation

\- Document generation from:

&nbsp; - Orders + Billing (invoices, dockets)

&nbsp; - Inventory \& RMA (RMA forms)

&nbsp; - P\&L (financial summaries)

\- PDF and HTML export

\- Integration with file storage (via File entity)



\### 2.2 Out of Scope (v1)



\- Email sending (handled by Email/Notification layer)

\- Advanced WYSIWYG editor (front-end concern)

\- Multi-language templating (v2 enhancement)



---



\## 3. Data Model



\### 3.1 DocumentTemplate



Represents a reusable, versioned template.



Fields:



\- `Id`

\- `CompanyId` (nullable for global templates)

\- `Name` (e.g. "TIME Invoice", "Default Docket")

\- `DocumentType` (enum: Invoice, JobDocket, RmaForm, PaymentReceipt, PnlSummary, GenericLetter)

\- `PartnerId` (nullable – if template is partner-specific)

\- `IsActive` (only one active per CompanyId + DocumentType + PartnerId)

\- `Engine` (enum: Razor, Handlebars, Liquid – config only, implementation can start with one)

\- `HtmlBody` (string – HTML with placeholders)

\- `JsonSchema` (optional JSON describing expected data shape)

\- `Version` (int, incremented)

\- `CreatedAt`, `CreatedByUserId`

\- `UpdatedAt`, `UpdatedByUserId`



Constraints:



\- Unique active template per `(CompanyId, DocumentType, PartnerId)`.



---



\### 3.2 GeneratedDocument



Represents a single rendered document instance.



Fields:



\- `Id`

\- `CompanyId`

\- `DocumentType`

\- `ReferenceEntity` (enum: Order, Invoice, RmaTicket, PayrollRun, PnlFact, Generic)

\- `ReferenceId` (ID of the referenced entity)

\- `TemplateId` (FK → DocumentTemplate)

\- `FileId` (FK → File entity in storage module)

\- `Format` (enum: Pdf, Html)

\- `GeneratedAt`

\- `GeneratedByUserId` (or System)

\- `MetadataJson` (additional info: partnerName, invoiceNo, etc.)



Indexes:



\- `(CompanyId, ReferenceEntity, ReferenceId)`

\- `(CompanyId, DocumentType, GeneratedAt)`



---



\### 3.3 File (if not already defined)



If you already have a `File` entity elsewhere, reuse it. Otherwise, minimal spec:



\- `Id`

\- `CompanyId`

\- `FileName`

\- `MimeType`

\- `SizeBytes`

\- `StoragePath` or `StorageKey`

\- `CreatedAt`

\- `CreatedByUserId`



---



\## 4. Template Placeholders \& Data Contracts



Templates will receive a \*\*typed view model\*\* depending on DocumentType.



Examples:



\- Invoice:

&nbsp; - `invoice.number`

&nbsp; - `invoice.date`

&nbsp; - `invoice.partner`

&nbsp; - `invoice.lines\[]`

&nbsp; - `invoice.total`

\- Job Docket:

&nbsp; - `order.serviceId`

&nbsp; - `order.customerName`

&nbsp; - `order.address`

&nbsp; - `order.materials\[]`

&nbsp; - `order.photos\[]`



The mapping from domain objects → template view model is handled in services (see below).



---



\## 5. Services



\### 5.1 Template Management Service



`IDocumentTemplateService`



Responsibilities:



\- Create / update / deactivate templates

\- Enforce single active template per key

\- Fetch template for:

&nbsp; - `(CompanyId, DocumentType, PartnerId)`

&nbsp; - Fallback to `(CompanyId, DocumentType, null)` if partner-specific not found



Key methods:



\- `Task<DocumentTemplate> CreateTemplateAsync(...)`

\- `Task<DocumentTemplate> UpdateTemplateAsync(...)`

\- `Task SetActiveTemplateAsync(templateId)`

\- `Task<DocumentTemplate?> GetEffectiveTemplateAsync(companyId, documentType, partnerId)`



---



\### 5.2 Document Generation Service



`IDocumentGenerationService`



Responsibilities:



\- Generate document from domain data and a template

\- Html rendering + PDF conversion

\- Persist as GeneratedDocument + File



Key methods:



\- `Task<GeneratedDocument> GenerateInvoiceDocumentAsync(invoiceId)`

\- `Task<GeneratedDocument> GenerateJobDocketAsync(orderId)`

\- `Task<GeneratedDocument> GenerateRmaFormAsync(rmaTicketId)`

\- `Task<GeneratedDocument> GeneratePnlSummaryAsync(period, companyId, ...)`



Implementation flow:



1\. Resolve template (`GetEffectiveTemplateAsync`).

2\. Load domain data (Invoice, Order, etc.).

3\. Map domain data → strongly typed view model / JSON.

4\. Render template engine → HTML string.

5\. Convert HTML → PDF (using chosen library).

6\. Store PDF through File service.

7\. Create GeneratedDocument record.



---



\## 6. API Contracts



Base path: `/api/document-templates` and `/api/documents`



\### 6.1 Template CRUD



\- `GET /api/document-templates`

&nbsp; - Filters: `companyId`, `documentType`, `partnerId`, `isActive`

\- `GET /api/document-templates/{id}`

\- `POST /api/document-templates`

\- `PUT /api/document-templates/{id}`

\- `POST /api/document-templates/{id}/activate`

\- `POST /api/document-templates/{id}/duplicate` — duplicate a template (returns new template, name suffixed with " (Copy)")



\### 6.2 Generate Documents



\- `POST /api/documents/invoices/{invoiceId}`

\- `POST /api/documents/orders/{orderId}/docket`

\- `POST /api/documents/rma/{rmaTicketId}`

\- `POST /api/documents/pnl`

&nbsp; - Body: `companyId`, `periodFrom`, `periodTo`, grouping options



\### 6.3 Retrieve Documents



\- `GET /api/documents/{id}`

\- `GET /api/documents/by-reference`

&nbsp; - Query: `referenceEntity`, `referenceId`, optional `documentType`



---



\## 7. Integrations



\- \*\*Billing\*\* → Invoice PDF download, print, and preview via Document Templates. `BillingController` uses `IDocumentGenerationService`; endpoints: `GET /api/billing/invoices/{id}/pdf`, `GET /api/billing/invoices/{id}/preview-html`. Default invoice template seeded on startup. Optional `?templateId=` on both.

\- \*\*Orders\*\* → job dockets

\- \*\*Inventory/RMA\*\* → RMA forms

\- \*\*P\&L\*\* → summary exports

\- \*\*Background Jobs\*\* → automatic nightly generation if needed



---



\## 8. Error Handling



\- Missing template → HTTP 409 with clear message (“No active template for TIME Invoice”)

\- Render failure → log details, return 500 with correlationId

\- PDF conversion failure → same as above



---



\## 9. Security / RBAC



\- Only roles with `ManageTemplates` can CRUD templates.

\- Document generation allowed for:

&nbsp; - Finance (invoices, P\&L)

&nbsp; - Ops (dockets, RMA)

\- Documents always scoped by `companyId`.

---



\## 10. Settings UI (List View)



The Document Templates list at **Settings → Document Templates** (`/settings/document-templates`) provides a receipt-management-style UI:



\- **View modes:** **Cards** (default) or **Table** — toggle in the header.

\- **Type filter:** Chips for document type (All, Invoice, Job Docket, RMA Form, Purchase Order, Quotation, BOQ, Delivery Order, Receipt); each type has an icon and colour.

\- **Card view:** Each template is a card showing type icon, name, document type badge, engine, partner (if any), Active/Inactive status, last updated, and actions (Activate/Deactivate, Duplicate, Edit, Delete). Clicking the card opens the editor.

\- **Table view:** Same data in a table with sortable columns and row click to edit.

\- **Search:** Text search over template name and document type.

\- **Actions:** New Template, Duplicate (from list or editor), Activate/Deactivate, Edit, Delete. Duplicate uses `POST /api/document-templates/{id}/duplicate` and navigates to the new template’s editor.



See also: `07_frontend/ui/RATES_AND_TEMPLATES.md` (§ Document Templates UI) and `02_modules/document_generation/OVERVIEW.md` (§ Step 1).





