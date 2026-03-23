# CephasOps SaaS System Overview

**Date:** 13 March 2026  
**Purpose:** High-level overview of the CephasOps SaaS architecture for stakeholders and documentation.

---

## CephasOps SaaS Platform — Visual Overview

```mermaid
flowchart LR

subgraph Users
U1[Installer / Tenant User]
U2[Company Admin]
U3[Platform Admin]
end

subgraph Frontend
FE[React Web App]
CTX[Department / Tenant Context]
CACHE[React Query Cache]
GUARDS[Route & Permission Guards]
end

subgraph API Layer
API[REST API]
MW[Tenant Guard Middleware]
TP[ITenantProvider]
TS[TenantScope]
AUTH[Authorization Checks]
end

subgraph Application Services
SV1[Order & Workflow Services]
SV2[Messaging & Parser Services]
SV3[Financial Services]
SV4[Platform Analytics Service]
end

subgraph Data Protection
EF[EF Query Filters]
TSG[TenantSafetyGuard]
DB[(Primary Database)]
end

subgraph Financial Isolation
RATE[BillingRatecardService]
PNL[PnlService]
PAYOUT[OrderPayoutSnapshotService]
end

subgraph Observability
METRICS[Tenant Operational Metrics]
ANOM[Anomaly Detection]
DASH[Platform Observability Dashboard]
end

U1 --> FE
U2 --> FE
U3 --> FE

FE --> CTX
CTX --> CACHE
FE --> GUARDS
FE --> API

API --> MW
MW --> TP
TP --> TS
TS --> AUTH

AUTH --> SV1
AUTH --> SV2
AUTH --> SV3
AUTH --> SV4

SV1 --> EF
SV2 --> EF
SV3 --> RATE
SV3 --> PNL
SV3 --> PAYOUT

RATE --> EF
PNL --> EF
PAYOUT --> EF

EF --> DB
TSG --> DB

DB --> METRICS
METRICS --> ANOM
ANOM --> DASH

U3 --> DASH
```

---

## How the System Works

### 1. Tenant Context

Every request begins with a **tenant context resolution**.

The system determines which company the request belongs to using:

- authentication token
- tenant provider
- tenant scope

All subsequent operations use this context.

### 2. Tenant Data Isolation

Multiple layers enforce strict tenant separation:

| Layer | Protection |
|-------|------------|
| Frontend | cache invalidation and tenant context control |
| API | middleware tenant validation |
| Services | tenant-scoped logic |
| Database | automatic query filtering |
| Write validation | tenant ownership enforcement |

This ensures companies cannot access each other's data.

### 3. Financial Protection

Financial operations are fully tenant-scoped.

**Protected services include:**

- Billing ratecards
- Installer payouts
- Profit and loss calculations
- Payout snapshots

Financial calculations automatically fail if tenant context is missing.

### 4. Operational Monitoring

CephasOps includes a platform-level observability dashboard.

**Administrators can monitor:**

- tenant activity
- job execution health
- notification delivery
- integration status
- anomaly events

This allows platform operators to detect issues early.

---

## Key Platform Principles

CephasOps follows these SaaS design principles:

- **Strict tenant isolation**
- **Fail-closed security model**
- **Financial calculation safety**
- **Defense-in-depth architecture**
- **Platform operational visibility**

---

## Result

CephasOps now operates as a secure multi-tenant SaaS platform capable of supporting multiple companies while maintaining strict data and financial separation.
