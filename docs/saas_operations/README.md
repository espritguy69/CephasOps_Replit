# SaaS Platform Operations

**Date:** 2026-03-13

Documentation for day-to-day SaaS platform operations: onboarding, billing integration, support procedures, and runbooks.

---

## Contents

| Document | Purpose |
|----------|---------|
| [ONBOARDING_FLOW.md](ONBOARDING_FLOW.md) | Self-service signup (POST /api/platform/signup), first login, onboarding wizard (GET/PATCH /api/onboarding/status) |
| [BILLING_ARCHITECTURE.md](BILLING_ARCHITECTURE.md) | Billing provider abstraction (IBillingProviderService), integration approach, stub implementation |
| [SUPPORT_PROCEDURES.md](SUPPORT_PROCEDURES.md) | Tenant diagnostics, logs hint, impersonation, job retry; SuperAdmin only; audit |
| [OPERATIONAL_RUNBOOKS.md](OPERATIONAL_RUNBOOKS.md) | Runbooks: signup, login issues, storage quota, failed jobs, impersonation, dashboard |

---

## Progress index

All SaaS phases (readiness → production readiness → scaling → hardening → platform operations) are summarized in **[../SAAS_PLATFORM_PROGRESS.md](../SAAS_PLATFORM_PROGRESS.md)**. Use that doc to see what’s been completed and where each deliverable is documented.
