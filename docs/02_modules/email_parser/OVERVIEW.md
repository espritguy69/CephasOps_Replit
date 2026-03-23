1. Purpose of the Email Parser

The CephasOps Email Parser is the automation engine that converts incoming partner emails into structured, validated, and lifecycle-ready order data.

It eliminates manual order entry by:

Reading emails from configured mailboxes

Applying Parser Templates to understand the data

Extracting information from attachments or email body

Creating ParsedOrderDrafts

Sending them to review queue (if needed)

Converting them into real CephasOps Orders following the workflow lifecycle

Currently, this operates for the GPON Vertical (ISP Operations), but is architected to support CWO, NWO, and future divisions simply by adding new Parser Templates and new mailbox configurations in Settings.

2. Current Operating Scope (GPON Only)

At this stage, CephasOps Email Parser handles:

✅ **TIME GPON Activations** - **COMPLETED** (Full field + material extraction with IsRequired logic)

⏳ **TIME Modifications (Indoor / Outdoor)** - **DOCUMENTED** (Database-first strategy, Service ID detection rules defined)

✅ **TIME Assurance (TTKT / AWO)** - **COMPLETED** (AWO Number extraction implemented - see [AWO_NUMBER_EXTRACTION.md](./AWO_NUMBER_EXTRACTION.md)) - ✅ **COMPLETED** (AWO Number extraction implemented)

TIME–Digi HSBB

TIME–Celcom HSBB

All logic, data mapping, and lifecycle handling follow GPON Order Lifecycle, Workflow Engine, and Email Pipeline Architecture.

3. Future-Proof Design (CWO / NWO)

While only GPON is active now, the parser is designed to easily support:

CWO (Customer Work Order)

NWO (Network Work Order)

These divisions:

Use their own mailbox

Use their own Parser Templates

Can reuse the same ingestion, parsing, and mapping engine

Have independent lifecycle rules

No code changes are required — only configuration changes in Settings → Parser and Settings → Email Accounts.

4. Key Concept: Parser Templates (Central Configuration)

Every email format is defined as a **Parser Template**, configured under:

**Settings → Email → Parser Templates**

A Parser Template defines:

| Field | Purpose |
|-------|---------|
| Partner Group | TIME Group / Direct / etc. (for email routing) |
| Partner | TIME FTTH, Digi, Celcom, U-Mobile, etc. (template owner) |
| Department | GPON / CWO / NWO (determines workflow) |
| Order Type | Activation / Modification / Assurance / Reschedule |
| Detection Rules | Subject, Sender, Keywords |
| Attachment Rules | Excel/PDF formats, expected columns |
| Body Rules | TTKT extraction, approvals, reschedule detection |
| Field Mappings | How parsed fields map to CephasOps.Order |
| Post-Parse Actions | Reschedule update, draft creation, direct order creation |

This makes the system fully modular:

- Add new partner? → Add Parser Template
- Add new department (CWO/NWO)? → Add Parser Template
- Add new email box? → Configure under Settings
- No architecture changes required

**Email Routing Hierarchy**: 

```
Email Account → Email Rule (optional) → Department → Parser Template → Order → Department Workflow
```

For detailed pipeline architecture, see [../../01_system/EMAIL_PIPELINE.md](../../01_system/EMAIL_PIPELINE.md).

5. Supported Email Types (GPON)
5.1 Structured + Attachment-Based

TIME GPON Activations (Excel)

TIME Modifications (Excel)

Digi HSBB (Excel)

Celcom HSBB (Excel)

5.2 Semi-Structured Emails

TIME Assurance TTKT / AWO

Modifications with body notes

Free-form partner emails

5.3 Approval Emails

Reschedule Approvals

Slot Confirmation Emails

“Approved, please proceed” emails

6. Core Components (Domain Architecture Aligned)
Component	Namespace	Description
EmailAccount	Domain.Email	Mailbox configuration
ParserTemplate	Domain.Parser	Format rules for parsing
ParseSession	Domain.Parser	One processing batch
ParsedOrderDraft	Domain.Parser	Intermediate parsed result
EmailIngestionService	Application.Email	Fetches emails into queue
ParserService	Application.Parser	Main parser engine
ParserClassifier	Application.Parser	Determines which template to use
OrderResolver	Application.Order	Decides new vs update
ParserReviewQueue	Application.Parser	Human approval queue
7. Parser Flow (8-Step Process)

The complete parsing flow aligns with the Email Pipeline architecture:

```
Step 1: Email Ingestion
    ↓ Mailbox worker fetches email → stores in email_raw
    
Step 2: Classification
    ↓ Classifier identifies Partner, Intent, Confidence
    
Step 3: Parser Template Selection
    ↓ Based on Partner + Department → select template
    
Step 4: Attachment/Body Extraction
    ↓ Excel, PDF, HTML, or plain text processing
    
Step 5: Field Mapping
    ↓ ParserService maps data → ParsedOrderDraft
    
Step 6: Normalization
    ↓ Fix phone, address, datetime, partner ID
    
Step 7: Building Matching (Auto-Resolution)
    ↓ Match against existing buildings (80-90% success)
    
Step 8: Order Resolution
    ↓ OrderResolver: New Order OR Update Existing Order
    ↓ OrderLifecycle Engine applies status transitions
```

For complete architecture details, see [Email Pipeline Architecture](../../01_system/EMAIL_PIPELINE.md).

8. Multi-Partner, Multi-Template Support
Partner Group

TIME

TIMEDIGI

TIMECELCOM

TIMEUMOBILE (future)

DIRECT

Partner ID

Configured in Parser Template
→ e.g., DIGI0016775, CELCOM0016996

Vertical

GPON (Current)

CWO (Future)

NWO (Future)

This ensures strict isolation even under one company.

9. TTKT Definition (Final Version)

Each TTKT (Trouble Ticket) represents a specific customer issue logged under a Service ID.

A single Service ID may contain multiple TTKT numbers

Each TTKT is treated as a separate assurance case

Each can be claimed individually

Parser extracts and maps TTKT → Order

Mandatory fields extracted:

Service ID (TBBN…)
TTKT / TTID / AWO number
Customer Name
Appointment Time
Issue Description

10. Relationship to Order Lifecycle & Workflow Engine

The parser does NOT decide lifecycle.

It only provides structured data.

Then:

OrderLifecycle Engine applies the correct status (e.g., → Assigned, → ReschedulePendingApproval)

Workflow Engine enforces validation rules

Splitter Rules apply only later (not during parsing)

Invoice & docket flows are handled AFTER order is created

---

## Related Documentation

All parser documentation is aligned with the system architecture:

| Document | Role |
|----------|------|
| [Email Pipeline](../../01_system/EMAIL_PIPELINE.md) | How email travels end-to-end through the pipeline |
| Email Parser Overview | How parsing & mapping works (this document) |
| [Workflow Engine](../../01_system/WORKFLOW_ENGINE.md) | When status transitions are allowed |
| [Order Lifecycle](../../01_system/ORDER_LIFECYCLE.md) | The business rules controlling movement |
| [SPECIFICATION.md](./SPECIFICATION.md) | Detailed parsing rules by partner |
| [WORKFLOW.md](./WORKFLOW.md) | Step-by-step processing workflow |
| [BUILDING_MATCHING.md](./BUILDING_MATCHING.md) | Auto-resolution algorithm |

---

**END OF EMAIL PARSER OVERVIEW**