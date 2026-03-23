# Platform Safety Drift Report

*Generated: 2026-03-13 14:20 UTC. Do not edit by hand. Run `./tools/architecture/run_platform_safety_drift.ps1` after artifact regeneration.*

---

## Safety posture summary

| Metric | Baseline | Current | Delta |
|--------|----------|---------|-------|
| Enforced findings | 4 | 4 | 0 |
| Advisory findings | 17 | 17 | 0 |
| Documented-only findings | 0 | 1 | 1 |

**Drift detected:** True  
**Summary:** Drift detected: 1 change(s). Review recommended.

---

## Detected changes


- Documented-only findings: 0 -> 1 (delta 1)


---

## New sensitive files requiring review

 None.


---

## Bypass footprint

- Allowlist increased: False
- Unallowlisted bypass/scope increased: False
- Detail: allowlist manual: 10 -> 10; executor not required: 8 -> 8; unallowlisted enter: 0 -> 0; exit: 0 -> 0; currentTenantId: 4 -> 4

---

## Investigation recommended?

Yes. Review the detected changes below and [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md), [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) as needed.

---

## Advisory vs enforced drift

- **Enforced drift** (increase in enforced findings) usually indicates new unallowlisted manual scope or executor gaps; CI may already fail. Fix or add justified allowlist entry; see [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md).
- **Advisory drift** (increase in advisory findings) is for visibility only; it does **not** cause CI failure. Review new advisory findings when touching related code; do not convert advisory drift into CI failures.

Machine-readable: `tools/architecture/platform_safety_drift_report.json`. Baseline: `tools/architecture/platform_guardian_baseline.json`.