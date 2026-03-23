# 📘 CephasOps Data Model – Master Reference

This folder contains the **canonical, authoritative data model** for the entire CephasOps platform.

It defines:

- All entities used across every module  
- All relationships and foreign keys  
- Cross-module mappings  
- Business rules tied to the domain model  
- Normalisation standards  
- Guidance for Cursor AI when generating backend code

If you modify anything in this folder, it **must be reflected in the database schema** and migrations.

---

# 📂 Folder Structure

05_data_model/
README.md ← You are here
DATA_MODEL_SUMMARY.md ← High-level overview + foundation rules
DATA_MODEL_INDEX.md ← Navigation index for all files

entities/ ← Detailed entity definitions
orders_entities.md
inventory_entities.md
billing_entities.md
scheduler_entities.md
payroll_entities.md
pnl_entities.md
settings_entities.md
users_rbac_entities.md
parser_entities.md
document_templates_entities.md
global_settings_entities.md
kpi_profile_entities.md
logging_entities.md
background_jobs_entities.md
splitters_entities.md
material_templates_entities.md

relationships/ ← All FK and module relationships
orders_relationships.md
inventory_relationships.md
billing_relationships.md
scheduler_relationships.md
payroll_relationships.md
pnl_relationships.md
settings_rbac_relationships.md
parser_relationships.md
document_templates_relationships.md
global_settings_relationships.md
kpi_profile_relationships.md
logging_relationships.md
background_jobs_relationships.md
splitters_relationships.md
material_templates_relationships.md
cross_module_relationships.md

yaml
Copy code

---

# 🧱 Purpose of the Data Model Layer

This folder represents the **entire conceptual, logical, and physical model** for CephasOps.  
It is used by:

- Backend developers  
- Cursor AI (to generate code)  
- Frontend developers  
- QA & testers  
- Database administrators  
- Integrations (BI, PowerApps, Make.com, etc.)  

The goal is to have **one single source of truth** for:

- Entity fields  
- Data types  
- Foreign keys  
- Domain relationships  
- Validation boundaries  
- Module integrations  

---

# 🧩 How the Files Work Together

## **1. DATA_MODEL_SUMMARY.md**
This is the **top-level blueprint**.

It contains:

- Architecture-level concepts  
- Multi-company design rules  
- ERD overview  
- Normalisation principles  
- All modules and entity groups  
- Why the model is structured this way  

Cursor must always read this FIRST.

---

## **2. DATA_MODEL_INDEX.md**
This file provides:

- A list of all entity files  
- A list of all relationship files  
- Navigation shortcuts for Cursor AI  
- A map of where every model lives  

This makes Cursor smarter and prevents missed references.

---

## **3. entities/\*.md**
Each file defines:

- Full entity fields  
- Data types  
- Enums  
- Special rules  
- Derived fields  
- Event triggers  

Modules included:

- Orders domain  
- Scheduler  
- Service Installer App  
- Inventory & RMA  
- Billing & e-Invoice  
- Payroll  
- P&L  
- Settings  
- Users & RBAC  
- Email Parser  
- Document Templates  
- Global Settings  
- KPI Profiles  
- Background Jobs  
- Splitters  
- Material Templates  

These **drive actual DB table generation**.

---

## **4. relationships/\*.md**
Each file maps:

- One module  
- Its foreign keys  
- Dependencies on other modules  
- Required cascading behaviour  
- Many-to-one / one-to-many / many-to-many edges  

`cross_module_relationships.md` is critical because it describes:

- Orders ↔ Scheduler  
- Orders ↔ SI App  
- Orders ↔ Payroll  
- Orders ↔ Inventory  
- Orders ↔ Billing  
- Billing + Inventory + Payroll ↔ P&L  
- Parser ↔ Orders  
- Background Jobs ↔ All  

Cursor uses this file to understand:

- Entity binding  
- How to join across modules  
- How to enforce integrity  
- Where to place navigation properties  
- Whether operations cascade  

---

# ⚙️ How Cursor AI Should Use This Folder

Cursor should follow this order:

### **Step 1 → Load DATA_MODEL_SUMMARY.md**
To understand:

- Multi-company isolation  
- Global architecture concepts  
- Domain rules  

### **Step 2 → Load DATA_MODEL_INDEX.md**
To scan all entity and relationship files.

### **Step 3 → For each module generation:**
Load both:

- `entities/<module>_entities.md`
- `relationships/<module>_relationships.md`

### **Step 4 → For system-wide actions:**
Load:

- `cross_module_relationships.md`

This ensures Cursor always:

- Generates correct EF Core models  
- Builds correct migrations  
- Adds correct DbSets  
- Creates correct relationships  
- Creates services with full domain context  
- Avoids circular references  
- Avoids missing foreign keys  

---

# 📐 Standards & Conventions Embedded in the Data Model

Across all files, these standards are consistent:

### **Multi-company isolation**
Every entity that contains business data has:

CompanyId (GUID)

markdown
Copy code

This enforces:

- Data separation  
- Access control  
- Multi-company multi-partner tenancy  

### **MetadataJson**
Every entity supports extension through:

MetadataJson (jsonb)

yaml
Copy code

This enables:

- Future proofing  
- Dynamic attributes  
- Non-breaking enhancements  

### **CreatedAt / UpdatedAt / CreatedByUserId**
Every change is traceable.

### **Razor-ready / API-ready field structures**
All fields are structured to support:

- Frontend forms  
- API payloads  
- EF Core migrations  
- BI reporting  
- P&L & KPI engines  

---

# 🧪 How to Validate Changes

Any time a change is made to:

- An entity  
- A relationship  
- A data type  
- A module dependency  

You must:

1. Update the relevant `entities/*.md`  
2. Update the relationship file  
3. Update `DATA_MODEL_SUMMARY.md` if it's architectural  
4. Rerun migrations  
5. Regenerate DTOs and services through Cursor  
6. Regenerate frontend forms if necessary  

---

# 🟢 Status

✔ Fully complete  
✔ Production-grade  
✔ Cursor-ready  
✔ Consistent across all modules  
✔ Matches backend implementation (Orders, Scheduler, SI App, Inventory, Billing, Payroll, P&L)  
✔ Includes missing modules (KPI Profile, Material Templates, Splitters, Global Settings)  

---

# 🏁 Final Notes

This folder **must NEVER contain code**.  
Only documentation.  
It should always be readable by:

- Humans  
- Cursor  
- Future developers  
- QA teams  

And it will remain the **gold standard reference** for the entire CephasOps backend.

---