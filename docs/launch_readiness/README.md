# Launch Readiness

This folder contains the **CephasOps Launch Readiness & Go-Live** documentation and procedures.

## Contents

| Document | Purpose |
|----------|---------|
| [ENVIRONMENT_VALIDATION.md](ENVIRONMENT_VALIDATION.md) | Production startup checks: database, Redis, secrets, rate-limit, Guardian, worker config |
| [HEALTH_CHECKS.md](HEALTH_CHECKS.md) | Health endpoints: `/health`, `/health/ready`, `/health/platform` and per-check semantics |
| [OPERATIONS_DASHBOARDS.md](OPERATIONS_DASHBOARDS.md) | Suggested dashboards: Platform Overview, Tenant Health, Infrastructure |
| [ALERTING_RULES.md](ALERTING_RULES.md) | Critical and warning alert rules for production monitoring |
| [TENANT_ONBOARDING_PLAYBOOK.md](TENANT_ONBOARDING_PLAYBOOK.md) | Step-by-step tenant onboarding and verification |
| [INCIDENT_RESPONSE.md](INCIDENT_RESPONSE.md) | Procedures for tenant data, job system, DB, signup, storage incidents |
| [GO_LIVE_CHECKLIST.md](GO_LIVE_CHECKLIST.md) | Final pre-launch checklist and sign-off |
| [CEPHASOPS_LAUNCH_READINESS_REPORT.md](CEPHASOPS_LAUNCH_READINESS_REPORT.md) | Launch summary: architecture, infrastructure, operations, monitoring, rollout, go/no-go |

## Quick start

1. Before production: complete [GO_LIVE_CHECKLIST.md](GO_LIVE_CHECKLIST.md) and ensure [ENVIRONMENT_VALIDATION.md](ENVIRONMENT_VALIDATION.md) requirements are met.
2. Use [HEALTH_CHECKS.md](HEALTH_CHECKS.md) to wire readiness probes and operations views.
3. Configure dashboards and alerts from [OPERATIONS_DASHBOARDS.md](OPERATIONS_DASHBOARDS.md) and [ALERTING_RULES.md](ALERTING_RULES.md).
4. Onboard first tenants with [TENANT_ONBOARDING_PLAYBOOK.md](TENANT_ONBOARDING_PLAYBOOK.md).
5. Use [INCIDENT_RESPONSE.md](INCIDENT_RESPONSE.md) when handling incidents.

The [CEPHASOPS_LAUNCH_READINESS_REPORT.md](CEPHASOPS_LAUNCH_READINESS_REPORT.md) gives the overall go/no-go and remaining risks.
