# 🖥️ CephasOps Admin Web Portal

This folder contains the **Admin Web UI** for CephasOps.

The Admin Portal is used by:

- Operations team (Orders, Dockets, Blockers, Reschedules)
- Scheduler team (calendar, SI availability)
- Inventory & RMA team
- Finance team (Billing, Payments, e-Invoice)
- Payroll team (SI rate plans, payroll runs)
- Directors (P&L dashboards, KPIs)
- System admins (Settings, RBAC, Global config)

The code in this folder is generated and maintained according to the specs in:

- `/docs/07_frontend`
- `/docs/02_modules`
- `/docs/05_data_model`
- `/docs/04_api`

---

## 1. Tech Stack (target for Cursor)

Suggested stack (can be adjusted):

- Framework: **React + TypeScript** (or Blazor WebAssembly if preferred)
- Routing: React Router / Next.js routing
- State: React Query / Zustand / Redux Toolkit
- UI Library: Headless + custom components (from docs `COMPONENT_LIBRARY`)
- HTTP Client: Fetch/axios typed from `/docs/04_api`
- Auth: Token-based (JWT / cookie) with role + company scoping

Cursor should scaffold this automatically.

---

## 2. Recommended Folder Structure

```text
frontend/
  src/
    app/                  ← app shell, routing, layout
    modules/              ← feature-based modules
      orders/
      scheduler/
      si-app-admin/
      inventory/
      billing/
      payroll/
      pnl/
      settings/
      rbac/
      parser/
      documents/
    components/           ← shared UI components
    hooks/                ← shared hooks (useApi, useCompanyScope, etc.)
    services/             ← API clients generated from docs/04_api
    utils/
    styles/
  public/
  package.json
  tsconfig.json
  vite.config.ts / next.config.js
  README.md               ← this file
Each modules/<name> maps directly to a module in /docs/02_modules.

3. Mapping to Documentation
When building or regenerating this app, always reference:

/docs/07_frontend/ui → Screens, layouts, forms, UX patterns

/docs/07_frontend/storybook → End-to-end flows

/docs/05_data_model/entities → Field names, types

/docs/04_api → Endpoints, payloads

/docs/03_business → Business rules, KPIs

This keeps UI, API, and data model perfectly aligned.

4. Roles & Access
The Admin Portal is role-aware and company-aware:

Company context from CompanyId

Role/permissions from RBAC (see /docs/02_modules/RBAC_MODULE.md)

Typical roles:

Admin

Scheduler

Inventory

Finance

Payroll

Director

Viewer

Frontend must hide/show modules and actions based on role + company.

5. Development Workflow
Read relevant module docs under /docs/02_modules.

Check entities/relationships under /docs/05_data_model.

Implement or regenerate pages in src/modules/<module>.

Wire to APIs described in /docs/04_api.

Run tests and linting before PR.

6. Environment & Config
Environment variables (example):

VITE_API_BASE_URL – Backend API URL

VITE_ENVIRONMENT – Dev / Staging / Prod

VITE_SENTRY_DSN – Optional error tracking

VITE_FEATURE_FLAGS – Flags for beta features

See /environments for environment-specific files.

7. Testing
Recommended:

Unit tests (components & hooks)

Integration tests for key flows (Orders, Scheduler, Billing)

E2E tests with Playwright/Cypress

Test specs should follow the flows documented in /docs/07_frontend/storybook.

8. Relationship with Other Folders
/frontend-si – separate SI mobile app (field usage)

/docs/07_frontend – this app’s specification, source of truth

/backend – REST API serving this app

/infra – deployment config for hosting static assets / SPA

yaml
Copy code

---

## 2️⃣ `/frontend-si/README.md` – Service Installer App

```md
# 📱 CephasOps Service Installer (SI) App

This folder contains the **Service Installer-facing UI** used in the field:

- View assigned jobs
- Start job sessions
- Record job events (OTW, Arrived, Met Customer, Testing, Completed)
- Capture photos (with GPS + timestamp)
- Scan device serial numbers
- Submit manual docket summaries
- Handle reschedules / blockers on site

This app is separate from the Admin Portal (`/frontend`) and is **mobile-first**.

---

## 1. Purpose

- Provide SIs with a **simple, fast** interface on phone/tablet.
- Collect **evidence** (photos, scans, GPS) for billing and quality control.
- Integrate tightly with:
  - Orders module
  - Scheduler module
  - SI App backend (SiJobSession, SiJobEvent, SiPhoto, SiDeviceScan)
  - Inventory (serial numbers)
  - Payroll & P&L (job completion events)

---

## 2. Tech Stack (target for Cursor)

Suggested options:

- **React Native + TypeScript**
  or
- **Blazor Hybrid / MAUI**

Features required:

- Camera access
- GPS access
- Local caching (offline support – later)
- API calls to `/api/si-app/...`

---

## 3. Recommended Folder Structure

