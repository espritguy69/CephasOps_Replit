# Document Generation Module

## Overview

The Document Generation Module is a **template-driven document generation engine** that allows users to create and manage document layouts without any coding. Users design templates using HTML and Handlebars syntax, and the system automatically populates them with data from the database to generate professional PDF documents.

---

## Key Features

| Feature | Description |
|---------|-------------|
| **No coding needed** | Users design templates in HTML/CSS |
| **Partner-specific templates** | Different layouts per partner (TIME vs Maxis) |
| **Version control** | Templates are versioned, can rollback |
| **Audit trail** | Every generated document is tracked |
| **Placeholder reference** | Users see available variables while editing |
| **Reusable** | Same template for multiple documents |
| **Flexible** | Any document type (Invoice, PO, BOQ, DO, Receipt) |

---

## Supported Document Types

| Type | Entity | Description |
|------|--------|-------------|
| **Invoice** | Invoice + LineItems | Customer invoices for billing |
| **JobDocket** | Order | Job sheet for service installers |
| **RmaForm** | RmaRequest + Items | Return merchandise authorization form |
| **PurchaseOrder** | PurchaseOrder + Items | PO to suppliers |
| **Quotation** | Quotation + Items | Sales quotes to customers |
| **BOQ** | Project + BoqItems | Bill of Quantities for projects |
| **DeliveryOrder** | DeliveryOrder + Items | Delivery notes for shipments |
| **PaymentReceipt** | Payment | Payment receipts |

---

## The Complete Process

### Step 1: User Creates a Template (Frontend)

Navigate to **Settings → Document Templates** (`/settings/document-templates`)

**List view (receipt-management style):**
- **Cards** (default) or **Table** — toggle in the header.
- **Type filter chips:** All, Invoice, Job Docket, RMA Form, Purchase Order, Quotation, BOQ, Delivery Order, Receipt (each with icon and colour).
- **Search** over template name and type.
- **Actions:** New Template, Duplicate (from list or editor), Activate/Deactivate, Edit, Delete. Duplicate calls `POST /api/document-templates/{id}/duplicate` and opens the new template.

**Editor:** From the list, the user:
1. Selects a **Document Type** (Invoice, PO, Quotation, BOQ, etc.)
2. Views **Available Placeholders** for that type (from `DocumentPlaceholderDefinitions`)
3. Writes **HTML + Handlebars** template in the editor
4. Saves the template

**Example Template (Invoice):**

```html
<div style="font-family: Arial; padding: 20px;">
  <div style="display: flex; justify-content: space-between;">
    <div>
      <h1 style="color: #333;">INVOICE</h1>
      <p><strong>Invoice #:</strong> {{invoice.number}}</p>
      <p><strong>Date:</strong> {{date invoice.date "dd MMM yyyy"}}</p>
      <p><strong>Due Date:</strong> {{date invoice.dueDate "dd MMM yyyy"}}</p>
    </div>
    <div style="text-align: right;">
      <h2>{{partner.name}}</h2>
      <p>{{partner.code}}</p>
    </div>
  </div>
  
  <table style="width: 100%; border-collapse: collapse; margin-top: 20px;">
    <thead>
      <tr style="background: #f5f5f5;">
        <th style="padding: 10px; border: 1px solid #ddd;">#</th>
        <th style="padding: 10px; border: 1px solid #ddd;">Description</th>
        <th style="padding: 10px; border: 1px solid #ddd;">Qty</th>
        <th style="padding: 10px; border: 1px solid #ddd;">Unit Price</th>
        <th style="padding: 10px; border: 1px solid #ddd;">Total</th>
      </tr>
    </thead>
    <tbody>
      {{#each lineItems}}
      <tr>
        <td style="padding: 10px; border: 1px solid #ddd;">{{rowIndex @index}}</td>
        <td style="padding: 10px; border: 1px solid #ddd;">{{description}}</td>
        <td style="padding: 10px; border: 1px solid #ddd; text-align: center;">{{quantity}}</td>
        <td style="padding: 10px; border: 1px solid #ddd; text-align: right;">{{currency unitPrice}}</td>
        <td style="padding: 10px; border: 1px solid #ddd; text-align: right;">{{currency total}}</td>
      </tr>
      {{/each}}
    </tbody>
  </table>
  
  <div style="margin-top: 20px; text-align: right;">
    <p><strong>Subtotal:</strong> {{currency invoice.subTotal}}</p>
    <p><strong>Tax:</strong> {{currency invoice.taxAmount}}</p>
    <p style="font-size: 1.2em;"><strong>Total: {{currency invoice.totalAmount}}</strong></p>
  </div>
  
  <div style="margin-top: 40px; font-size: 0.9em; color: #666;">
    <p>{{invoice.notes}}</p>
    <p><em>Generated: {{generatedAt}}</em></p>
  </div>
</div>
```

