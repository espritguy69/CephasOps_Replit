\# AI Documentation – CephasOps  

This folder contains all \*\*AI-specific helper documents\*\* that enable Cursor AI, ChatGPT, Claude, and other automated agents to correctly read, understand, and develop the CephasOps system end-to-end.



It ensures:

\- Correct interpretation of our architecture  

\- Consistent coding conventions  

\- Multi-company safety  

\- API accuracy  

\- Zero deviation from domain rules  



Every AI agent must read these files \*\*before writing any code\*\*.



---



\# 📁 Contents of `/docs/ai/`



\## 1. `CURSOR\_ONBOARDING.md`

\*\*The master AI onboarding file.\*\*



It teaches agents:

\- How to correctly navigate the repository  

\- Reading order (EXEC\_SUMMARY → ARCHITECTURE → STORYBOOK → API)  

\- What Clean Architecture rules to follow  

\- Mandatory multi-company constraints  

\- What AI must NOT do  

\- Commit-ready checklist  



This is the very first file Cursor should read.



---



\## 2. `PHASE1\_BACKLOG.md`

A full, structured engineering backlog for \*\*Phase 1 (ISP Vertical)\*\*.



Contains:

\- Backend tasks  

\- Entity creation  

\- Application services  

\- Controllers  

\- Frontend components  

\- Scheduler UI tasks  

\- SI PWA tasks  

\- QA \& deployment  

\- Documentation updates  



Cursor uses this to generate tasks and plan implementation.



---



\## 3. `API\_EXAMPLES.md`

Provides \*\*realistic request × response examples\*\* for:



\- Authentication  

\- Orders  

\- Scheduler (assign, reschedule)  

\- SI status updates (OnTheWay, MetCustomer, Completed)  

\- Materials / Inventory movements  

\- Billing and Submission ID flow  



This prevents API misunderstandings and ensures correct frontend hooks.



---



\## 4. `PARSER\_INPUT\_EXAMPLES.md`

Contains \*\*real-world TIME portal email examples\*\*, including:



\- Activation emails  

\- Assurance emails  

\- TTKT / AWO parsing  

\- MRA parsing  

\- Excel input → JSON output examples  



Used by Cursor to implement:

\- Email ingestion  

\- Excel mapping rules  

\- Parsing logic  

\- Data normalization  



Without this file, AI would guess incorrectly.



---



\## 5. `ENV\_CONFIG\_GUIDE.md`

Defines all \*\*environment variables and configuration rules\*\*:



\### Backend:

\- JWT  

\- Database  

\- Email parser  

\- Storage paths  

\- Multi-company headers  



\### Frontend:

\- Vite config  

\- API URL  

\- Company switch behavior  



\### Docker:

\- Local dev environment  

\- Parser worker container  



This ensures reproducible development for both humans and AI.



---



\## 6. Optional: `AI\_RULES\_FOR\_CURSOR.md`

(Primary copy lives under `/docs/governance/`)



Defines \*\*strict engineering rules AI must follow\*\*, including:



\- No business logic in controllers  

\- Enforce `companyId` in all layers  

\- No inline SQL  

\- No bypassing RBAC  

\- Doc updates required for each commit  



This file may be mirrored here for convenience.



---



\# 🔧 How AI should use this folder

1\. \*\*Read `CURSOR\_ONBOARDING.md` first.\*\*  

2\. Follow the onboarding reading order.  

3\. Use `PHASE1\_BACKLOG.md` to plan tasks.  

4\. Use `API\_EXAMPLES.md` while implementing endpoints.  

5\. Use `PARSER\_INPUT\_EXAMPLES.md` when building parser modules.  

6\. Use `ENV\_CONFIG\_GUIDE.md` when generating `.env` or Docker instructions.  

7\. Follow `AI\_RULES\_FOR\_CURSOR.md` at all times.



---



\# 🔒 Why this folder exists

\- Prevent AI from generating broken code  

\- Standardize agent behavior  

\- Protect multi-company data separation  

\- Ensure architectural consistency  

\- Enable full automated development with no human ambiguity  



---



\# 🧩 Related folders

/docs

/ai <-- you are here

/architecture <-- system design

/spec/api <-- API blueprint

/spec/database <-- DB schema

/storybook <-- user flows

/governance <-- code conduct \& review rules

/archive <-- legacy files



yaml

Copy code



---



\# ✔️ Maintainer Notes

Update these files whenever:

\- Architecture changes  

\- New endpoints are added  

\- Parser rules change  

\- Multi-company logic updates  

\- New phases (Phase 2, Phase 3, etc.) begin  



AI agents rely on this folder for \*\*every commit\*\*, so accuracy here is critical.

