\# Documents \& Files Entities  

CephasOps – Document \& File Data Model  

Version 1.0



Defines:



\- File

\- DocumentTemplate

\- GeneratedDocument



---



\## 1. File



Binary file stored in object storage.



\### 1.1 Table: `Files`



| Field        | Type     | Required | Description                          |

|--------------|----------|----------|--------------------------------------|

| id           | uuid     | yes      | Primary key.                         |

| companyId    | uuid     | yes      | FK → Companies.id.                   |

| fileName     | string   | yes      | Original file name.                  |

| storagePath  | string   | yes      | Path in storage provider.            |

| contentType  | string   | yes      | MIME type.                           |

| sizeBytes    | bigint   | yes      | File size.                           |

| checksum     | string   | no       | Optional hash for integrity.         |

| createdById  | uuid     | yes      | FK → Users.id or SI id (polymorphic).|

| createdAt    | datetime | yes      | Uploaded timestamp.                  |



---



\## 2. DocumentTemplate



HTML/handlebars templates used to generate PDFs.



\### 2.1 Table: `DocumentTemplates`



| Field        | Type     | Required | Description                                |

|--------------|----------|----------|--------------------------------------------|

| id           | uuid     | yes      | Primary key.                               |

| companyId    | uuid     | yes      | FK → Companies.id.                         |

| name         | string   | yes      | E.g. `TaxInvoice`, `JobDocket`.           |

| documentType | string   | yes      | `Invoice`, `Docket`, `RMA`, etc.          |

| version      | int      | yes      | Template version.                          |

| isActive     | boolean  | yes      | Active template flag.                      |

| engine       | string   | yes      | `HTML`, `Handlebars`, `Liquid`, etc.      |

| contentHtml  | text     | yes      | Actual template body.                      |

| createdAt    | datetime | yes      | Created timestamp.                         |



---



\## 3. GeneratedDocument



Represents a system-generated business document (invoice PDF, docket PDF, etc.).



\### 3.1 Table: `GeneratedDocuments`



| Field           | Type     | Required | Description                               |

|-----------------|----------|----------|-------------------------------------------|

| id              | uuid     | yes      | Primary key.                              |

| companyId       | uuid     | yes      | FK → Companies.id.                        |

| documentType    | string   | yes      | `Invoice`, `CreditNote`, `Docket`, etc.   |

| entityId        | uuid     | yes      | ID of main entity (Invoice, Order, etc.). |

| templateId      | uuid     | no       | FK → DocumentTemplates.id used.           |

| fileId          | uuid     | yes      | FK → Files.id of generated PDF.           |

| isFinal         | boolean  | yes      | Final/locked document flag.               |

| createdAt       | datetime | yes      | Generated timestamp.                      |



---



\# End of Documents \& Files Entities