### Step 2: Template is Stored in Database

```
┌─────────────────────────────────────────────────────────────┐
│                    document_templates                        │
├─────────────────────────────────────────────────────────────┤
│ id           │ GUID                                          │
│ company_id   │ Company GUID                                  │
│ name         │ "TIME Invoice Template"                       │
│ document_type│ "Invoice"                                     │
│ partner_id   │ null (or specific partner for override)       │
│ is_active    │ true                                          │
│ engine       │ "Handlebars"                                  │
│ html_body    │ (the HTML template)                           │
│ version      │ 1                                             │
│ created_at   │ timestamp                                     │
│ updated_at   │ timestamp                                     │
└─────────────────────────────────────────────────────────────┘
```

### Step 3: User Triggers Document Generation

Generation can be triggered from:
- **Invoice page** → "Generate PDF", "Print Preview", or "Print" (uses Document Templates)
- **Order detail** → "Print Job Docket" button
- **RMA page** → "Export RMA Form"
- **API call** → `POST /api/documents/generate` or billing endpoints below

**Invoice-specific endpoints (BillingController):**
- `GET /api/billing/invoices/{id}/pdf?templateId=` — Download invoice PDF (optional template override)
- `GET /api/billing/invoices/{id}/preview-html?templateId=` — HTML for preview/print in frontend

**Generic API Request:**
```json
{
  "documentType": "Invoice",
  "referenceEntity": "Invoice",
  "referenceId": "abc123-invoice-id",
  "templateId": null
}
```

### Step 4: Backend Processing

The `DocumentGenerationService` performs these steps:

