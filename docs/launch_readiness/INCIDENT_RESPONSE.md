# Production Incident Response

Procedures for responding to common production incidents. Adjust severity levels and escalation to your organization’s policy.

---

## Tenant data incident

**Symptoms:** Wrong data shown to a tenant, data leak between tenants, or suspected tenant data corruption.

**Actions:**

1. **Contain:** If a leak is suspected, restrict access (e.g. disable affected tenant or API key) and preserve logs and DB state.
2. **Assess:** Confirm scope (which tenants, which data). Use audit logs, event store, and DB queries as needed. Involve security/legal if PII or compliance is involved.
3. **Remediate:** Fix the root cause (e.g. missing tenant filter, bug in shared code). Restore or correct data from backups if necessary.
4. **Communicate:** Notify affected tenant(s) per your SLA and legal requirements. Document the incident and post-mortem.

**Prevention:** Tenant safety analyzers (CEPHAS*), code review for tenant scope, and regular audits. See backend tenant safety docs.

---

## Job system failure

**Symptoms:** High job backlog, many failed or dead-letter jobs, or workers not processing.

**Actions:**

1. **Check health:** `/health/platform` → `jobbacklog` and `eventbus`. Confirm workers are running (`ProductionRoles:RunJobWorkers` on worker nodes).
2. **Identify cause:** Inspect dead-letter and failed jobs (type, error message, tenant). Check DB, Redis, and external dependencies (e.g. email, integrations).
3. **Mitigate:** Scale workers if under-provisioned; fix bug or dependency (e.g. timeout, credential); clear or replay dead-letter per runbook.
4. **Stabilize:** Ensure backlog drains and alert thresholds return to normal. Document and add monitoring/alerting if missing.

**Prevention:** Job backlog and event bus health checks; alerting (see [ALERTING_RULES.md](ALERTING_RULES.md)); periodic review of job types and failure rates.

---

## Database overload

**Symptoms:** High latency, connection pool exhaustion, or DB CPU/memory high.

**Actions:**

1. **Check health:** `/health/ready` and DB metrics (connections, slow queries).
2. **Reduce load:** Throttle or queue non-critical work; scale read replicas if available; block or rate-limit abusive tenants if identified.
3. **Identify expensive operations:** Use slow-query logs, APM, or metrics to find heavy queries or N+1 patterns. Optimize or add caching.
4. **Scale or tune:** Add capacity, tune connection pool and timeouts, or schedule heavy jobs off-peak.

**Prevention:** Connection and query monitoring; indexing and query review; load testing before launch.

---

## Signup outage

**Symptoms:** New tenants cannot sign up or complete provisioning.

**Actions:**

1. **Check dependencies:** DB, Redis (if used in signup), and any external auth or billing provider. Confirm `/health/ready` and provisioning endpoints.
2. **Verify config:** Signup and provisioning settings (e.g. feature flags, billing provider). Check recent deployments and config changes.
3. **Fix and retry:** Resolve outage (e.g. DB back up, config fix). Retry failed signups or guide users to retry.
4. **Communicate:** Notify affected users if signup was partially completed or data was impacted.

**Prevention:** Signup flow in staging; health checks and alerting on provisioning dependencies.

---

## Storage quota incidents

**Symptoms:** Tenant over quota, uploads failing, or storage metrics spiking.

**Actions:**

1. **Verify usage:** Confirm tenant storage usage and quota from metrics or admin API.
2. **Short-term:** Temporarily raise quota if policy allows, or guide tenant to reduce usage (e.g. delete old files). Throttle uploads if needed.
3. **Long-term:** Enforce and communicate quota policy; implement or tune retention and lifecycle (e.g. storage lifecycle worker). Consider tenant notifications before hard limit.

**Prevention:** Quota and storage growth monitoring; alerts (see [ALERTING_RULES.md](ALERTING_RULES.md)); documented quota and retention policy.

---

## General

- **Log and document:** Keep a log of detection time, actions, and resolution. Run a blameless post-mortem for significant incidents.
- **Update runbooks:** After each incident, update this document and [ALERTING_RULES.md](ALERTING_RULES.md) with new learnings and thresholds.

## See also

- [ALERTING_RULES.md](ALERTING_RULES.md) — When to fire alerts.
- [GO_LIVE_CHECKLIST.md](GO_LIVE_CHECKLIST.md) — Pre-launch readiness.
