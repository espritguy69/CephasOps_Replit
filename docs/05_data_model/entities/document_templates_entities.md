# DOCUMENT TEMPLATES ENTITIES

## Overview

Document Templates control how CephasOps generates all customer-facing and operational documents, including PDFs, emails, and dockets. Every template is associated with a specific company, enabling complete customization and white-label support across multiple organizations within a single CephasOps installation.

---

## 1. Core Entities

### 1.1 DocumentTemplate

The `DocumentTemplate` entity stores the complete definition of a document format.

**Fields:**

- **id** – Unique identifier for the template
- **companyId** – Foreign key linking to the Company that owns this template
- **code** – Internal reference code (e.g., `WORK_ORDER_STD`, `INVOICE_STD`, `TIME_DOCKET`)
- **displayName** – Human-readable name shown in the UI
- **type** – Category of document this template generates:
  - `WORK_ORDER_PDF`
  - `INSTALLER_DOCKET_PDF`
  - `INVOICE_PDF`
  - `EMAIL_HTML`
  - `WHATSAPP_TEXT`
- **partnerCode** – Optional field to scope templates by partner (e.g., `TIME`, `DIGI`)
- **language** – Language code for the template content (e.g., `en`, `ms`)
- **version** – Version number for tracking template revisions
- **isDefault** – Boolean flag indicating whether this template is the default for its combination of `companyId`, `type`, and `partnerCode`
- **templateEngine** – The rendering engine used to process this template (e.g., `HANDLEBARS`, `MUSTACHE`, `LIQUID`)
- **body** – Complete template string containing the layout and placeholder variables
- **previewSampleJson** – JSON data structure used for generating preview renders in the UI
- **createdAt** – Timestamp of template creation
- **updatedAt** – Timestamp of last modification

### 1.2 DocumentPlaceholderDefinition

The `DocumentPlaceholderDefinition` entity catalogs all available placeholder variables that can be used within templates of a specific document type.

**Fields:**

- **id** – Unique identifier
- **documentType** – The document type these placeholders apply to
- **key** – The placeholder variable syntax (e.g., `{{customer.name}}`, `{{order.serviceId}}`)
- **description** – Human-readable explanation of what this placeholder represents
- **exampleValue** – Sample value shown in the UI for reference
- **isRequired** – Boolean indicating whether this placeholder must be present in templates of this type

**Purpose:**

These definitions power the template editor UI by providing:
- Autocomplete suggestions when editing templates
- Validation to ensure required placeholders are present
- Tooltips explaining each placeholder's purpose
- Example values for testing template rendering

---

## 2. Multi-Company Behavior

CephasOps supports complete isolation and customization at the company level. Each company can maintain entirely separate document templates for all document types.

**Company-Specific Customization:**

- **Invoice Templates** – Each company can specify its own logo, business address, tax registration numbers, and payment terms
- **Docket Templates** – Companies can define custom headers, footers, and installer instructions
- **Email Templates** – Separate templates can be configured for:
  - Installer assignment notifications
  - Customer appointment confirmations
  - Internal operations alerts
  - Partner communications

**White-Label Support:**

The same CephasOps backend instance can serve multiple companies with completely different branding:
- Distinct logos and color schemes per company
- Custom headers and footers reflecting each company's identity
- Company-specific legal terms and conditions
- Multiple language support per company based on their market

This architecture enables CephasOps to function as a true multi-tenant platform where each company appears as an independent system to their customers and installers.

---

## 3. UI Behavior

The user interface must never contain hardcoded document text or layout. All document generation relies on templates retrieved from the database.

**API Integration:**

The UI interacts with document templates through these endpoints:

- **Retrieve Templates by Type:**  
  `GET /api/companies/{companyId}/document-templates?type=INVOICE_PDF`  
  Returns all templates of the specified type for a given company

- **Retrieve Specific Template:**  
  `GET /api/document-templates/{id}`  
  Fetches a single template for viewing or editing

- **Update Template:**  
  `PUT /api/document-templates/{id}`  
  Saves modifications to an existing template

**UI Responsibilities:**

The user interface should:
- Display company information and settings from the `Company` and `CompanySettings` entities
- Present available placeholder variables from `DocumentPlaceholderDefinition` for the template editor
- Provide a template editing interface with syntax highlighting and placeholder insertion tools
- Validate template structure before saving
- Generate preview renders using the `previewSampleJson` data

**Document Generation:**

All document generation operations, whether triggered by jobs, background workers, or API calls, must exclusively use templates stored in the `DocumentTemplate` entity. This ensures:
- Consistent branding across all generated documents
- Centralized control over document content and layout
- Easy updates without code deployments
- Full audit trail of template changes