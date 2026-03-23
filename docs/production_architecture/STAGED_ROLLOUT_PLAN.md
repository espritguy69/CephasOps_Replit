# Staged Rollout Architecture

**Purpose:** Roll out CephasOps safely to production: internal → pilot → limited production → full rollout; monitoring and rollback at each phase.

---

## 1. Phase overview

| Phase | Scope | Success criteria | Stop/rollback criteria |
|-------|--------|-------------------|-------------------------|
| **Internal only** | Platform team and internal test tenants only | Stable API and workers; no critical Guardian findings; migrations apply cleanly | Repeated crashes; critical drift; data integrity issue |
| **Pilot tenants** | 1–5 selected external or friendly tenants | Login, core flows (orders, jobs, files) work; tenant health Healthy; no critical anomalies | Critical anomaly; tenant data mix-up; persistent failures |
| **Limited production** | 10–20% of target tenant base or cap (e.g. 50 tenants) | Latency and error rate within SLO; job queue stable; Guardian drift/anomaly acceptable | SLO breach; rate limit or quota bugs; security finding |
| **Full rollout** | All target tenants; open signup if applicable | Same as limited; capacity and cost validated | Platform-wide incident; rollback to limited |

---

## 2. Phase 1: Internal only

- **Tenants:** Create 1–3 internal tenants (e.g. company code INTERNAL, PILOT). Use for end-to-end testing and demos.
- **Traffic:** Only platform team and automated tests. No external signup yet (or signup disabled).
- **Monitoring:** Platform health daily; drift and anomaly endpoints; job queue depth; DB health. Logs and metrics reviewed for errors.
- **Checkpoints:** (1) All ProductionRoles run without crash. (2) Guardian shows no critical drift. (3) Tenant health Healthy for internal tenants. (4) One full flow per critical path (order, job, file, report).
- **Rollback:** Disable new signup; fix forward or roll back app release; no tenant data migration needed (internal only).

---

## 3. Phase 2: Pilot tenants

- **Tenants:** Invite 1–5 pilot customers; provision via platform (POST /api/platform/tenants/provision) or allow signup with invite code.
- **Traffic:** Real but limited; pilot tenants use core features. Monitor per-tenant metrics.
- **Monitoring:** GET /api/platform/analytics/tenant-health for each pilot; GET /api/platform/analytics/anomalies (severity Critical/Warning); rate limit 429 count; failed job count per tenant.
- **Checkpoints:** (1) No critical anomaly for pilot tenants. (2) No cross-tenant data exposure (manual spot check). (3) Support runbooks validated (impersonation, retry job, quota). (4) Pilot feedback incorporated.
- **Rollback:** Pause new pilot onboarding; fix issues; optionally move pilot to previous environment if severe.

---

## 4. Phase 3: Limited production rollout

- **Tenants:** Cap at 50 or 10–20% of target; open signup or controlled invite. Tenant enablement flags if you support feature flags per tenant.
- **Traffic:** Production-like; rate limits and quotas enforced. Multiple tenants active.
- **Monitoring:** Platform health; performance-health (degraded tenants, pending jobs); anomaly count; rate limit breaches; DB and API latency. Set alerts (e.g. critical anomaly > 0, pending jobs > 500, error rate > 1%).
- **Checkpoints:** (1) SLO met (availability, latency). (2) No critical drift; anomaly count within expected range. (3) Job queue and storage lifecycle stable. (4) No security or isolation incident.
- **Rollback:** Stop new signup; fix and redeploy; or roll back app; communicate to affected tenants if downtime or data impact.

---

## 5. Phase 4: Full rollout

- **Tenants:** Remove cap; open signup if desired; all target segments.
- **Traffic:** Full production; scale API and workers per DEPLOYMENT_ARCHITECTURE and WORKER_SCALING.
- **Monitoring:** Same as limited; capacity and cost monitored; retention and backup validated.
- **Checkpoints:** (1) Capacity and cost within plan. (2) Staged rollout runbooks and playbooks updated. (3) Support and ops trained.
- **Rollback:** Same as limited; full rollback is last resort; prefer fix forward.

---

## 6. Tenant enablement flags

- If supported in your model: **TenantFeatureFlags** or subscription plan can gate features (e.g. advanced reporting, SI app). Use to gradually enable features per tenant segment during rollout.
- **InitialStatus** or **OnboardingProgress** can indicate “pilot” vs “full” for support and monitoring (e.g. prioritize pilot tenants in runbooks).

---

## 7. Guardian and analytics during rollout

- **Guardian:** Run PlatformGuardianHostedService in all phases. Review drift and anomaly reports after each deploy; act on Critical.
- **Analytics:** Use GET /api/platform/analytics/platform-health and tenant-health as the main operational view; track TenantsInWarningState, TenantsInCriticalAnomalyState, FailedJobsLast24h, PerformanceDegradationFlag.
- **Alerts:** Configure alerts on critical anomaly count, platform health summary, and job queue depth before limited production.

---

## 8. Rollback triggers

- **Immediate:** Data corruption or cross-tenant data exposure; critical security finding; persistent 5xx or health check failure.
- **Planned:** SLO breach for 2+ consecutive checks; critical drift that cannot be fixed quickly; pilot or tenant-reported blocker that cannot be worked around.
- **Communication:** Notify support/ops and affected tenants per incident plan; post-mortem after rollback.

---

## 9. Communication guidance

- **Internal:** Announce phase transitions and go/no-go in ops channel; document decisions and checkpoints.
- **Pilot tenants:** Set expectation that pilot is for feedback and may have short outages or fixes; provide support contact.
- **Production tenants:** Use status page or email for planned maintenance and incidents; document in OPERATIONAL_RUNBOOKS and support procedures.

---

## 10. References

- [DEPLOYMENT_ARCHITECTURE.md](DEPLOYMENT_ARCHITECTURE.md)
- [PRODUCTION_RUNBOOKS.md](PRODUCTION_RUNBOOKS.md)
- [docs/platform_guardian/PLATFORM_GUARDIAN_SUMMARY.md](../platform_guardian/PLATFORM_GUARDIAN_SUMMARY.md)
