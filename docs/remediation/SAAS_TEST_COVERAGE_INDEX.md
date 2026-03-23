# SaaS Test Coverage Index

**Purpose:** Index of key test files that protect SaaS hardening. When adding tenant-scoped behaviour, add or extend tests in these areas and follow the same patterns. When changing CI or PR checks, ensure these suites remain in scope.

**See also:** [SAAS_REGRESSION_GUARDRAILS.md](../operations/SAAS_REGRESSION_GUARDRAILS.md), [SAAS_CODE_REVIEW_CHECKLIST.md](SAAS_CODE_REVIEW_CHECKLIST.md).

---

## Tenant fallback removal

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/TenantFallbackRemovalTests.cs` | Services throw when `companyId` is null or `Guid.Empty`; no Guid.Empty-as-all-companies | Unit |
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/SingleCompanyModeRemovalTests.cs` | No single-company mode assumptions in tenant-scoped code paths | Unit |
| `backend/tests/CephasOps.Application.Tests/Architecture/TenantSafetyInvariantTests.cs` | Architectural invariants (e.g. executor usage, guard usage) | Unit |

---

## Financial isolation

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Application.Tests/Common/FinancialIsolationGuardTests.cs` | FinancialIsolationGuard behaviour (RequireTenantOrBypass, RequireCompany, RequireSameCompany) | Unit |
| `backend/tests/CephasOps.Application.Tests/Billing/BillingServiceFinancialIsolationTests.cs` | Billing service company mismatch and tenant context | Unit |
| `backend/tests/CephasOps.Application.Tests/Billing/PaymentServiceFinancialSafetyTests.cs` | Payment service financial safety and tenant isolation | Unit |
| `backend/tests/CephasOps.Application.Tests/Rates/OrderPayoutSnapshotServiceFinancialIsolationTests.cs` | Payout snapshot tenant isolation | Unit |
| `backend/tests/CephasOps.Api.Tests/Integration/FinancialIdempotencyApiTests.cs` | Financial idempotency at API level | Integration / API |

---

## EventStore consistency

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Application.Tests/Events/EventStoreConsistencyGuardTests.cs` | EventStoreConsistencyGuard (append, entity context, parent/root, stream) | Unit |
| `backend/tests/CephasOps.Application.Tests/Events/EventStoreRepositoryConsistencyTests.cs` | Event store repository consistency and tenant/bypass behaviour | Unit |
| `backend/tests/CephasOps.Application.Tests/Events/EventReplayTests.cs` | Replay tenant scope and tenant-mismatch handling | Unit |
| `backend/tests/CephasOps.Application.Tests/Events/OperationalReplayTests.cs` | Operational replay behaviour and tenant context | Unit |

---

## Platform observability

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Api.Tests/Integration/PlatformObservabilityApiTests.cs` (or equivalent) | Platform analytics restricted to SuperAdmin/AdminTenantsView; tenant users get 403 | Integration / API |
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/TenantOperationalObservabilityTests.cs` | Tenant-scoped operational observability and access control | Unit |
| `backend/tests/CephasOps.Application.Tests/Admin/OperationsOverviewServiceTests.cs` | Operations overview service platform vs tenant behaviour | Unit |

---

## Operational insights / tenant dashboards

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/OperationalInsightsTenantIsolationTests.cs` | OperationalInsightsService rejects Guid.Empty; tenant-scoped methods require valid CompanyId | Unit |
| `backend/tests/CephasOps.Api.Tests/Integration/OperationalInsightsApiTests.cs` | Platform-health requires AdminTenantsView; tenant endpoints require company context (403 when missing) | Integration / API |

---

## SI-app tenant isolation

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/SiAppTenantIsolationTests.cs` | SI-app material/order access constrained by tenant and SI assignment | Unit |

---

## Tenant scope execution and persistence

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Application.Tests/Integration/TenantScopeExecutorTests.cs` | TenantScopeExecutor scope and bypass behaviour | Unit / Integration |
| `backend/tests/CephasOps.Application.Tests/Integration/InboundWebhookRuntimeTenantScopeTests.cs` | Webhook runtime tenant scope and bypass | Unit / Integration |
| `backend/tests/CephasOps.Application.Tests/Persistence/SaveChangesTenantIntegrityTests.cs` | SaveChanges tenant validation (TenantSafetyGuard) | Unit |
| `backend/tests/CephasOps.Api.Tests/Integration/TenantIsolationIntegrationTests.cs` | End-to-end tenant isolation at API boundary | Integration / API |
| `backend/tests/CephasOps.Api.Tests/Integration/TenantBoundaryTests.cs` | Tenant boundary behaviour at API level | Integration / API |

---

## Other tenant-aware tests

| Test file | Protects | Type |
|-----------|----------|------|
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/BillingRatecardTenantIsolationTests.cs` | Billing ratecard tenant isolation | Unit |
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/PnlAndSkillTenantIsolationTests.cs` | P&amp;L and skill tenant isolation | Unit |
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/EmailTemplateTenantAwarenessTests.cs` | Email template tenant awareness | Unit |

---

**Running tests:** From repo root, run Application tests with `dotnet test backend/tests/CephasOps.Application.Tests` and Api tests with `dotnet test backend/tests/CephasOps.Api.Tests`. For PRs touching tenant-scoped services, run the full suite; focus on TenantIsolation, Events, Common (FinancialIsolationGuard), and Integration tenant/API tests.
