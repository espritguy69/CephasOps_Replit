This directory contains the UI design system, wireframes, and component specifications for the CephasOps Frontend (Admin Web App & SI Mobile App).
It defines how every screen should look and which UI components must be used.

These documents serve as the source of truth for:

Frontend developers

Cursor agents (for auto-generation)

UI/UX designers

QA testers validating layout consistency

Multi-company theming rules

The Storybook behaviour, page flows, and application routing are kept separately in:

docs/07_frontend/storybook/


This folder focuses only on the visual and component layer.

🧩 1. Files in This Directory
### COMPONENT_LIBRARY.md

Defines the entire CephasOps UI component system, including:

Layout components (AppShell, Sidebar, TopBar, PageHeader)

Interactive components (StatusPill, Tabs, Filters, DatePicker)

Data-driven components (DataTable, MetricCard, Timeline)

Domain components (OrderCard, JobCard, InvoicePanel, MaterialList)

Reusable UI atoms for Admin App & SI PWA

This file ensures design consistency across all companies (Cephas ISP, Kingsman, Menorah).

WIREFRAMES.md

Contains the wireframe layouts for all major screens in both applications:

Admin App

Login

Dashboard

Orders List / Order Detail

Scheduler

Inventory

Invoices

P&L

Settings

SI Mobile App (PWA)

Login

Jobs List / Job Detail

Materials

Profile

Each wireframe describes:

Screen structure (header, sidebar, content blocks)

Field layout & grouping

Component usage

UI state behaviours

Cross-company visual differences (if needed)

These wireframes act as the blueprint for Figma and for Cursor-driven component code generation.

🧱 2. Purpose of This Folder

The UI layer defines:

✔ How the system should look and feel

(Visual structure, hierarchy, spacing, grouping, positioning)

✔ Which components should be used

(Consistent design system across Admin App & SI App)

✔ Alignment with Storybook behavioural flows

(UI reflects the step-by-step journeys defined in 07_frontend/storybook/)

✔ Consistency across all companies

CephasOps serves multiple business verticals:

Cephas ISP operations

Kingsman (barbershop & spa)

Menorah (travel)

UI must adapt while maintaining the core system identity.

🗺️ 3. How This UI Folder Connects to the Rest of the Docs
docs/
   07_frontend/
      storybook/    ← What should happen (behaviour, flows, pages)
      ui/           ← How it should look (components, wireframes)

Relationship:
Layer	Purpose
Storybook	Functional behaviour & flow
UI	Visual implementation of that behaviour
Modules (02_modules)	Backend domain logic the UI consumes
API (04_api)	Endpoints UI interacts with
Data Model (05_data_model)	Shape of the data powering UI components

The UI folder does not define behaviour.
It defines appearance + component usage.

🧭 4. Development Guide for Frontend Engineers

When developing a screen:

1️⃣ Start with:

docs/07_frontend/storybook/PAGES.md
→ to know which page you are building.

2️⃣ Refer next to:

docs/07_frontend/ui/WIREFRAMES.md
→ to know how it should look.

3️⃣ Use components defined in:

docs/07_frontend/ui/COMPONENT_LIBRARY.md
→ reusable, consistent, company-themed UI.

4️⃣ Finally check:

docs/04_api/
→ to confirm the API structures required.

🧪 5. Used By

Frontend Developers — to design and build pages correctly

Cursor AI — to generate React components aligned with your spec

UI/UX Designers — as a master blueprint for Figma

QA Team — to validate layout and component usage

Project Owners — to maintain visual consistency

🎯 6. Goal of This Folder

To provide a single, authoritative UI source so the system can be built:

Fast

Consistent

Scalable

Multi-company ready

Cursor-friendly

Developer-friendly