\# 👩‍💻 CephasOps Developer Guide



This guide helps engineers onboard quickly and work correctly within the CephasOps architecture.



---



\# 📚 1. Required Reading



Before writing any code, read these:



1\. `EXEC\_SUMMARY.md`

2\. `ARCHITECTURE\_BOOK.md`

3\. `05\_data\_model/README.md`

4\. `05\_data\_model/DATA\_MODEL\_SUMMARY.md`

5\. Your module spec inside `02\_modules/<module>.md`



These define the domain.



---



\# 🧩 2. Architecture Overview



CephasOps follows:



\- \*\*Clean Architecture\*\*

\- \*\*Domain-Driven Design (DDD)\*\*

\- \*\*Modular feature-based folders\*\*

\- \*\*PostgreSQL + EF Core\*\*

\- \*\*Background Jobs (Hangfire / Quartz)\*\*

\- \*\*REST API\*\*

\- \*\*React/Next.js or Blazor frontend\*\*

\- \*\*Mobile SI App (React Native / Blazor Hybrid)\*\*



---



\# 🗂️ 3. Project Structure



/backend

/frontend

/docs

/tests

/infra



markdown

Copy code



---



\# 🧱 4. Modules



Each module follows the same:



\- Entities

\- Repositories

\- Services

\- Domain events

\- Controllers

\- DTOs

\- Validators

\- Migrations



Example modules:



\- Orders

\- Scheduler

\- SI App

\- Inventory \& RMA

\- Billing

\- Payroll

\- P\&L

\- Parser

\- Settings

\- RBAC



---



\# 🔌 5. Data Model Rules (Critical)



Every entity:



\- MUST have `CompanyId`

\- MUST have `CreatedAt`

\- SHOULD have `UpdatedAt`

\- SHOULD have `MetadataJson` for future-proofing



Every relationship linking two domains must appear in:



05\_data\_model/relationships/<module>\_relationships.md



yaml

Copy code



Cursor depends on this.



---



\# 🏗️ 6. Development Flow



\### Step 1 – Read the spec  

Identify your module in:



02\_modules/



shell

Copy code



\### Step 2 – Check entities  

Find related entities in:



05\_data\_model/entities/



powershell

Copy code



\### Step 3 – Implement  

Using the standard structure:



Domain/

Application/

Infrastructure/

Api/



makefile

Copy code



\### Step 4 – Create migrations  

Run:



dotnet ef migrations add <Name>

dotnet ef database update



shell

Copy code



\### Step 5 – Write tests  

Tests go under:



/tests/<module>/



yaml

Copy code



\### Step 6 – Submit PR  

Every PR must include:



\- What changed  

\- Updated docs  

\- Migration script  

\- Tests  



---



\# 🚦 7. Code Quality Rules



\- No logic inside controllers  

\- Services must validate business rules  

\- Repositories MUST respect company scoping  

\- Never bypass domain rules  

\- No “magic strings” – use enums  

\- No raw SQL unless approved  

\- No duplicate code  

\- No cross-module references without explicit documentation



---



\# 📡 8. Background Jobs



Background job specs live in:



08\_infrastructure/background\_jobs\_infrastructure.md



yaml

Copy code



---



\# 📬 9. Contact



For deep architecture changes:



\*\*Contact the CephasOps Lead Architect.\*\*

