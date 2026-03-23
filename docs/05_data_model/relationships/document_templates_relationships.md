Document Templates – Data Model Relationships



This file documents how the Document Templates module fits into the overall CephasOps data model.



It focuses on:



How templates are scoped by Company



How templates are used to generate documents for Orders, Invoices, RMA, and Job Sessions



How generated documents link to the Files table and other modules



1\. Core Entities (Recap)



From document\_templates\_entities.md (naming assumed):



DocumentTemplate



GeneratedDocument



(Optional) DocumentTemplateVariable / DocumentTemplateContextHint – if present in the entities file



Enums:



DocumentType (Invoice, Docket, JobCompletionReport, RmaReport, …)



DocumentSourceType (Order, Invoice, RmaTicket, SiJobSession, Manual)



All entities are company-scoped.



2\. Company Relationships

2.1 Company → DocumentTemplate



Relationship: Company (1) → (n) DocumentTemplate



Keys:



DocumentTemplate.CompanyId → Companies.Id (FK)



Notes:



A template is always owned by a single company.



No cross-company sharing of templates.



Template codes are unique per company.



2.2 Company → GeneratedDocument



Relationship: Company (1) → (n) GeneratedDocument



Keys:



GeneratedDocument.CompanyId → Companies.Id (FK)



Notes:



All generated documents are scoped by company.



This enforces data isolation at reporting and file access level.



3\. Template Relationships

3.1 DocumentTemplate → GeneratedDocument



Relationship: DocumentTemplate (1) → (n) GeneratedDocument



Keys:



GeneratedDocument.TemplateId → DocumentTemplate.Id (FK)



Notes:



Every generated document records which template produced it.



When a template is updated, new documents reference the new template version; old documents still reference the previous template record (or version, if versioning is implemented in entities).



3.2 DocumentTemplate → DocumentType



Relationship: DocumentTemplate (n) → (1) DocumentType (enum)



Notes:



Each template has a single DocumentType, e.g.:



Invoice



ManualInvoice



Docket



JobCompletionReport



RmaReport



A template cannot be reused for a different document type; new template records must be created instead.



3.3 DocumentTemplate → DocumentSourceType



Relationship: DocumentTemplate (n) → (1) DocumentSourceType (enum)



Notes:



Defines which source entity this template expects, e.g.:



Order (for dockets, job completion reports)



Invoice



RmaTicket



SiJobSession



The Workflow / Document generation service validates that:



DocumentTemplate.SourceType matches the actual source (e.g. you cannot use an Invoice template for an Order).



4\. GeneratedDocument Source Relationships



The GeneratedDocument links back to the originating business entity using a polymorphic pattern:



GeneratedDocument.SourceType  (enum)

GeneratedDocument.SourceId    (Guid)





The combination of (SourceType, SourceId, CompanyId) identifies the source.



4.1 GeneratedDocument → Orders



When: GeneratedDocument.SourceType = Order



Relationship (conceptual): Order (1) → (n) GeneratedDocument



Logical FK:



GeneratedDocument.SourceId → Orders.Id



GeneratedDocument.CompanyId → Orders.CompanyId (must match)



Used for:



Order-level documents:



Dockets



Job completion reports



Internal job summary PDFs, etc.



Notes:



The Document generation service must enforce that the company and source type match before saving.



4.2 GeneratedDocument → Invoices



When: GeneratedDocument.SourceType = Invoice



Relationship: Invoice (1) → (n) GeneratedDocument



Logical FK:



GeneratedDocument.SourceId → Invoices.Id



GeneratedDocument.CompanyId → Invoices.CompanyId



Used for:



Invoice PDFs (standard and manual invoices).



Notes:



This enables multiple document versions per invoice, e.g. regenerated or revised PDFs.



4.3 GeneratedDocument → RmaTickets



When: GeneratedDocument.SourceType = RmaTicket



Relationship: RmaTicket (1) → (n) GeneratedDocument



Logical FK:



GeneratedDocument.SourceId → RmaTickets.Id



GeneratedDocument.CompanyId → RmaTickets.CompanyId



Used for:



RMA forms or vendor submission documents.



4.4 GeneratedDocument → SiJobSessions (SI App)



When: GeneratedDocument.SourceType = SiJobSession



Relationship: SiJobSession (1) → (n) GeneratedDocument



Logical FK:



GeneratedDocument.SourceId → SiJobSessions.Id



GeneratedDocument.CompanyId → SiJobSessions.CompanyId



Used for:



On-site job reports



Customer sign-off documents, if configured.



4.5 GeneratedDocument → Manual / Misc (Optional)



When: GeneratedDocument.SourceType = Manual



Notes:



Some documents may not be tied to a specific entity.



In this case, SourceId may be nullable or point to a synthetic ID, depending on the final entities spec.



5\. GeneratedDocument → Files / Storage

5.1 GeneratedDocument → Files



Relationship: GeneratedDocument (n) → (1) File



Keys:



GeneratedDocument.FileId → Files.Id (FK)



Notes:



Files is the central table for stored binary content (PDF/HTML, etc.).



GeneratedDocument stores:



FileId – link to the stored artifact



StorageKind or similar enum (if defined)



If the Files module is not yet implemented, this relationship must align with whatever is defined in its entities doc.



5.2 GeneratedDocument → Storage Metadata



If the data model includes additional storage metadata (e.g. StoragePath, Bucket, ContentType), those are attributes of File or GeneratedDocument as per entities spec, not separate relationships.



6\. Relationships with Billing, Orders \& RMA Modules

6.1 Orders ↔ Document Templates



Indirect via:



Generated documents for:



Dockets



Job completion reports



Workflow engine may enforce that an Order has required documents generated before moving to certain statuses (e.g. “ReadyForBilling”).



6.2 Billing (Invoices) ↔ Document Templates



Indirect via:



GeneratedDocument records for invoice PDFs.



Billing services:



Trigger document generation after invoice creation or approval.



Store the resulting GeneratedDocument.Id and/or FileId for later retrieval.



6.3 Inventory \& RMA ↔ Document Templates



Indirect via:



RMA ticket documents.



The RMA service may call the Document templates service to produce documents to send to vendors or attach to tickets.



7\. Multi-Company \& RBAC Rules (Relationship Impact)



All relationships are constrained by CompanyId:



A GeneratedDocument must always have the same CompanyId as its source entity (Order, Invoice, RmaTicket, SiJobSession).



A DocumentTemplate may only be used for entities belonging to the same company.



RBAC:



Access to templates and generated documents is controlled via roles:



e.g. Finance can see invoice documents; Ops can see dockets.



This is enforced at the API/service layer but must be considered when querying data across relationships.



8\. Deletion / Archival Behaviour (Conceptual)

8.1 Templates



Templates are almost always soft-disabled (IsActive = false) instead of hard-deleted, to preserve referential integrity with existing GeneratedDocument rows.



8.2 Generated Documents



Generated documents are never silently deleted while source records (Orders, Invoices, RmaTickets, SiJobSessions) exist.



If physical file deletion is required (e.g. retention policy), GeneratedDocument may keep metadata and point to a “deleted/archived” file status, depending on the final Files design.