```text
frontend-si/
  src/
    app/
    screens/
      jobs/
      job-session/
      photos/
      scans/
      profile/
    components/
      buttons/
      forms/
      tiles/
      status-badges/
    services/           ← API clients for /api/si-app and /api/orders
    hooks/
    utils/
  assets/
  app.json / capacitor.config / etc.
  package.json
  README.md
Each screen maps to flows in /docs/07_frontend/storybook (SI stories).

4. Mapping to Documentation
When building this app, always refer to:

/docs/07_frontend/storybook:

SI job flow

JobSession events

Photo + scan requirements

/docs/07_frontend/ui:

SI mobile screens

Components for tiles, lists, and action bars

/docs/05_data_model/entities/si_app_entities.md:

SiJobSession, SiJobEvent, SiPhoto, SiDeviceScan, SiLocationPing

/docs/04_api:

/api/si-app/... endpoints

/api/orders/... for job details

5. SI App Business Rules (Important)
Only the assigned SI for an order may start a job session.

Only one active session per SI per order.

Events, photos, and scans must be tied to the current session.

GPS coordinates & timestamp should be included with:

Events

Photos

Device scans (when possible)

Optional future features:

Offline mode

Background GPS pings (LocationPing)

All rules are fully described in SERVICE_INSTALLER_APP_MODULE.md.

6. Environment
Typical variables:

API_BASE_URL

ENVIRONMENT (dev/staging/prod)

SENTRY_DSN (optional)

Any mobile-specific config (push notifications etc.)

7. Testing
Minimum:

Screen-level tests (navigation, required fields)

API integration mocks

Permission flows (location/camera denied vs allowed)

Happy path: start → complete job session

8. Relationship with Other Folders
/frontend – Admin web portal (not used by SI)

/backend – SI App API implementation

/docs/07_frontend – UI + storybook specs

/docs/02_modules/SERVICE_INSTALLER_APP_MODULE.md – backend & domain spec

/infra – deployment config (APK/IPA build pipelines later)

yaml
Copy code

---

## 3️⃣ `/infra/frontend-deployment.md` – Deployment Guide

```md
# 🚀 CephasOps Frontend Deployment Guide

This document describes how to **build and deploy** the CephasOps frontends:

- Admin Web Portal (`/frontend`)
- Service Installer App (`/frontend-si`)

It is infrastructure-focused and should be kept in sync with CI/CD pipelines.

---

## 1. Environments

Typical environments:

- **DEV** – internal testing
- **STAGING/UAT** – near-production, client testing
- **PROD** – live

Configuration values per environment are stored in `/environments` and/or CI secrets.

---

## 2. Admin Web Portal (`/frontend`)

### 2.1 Build

Example (React + Vite/Next):

```bash
cd frontend
npm install
npm run build
This generates a static bundle (e.g. dist/ or .next/).

2.2 Deploy Targets (options)
S3 + CloudFront (static SPA)

NGINX serving static files

Azure Static Web Apps / Vercel / Netlify

The app must be configured to:

Serve index.html for all routes (SPA routing)

Forward API calls to backend (/api) via reverse proxy or CORS

2.3 Required Environment Variables
At minimum:

API_BASE_URL – URL of backend API (e.g. https://api.cephasops.com)

ENVIRONMENT – dev / staging / prod

COMPANY_SELECTOR_MODE – Single vs multi-company

Optional: logging/monitoring (Sentry, etc.)

These are injected at build time or runtime depending on setup.

3. Service Installer App (/frontend-si)
3.1 Build
Example (React Native):

bash
Copy code
cd frontend-si
npm install

# Android
npx expo run:android   # or gradle build pipeline

# iOS
npx expo run:ios       # or Xcode build pipeline
The actual commands depend on framework (React Native / MAUI / Blazor Hybrid).

3.2 Distribution
Internal testing: TestFlight / Internal App Sharing / APK sideload

Production: Google Play Store / Apple App Store (or private MDM)

Environment variables must point to:

Production API base URL

Correct auth endpoints

4. Backend Integration
Both frontends call the backend hosted under:

https://<env>.api.cephasops.com (example)

All endpoints are defined in:

/docs/04_api/API_BLUEPRINT.md

/docs/04_api/API_CONTRACTS_SUMMARY.md

API versioning (if used) must be reflected here.

5. Static Assets & Caching
For the Admin Portal:

Enable caching for static assets (JS, CSS, images)

Use cache-busting based on build hash

Ensure index.html is cache-controlled appropriately to allow fresh deployments

6. CI/CD (Outline)
Typical pipeline steps:

Checkout repo

Install dependencies

Run tests (npm test, npm run lint, backend tests)

Build frontend (npm run build)

Upload artifact (S3/CloudFront, static host, or app distribution)

Invalidate cache / restart services

Notify (Slack/Teams/email)

Separate pipelines can be configured for:

/frontend

/frontend-si

/backend

7. Monitoring & Logging
Frontend errors → browser logging + Sentry / AppCenter

Backend logs → centralised logging (ELK, CloudWatch, etc.)

Uptime monitoring for:

API

Web app URL

SI App API endpoints

8. Security Considerations
Enforce HTTPS only

Use secure cookies or secure token storage

Enable CORS rules on API for allowed origins only

Lock down admin URLs with proper auth + RBAC

Protect build secrets in CI/CD (API keys, DSNs, etc.)

9. Change Management
Any change to deployment process should be reflected in:

This file (infra/frontend-deployment.md)

Environment documentation under /environments

Release notes / CHANGELOG