```
┌─────────────────────────────────────────────────────────────┐
│                DocumentGenerationService                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. RESOLVE TEMPLATE                                         │
│     Priority:                                                │
│     1. Specific templateId (if provided)                     │
│     2. Partner-specific template (if exists)                 │
│     3. Company default template                              │
│                                                              │
│  2. FETCH ENTITY DATA                                        │
│     Based on documentType, fetch from DB:                    │
│     - Invoice → Invoices + LineItems + Partner               │
│     - PO → PurchaseOrders + Items + Supplier                 │
│     - BOQ → Projects + BoqItems                              │
│     - etc.                                                   │
│                                                              │
│  3. BUILD DATA DICTIONARY                                    │
│     {                                                        │
│       "invoice": { number, date, total... },                 │
│       "partner": { name, code... },                          │
│       "lineItems": [ {...}, {...} ],                         │
│       "generatedAt": "06 Dec 2025 10:30"                     │
│     }                                                        │
│                                                              │
│  4. RENDER TEMPLATE (Handlebars.Net)                         │
│     Template + Data → Rendered HTML                          │
│                                                              │
│  5. CONVERT TO PDF (QuestPDF)                                │
│     Rendered HTML → PDF bytes                                │
│                                                              │
│  6. SAVE TO FILE STORAGE                                     │
│     PDF → FileService.UploadFileAsync()                      │
│     Returns: FileId for download                             │
│                                                              │
│  7. CREATE GENERATED_DOCUMENT RECORD                         │
│     Tracks: who generated, when, which template              │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Step 5: PDF Returned to User

**API Response:**
```json
{
  "id": "generated-doc-id",
  "documentType": "Invoice",
  "referenceEntity": "Invoice",
  "referenceId": "abc123-invoice-id",
  "templateId": "template-id-used",
  "fileId": "file-storage-id",
  "format": "Pdf",
  "generatedAt": "2025-12-06T10:30:00Z",
  "generatedByUserId": "user-id"
}
```

User can then:
- **Download** using `GET /api/files/{fileId}`
- **Preview** in browser
- **Email** to customer/partner

---

## Visual Flow Diagram

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   FRONTEND   │     │   BACKEND    │     │   DATABASE   │
└──────┬───────┘     └──────┬───────┘     └──────┬───────┘
       │                    │                    │
       │  1. Create Template│                    │
       │───────────────────>│                    │
       │                    │  Save template     │
       │                    │───────────────────>│
       │                    │                    │
       │  2. Generate PDF   │                    │
       │───────────────────>│                    │
       │                    │  Fetch template    │
       │                    │<───────────────────│
       │                    │                    │
       │                    │  Fetch entity data │
       │                    │<───────────────────│
       │                    │                    │
       │                    │  ┌─────────────┐   │
       │                    │  │  Handlebars │   │
       │                    │  │  Rendering  │   │
       │                    │  └─────────────┘   │
       │                    │                    │
       │                    │  ┌─────────────┐   │
       │                    │  │  QuestPDF   │   │
       │                    │  │  Convert    │   │
       │                    │  └─────────────┘   │
       │                    │                    │
       │                    │  Save file         │
       │                    │───────────────────>│
       │                    │                    │
       │  3. Return FileId  │                    │
       │<───────────────────│                    │
       │                    │                    │
       │  4. Download PDF   │                    │
       │───────────────────>│                    │
       │                    │  Fetch file        │
       │                    │<───────────────────│
       │  PDF File          │                    │
       │<───────────────────│                    │
       ▼                    ▼                    ▼
```

---

## Handlebars Helpers Reference

The following helpers are available in all templates:

| Helper | Syntax | Example Output | Description |
|--------|--------|----------------|-------------|
| **currency** | `{{currency amount}}` | `RM 1,060.00` | Format number as Malaysian Ringgit |
| **date** | `{{date value "format"}}` | `06 Dec 2025` | Format date with custom pattern |
| **time** | `{{time value}}` | `09:30` | Format time as HH:mm |
| **number** | `{{number value decimals}}` | `1,234.56` | Format number with decimals |
| **rowIndex** | `{{rowIndex @index}}` | `1, 2, 3...` | Convert 0-based index to 1-based |
| **uppercase** | `{{uppercase text}}` | `INVOICE` | Convert to uppercase |
| **lowercase** | `{{lowercase text}}` | `invoice` | Convert to lowercase |

### Loop Syntax

```handlebars
{{#each lineItems}}
  <tr>
    <td>{{rowIndex @index}}</td>
    <td>{{description}}</td>
    <td>{{currency total}}</td>
  </tr>
{{/each}}
```

### Conditional Syntax

```handlebars
{{#if invoice.notes}}
  <p>Notes: {{invoice.notes}}</p>
{{/if}}
```

---

## Placeholder Reference by Document Type

### Invoice

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{invoice.id}}` | Invoice unique identifier | GUID |
| `{{invoice.number}}` | Invoice number | INV-2025-0001 |
| `{{invoice.date}}` | Invoice date | 2025-12-06 |
| `{{invoice.dueDate}}` | Payment due date | 2026-01-06 |
| `{{invoice.status}}` | Invoice status | Pending |
| `{{invoice.subTotal}}` | Subtotal before tax | 1000.00 |
| `{{invoice.taxAmount}}` | Tax amount | 60.00 |
| `{{invoice.totalAmount}}` | Total amount | 1060.00 |
| `{{invoice.notes}}` | Invoice notes | - |
| `{{partner.name}}` | Partner name | TIME dotCom |
| `{{partner.code}}` | Partner code | TIME |
| `{{#each lineItems}}` | Line items loop | - |
| `{{lineItems.description}}` | Item description | GPON Installation |
| `{{lineItems.quantity}}` | Quantity | 1 |
| `{{lineItems.unitPrice}}` | Unit price | 250.00 |
| `{{lineItems.total}}` | Line total | 250.00 |

### Job Docket

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{order.serviceId}}` | Service ID / TBBN | TBBN12345678 |
| `{{order.ticketId}}` | Ticket ID | TKT-2025-001 |
| `{{order.status}}` | Order status | Assigned |
| `{{order.priority}}` | Priority | Normal |
| `{{order.appointmentDate}}` | Appointment date | 2025-12-06 |
| `{{order.appointmentWindowFrom}}` | Window start | 09:00 |
| `{{order.appointmentWindowTo}}` | Window end | 12:00 |
| `{{customer.name}}` | Customer name | Ahmad bin Ali |
| `{{customer.phone}}` | Customer phone | 0123456789 |
| `{{customer.email}}` | Customer email | ahmad@email.com |
| `{{location.buildingName}}` | Building name | Menara ABC |
| `{{location.unitNo}}` | Unit number | A-12-03 |
| `{{location.fullAddress}}` | Full address | Full formatted address |
| `{{installer.name}}` | Installer name | John Doe |
| `{{installer.phone}}` | Installer phone | 0198765432 |
| `{{orderType.name}}` | Order type | New Activation |

### Purchase Order

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{po.number}}` | PO number | PO-2025-0001 |
| `{{po.date}}` | PO date | 2025-12-06 |
| `{{po.expectedDeliveryDate}}` | Expected delivery | 2026-01-06 |
| `{{po.status}}` | PO status | Approved |
| `{{po.subTotal}}` | Subtotal | 5000.00 |
| `{{po.taxAmount}}` | Tax amount | 300.00 |
| `{{po.totalAmount}}` | Total amount | 5300.00 |
| `{{po.paymentTerms}}` | Payment terms | Net 30 |
| `{{supplier.name}}` | Supplier name | ABC Supplies |
| `{{supplier.contactPerson}}` | Contact person | Mr. Lee |
| `{{supplier.phone}}` | Supplier phone | 03-12345678 |
| `{{supplier.email}}` | Supplier email | sales@abc.com |
| `{{supplier.address}}` | Supplier address | - |
| `{{#each items}}` | PO items loop | - |
| `{{items.description}}` | Item description | ONT Router |
| `{{items.sku}}` | SKU | ZTE-F660 |
| `{{items.quantity}}` | Quantity | 100 |
| `{{items.unitPrice}}` | Unit price | 50.00 |
| `{{items.total}}` | Line total | 5000.00 |

### Quotation

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{quotation.number}}` | Quote number | QT-2025-0001 |
| `{{quotation.date}}` | Quote date | 2025-12-06 |
| `{{quotation.validUntil}}` | Valid until | 2026-01-06 |
| `{{quotation.subject}}` | Subject | Solar Installation |
| `{{quotation.totalAmount}}` | Total amount | 10100.00 |
| `{{customer.name}}` | Customer name | Ahmad bin Ali |
| `{{customer.phone}}` | Customer phone | 0123456789 |
| `{{customer.address}}` | Customer address | - |
| `{{#each items}}` | Quote items loop | - |
| `{{items.itemType}}` | Item type | Material |
| `{{items.description}}` | Description | Solar Panel 400W |
| `{{items.quantity}}` | Quantity | 10 |
| `{{items.unitPrice}}` | Unit price | 800.00 |
| `{{items.total}}` | Line total | 8000.00 |

### BOQ (Bill of Quantities)

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{project.code}}` | Project code | PRJ-2025-001 |
| `{{project.name}}` | Project name | Solar Installation |
| `{{project.customerName}}` | Customer name | ABC Corporation |
| `{{project.siteAddress}}` | Site address | - |
| `{{#each materials}}` | Materials loop | - |
| `{{materials.section}}` | Section | Solar Panels |
| `{{materials.description}}` | Description | 400W Panel |
| `{{materials.quantity}}` | Quantity | 20 |
| `{{materials.unitRate}}` | Unit rate | 800.00 |
| `{{materials.total}}` | Total | 16000.00 |
| `{{materials.sellingPrice}}` | Selling price | 18400.00 |
| `{{#each labor}}` | Labor loop | - |
| `{{labor.description}}` | Description | Installation |
| `{{labor.total}}` | Total | 2500.00 |
| `{{summary.materialTotal}}` | Materials total | 16000.00 |
| `{{summary.laborTotal}}` | Labor total | 2500.00 |
| `{{summary.grandTotal}}` | Grand total | 18500.00 |

### Delivery Order

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{deliveryOrder.number}}` | DO number | DO-2025-0001 |
| `{{deliveryOrder.date}}` | DO date | 2025-12-06 |
| `{{deliveryOrder.type}}` | DO type | Outbound |
| `{{deliveryOrder.status}}` | Status | InTransit |
| `{{deliveryOrder.deliveryPerson}}` | Driver name | John Driver |
| `{{deliveryOrder.vehicleNumber}}` | Vehicle | WXY 1234 |
| `{{recipient.name}}` | Recipient name | Ahmad bin Ali |
| `{{recipient.phone}}` | Recipient phone | 0123456789 |
| `{{recipient.address}}` | Delivery address | 123 Jalan ABC |
| `{{#each items}}` | DO items loop | - |
| `{{items.description}}` | Description | ONT Router |
| `{{items.quantity}}` | Quantity | 10 |
| `{{items.quantityDelivered}}` | Qty delivered | 10 |
| `{{items.serialNumbers}}` | Serial numbers | SN001, SN002 |

### Payment Receipt

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{payment.number}}` | Receipt number | RCP-2025-0001 |
| `{{payment.date}}` | Payment date | 2025-12-06 |
| `{{payment.type}}` | Payment type | Income |
| `{{payment.method}}` | Payment method | BankTransfer |
| `{{payment.amount}}` | Amount | 1060.00 |
| `{{payment.bankReference}}` | Bank reference | TRX123456 |
| `{{payment.description}}` | Description | Payment for INV-001 |
| `{{payer.name}}` | Payer name | TIME dotCom |

---

## API Endpoints

### Document Templates

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/document-templates` | List all templates |
| GET | `/api/document-templates/{id}` | Get template by ID |
| POST | `/api/document-templates` | Create new template |
| PUT | `/api/document-templates/{id}` | Update template |
| DELETE | `/api/document-templates/{id}` | Delete template |
| POST | `/api/document-templates/{id}/activate` | Set as active template |
| POST | `/api/document-templates/{id}/duplicate` | Duplicate template (returns new template) |

### Document Generation

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/documents/generate` | Generate document |
| GET | `/api/documents` | List generated documents |
| GET | `/api/documents/{id}` | Get generated document |
| POST | `/api/documents/invoice/{invoiceId}` | Generate invoice PDF |
| POST | `/api/documents/docket/{orderId}` | Generate job docket |
| POST | `/api/documents/rma/{rmaId}` | Generate RMA form |

### Billing (Invoice Templates)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/billing/invoices/{id}/pdf?templateId=` | Download invoice PDF (template-driven) |
| GET | `/api/billing/invoices/{id}/preview-html?templateId=` | HTML for preview/print (template-driven) |

### Placeholder Definitions

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/document-placeholders` | List all placeholders |
| GET | `/api/document-placeholders?documentType=Invoice` | Filter by type |

---

## Database Schema

### document_templates

```sql
CREATE TABLE document_templates (
    id UUID PRIMARY KEY,
    company_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    document_type VARCHAR(50) NOT NULL,
    partner_id UUID NULL,
    is_active BOOLEAN DEFAULT true,
    engine VARCHAR(50) DEFAULT 'Handlebars',
    html_body TEXT NOT NULL,
    json_schema TEXT NULL,
    version INT DEFAULT 1,
    created_by_user_id UUID NULL,
    updated_by_user_id UUID NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE UNIQUE INDEX ix_document_templates_active 
ON document_templates (company_id, document_type, partner_id) 
WHERE is_active = true;
```

### generated_documents

```sql
CREATE TABLE generated_documents (
    id UUID PRIMARY KEY,
    company_id UUID NOT NULL,
    document_type VARCHAR(50) NOT NULL,
    reference_entity VARCHAR(50) NOT NULL,
    reference_id UUID NOT NULL,
    template_id UUID NOT NULL,
    file_id UUID NOT NULL,
    format VARCHAR(20) DEFAULT 'Pdf',
    generated_at TIMESTAMP DEFAULT NOW(),
    generated_by_user_id UUID NULL,
    metadata_json TEXT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);
```

### document_placeholder_definitions

```sql
CREATE TABLE document_placeholder_definitions (
    id UUID PRIMARY KEY,
    document_type VARCHAR(50) NOT NULL,
    key VARCHAR(100) NOT NULL,
    description VARCHAR(500) NOT NULL,
    example_value VARCHAR(200) NULL,
    is_required BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT NOW()
);
```

---

## Template Resolution Priority

When generating a document, the system resolves the template in this order:

1. **Specific Template ID** - If `templateId` is provided in the request
2. **Partner-Specific Template** - If a template exists for `(companyId, documentType, partnerId)`
3. **Company Default Template** - If a template exists for `(companyId, documentType, null)`
4. **Error** - If no template found, throws `InvalidOperationException`

This allows:
- Different invoice layouts for different partners
- Override specific templates per request
- Fallback to company defaults

---

## Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Template Engine** | Handlebars.Net | HTML + placeholder rendering |
| **PDF Generation** | QuestPDF | Convert rendered HTML to PDF |
| **File Storage** | FileService | Store generated PDFs |
| **Database** | PostgreSQL | Store templates and records |

---

## Future Enhancements

### PuppeteerSharp Integration

The current QuestPDF implementation has limited HTML/CSS support. For full WYSIWYG rendering, **PuppeteerSharp** (headless Chrome) can be integrated:

| Feature | Current (QuestPDF) | Future (PuppeteerSharp) |
|---------|-------------------|------------------------|
| CSS3 Support | Limited | Full |
| Flexbox/Grid | No | Yes |
| Custom Fonts | Limited | Yes |
| Images | Basic | Full |
| Complex Layouts | No | Yes |
| Performance | Fast | Slower (browser startup) |
| Dependencies | None | Chrome binary |

### Additional Document Types

- **Contract** - Service agreements
- **Work Order** - Internal work orders
- **Packing List** - Warehouse packing lists
- **Certificate** - Completion certificates
- **Statement** - Account statements

---

## Best Practices

### Template Design

1. **Use inline styles** - External CSS may not render correctly
2. **Test with sample data** - Preview before saving
3. **Keep it simple** - Complex layouts may not render as expected
4. **Use tables for structure** - More reliable than flexbox/grid
5. **Include all required fields** - Check placeholder definitions

### Performance

1. **Limit line items** - Very long tables may cause memory issues
2. **Optimize images** - Use compressed images if embedding
3. **Cache templates** - Templates are cached after first load

### Security

1. **Sanitize HTML** - Templates are stored as-is, be careful with user input
2. **Validate placeholders** - Only documented placeholders work
3. **Audit trail** - All generated documents are logged

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Placeholder not replaced | Typo in placeholder name | Check placeholder reference |
| PDF is blank | Template has no content | Verify HTML body is saved |
| Missing data | Entity not found | Check referenceId is valid |
| Template not found | No active template | Create and activate a template |
| Formatting issues | Complex CSS | Simplify to inline styles |

---

## Related Documentation

- [Global Settings](../global_settings/GLOBAL_SETTINGS_MODULE.md)
- [Billing](../billing/OVERVIEW.md)
- [Orders](../orders/OVERVIEW.md)
- [Inventory & RMA](../inventory/OVERVIEW.md)

