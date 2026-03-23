# Stabilization Pass – OrderServiceIntegrationTests Build Blocker

**Date:** 2026-03-08

---

## 1. Root cause of the build failure

- **OrderService** (Application) had a new constructor parameter **IOrderPayoutSnapshotService** (added when payout snapshot behavior was introduced). The constructor requires 18 dependencies; the last is `IOrderPayoutSnapshotService`.
- **OrderServiceIntegrationTests** builds an **OrderService** in **CreateOrderService()** by passing 17 arguments (context, logger, and 15 service mocks), matching the previous constructor. The 18th parameter was never added.
- **Result:** `CS7036: There is no argument given that corresponds to the required parameter 'orderPayoutSnapshotService' of 'OrderService.OrderService(...)'`.

---

## 2. Files changed

| File | Change |
|------|--------|
| `backend/tests/CephasOps.Application.Tests/Phase2Settings/OrderServiceIntegrationTests.cs` | Added mock for `IOrderPayoutSnapshotService`, passed into `OrderService` in `CreateOrderService()`. |
| `backend/tests/CephasOps.Application.Tests/Auth/AuthServiceTests.cs` | Fixed flaky assertion in `ChangePasswordRequiredAsync_ValidRequest_ClearsMustChangePassword`: use `AsNoTracking()` and assert new password verifies instead of comparing hash to same tracked instance. Added null-forgiving for `PasswordHash` to satisfy nullable reference. |

---

## 3. Exact fix applied

**OrderServiceIntegrationTests.cs**

- Added `using CephasOps.Application.Rates.Services;`.
- Added field: `private readonly Mock<IOrderPayoutSnapshotService> _orderPayoutSnapshotServiceMock;`
- In constructor:
  - `_orderPayoutSnapshotServiceMock = new Mock<IOrderPayoutSnapshotService>();`
  - Stubbed the three interface methods so they do not return null when called:
    - `CreateSnapshotForOrderIfEligibleAsync` → `Returns(Task.CompletedTask)`
    - `GetSnapshotByOrderIdAsync` → `ReturnsAsync((OrderPayoutSnapshotDto?)null)`
    - `GetPayoutWithSnapshotOrLiveAsync` → `ReturnsAsync(new OrderPayoutSnapshotResponseDto())`
- In **CreateOrderService()**: added `_orderPayoutSnapshotServiceMock.Object` as the 18th (last) argument to `new OrderService(...)`.

**AuthServiceTests.cs**

- In **ChangePasswordRequiredAsync_ValidRequest_ClearsMustChangePassword**:
  - Reload user after the service call with `_context.Users.AsNoTracking().FirstAsync(u => u.Id == _userId)` so assertions are against a fresh instance.
  - Assert `MustChangePassword` is false, `PasswordHash` is not null/empty, and `DatabaseSeeder.VerifyPassword("newpassword456", updated.PasswordHash!)` is true (behavioral assertion instead of hash inequality on the same tracked entity).

---

## 4. Why this fix is the safest option

- **No production changes:** OrderService and its constructor are unchanged; only the test is updated to satisfy the current API.
- **Consistent with existing tests:** The test already mocks all other OrderService dependencies with Moq; adding one more mock follows the same pattern (no new test infrastructure).
- **Minimal stub behavior:** The three methods are stubbed with safe defaults (completed task, null snapshot, empty response DTO) so if any test or future expansion calls them, the test does not fail with null reference or unconfigured mock.
- **Localized:** Only the single test class that constructs OrderService was modified; no shared factories or production code.
- **Auth test fix:** The Auth test was failing because it compared `updated.PasswordHash` to `user.PasswordHash` where both referred to the same tracked entity after the service updated it; the new assertion checks that the new password verifies and uses a fresh read, which is more reliable and tests real behavior.

---

## 5. Test status after fix

- **Build:** `dotnet build` for `CephasOps.Application.Tests` succeeds (0 errors).
- **OrderServiceIntegrationTests:** 4 tests, all passed.
- **AuthServiceTests:** 5 tests, all passed.
- **AdminUserServiceTests:** 12 tests, all passed.
- **Combined filter:** `FullyQualifiedName~OrderServiceIntegrationTests|FullyQualifiedName~AuthServiceTests|FullyQualifiedName~AdminUserServiceTests` → **21 passed, 0 failed.**

---

## 6. Remaining unrelated warnings / TODOs

- **NU1902:** Package `MimeKit` 4.7.1 has a known moderate severity vulnerability (GHSA-g7hc-96xr-gvvx). Address by upgrading or replacing the package when convenient.
- **CS0618:** `Building.PropertyType` and `MaterialTemplate.BuildingTypeId` obsolete usage in tests (BuildingServiceTests, MaterialTemplateServiceTests). Follow-up: migrate to the recommended APIs.
- **CS8618 / CS0169:** EmailIngestionServiceTests – `_service` field never assigned/used. Consider initializing or removing.
- **CS0219:** ServiceInstallerServiceTests – variable `exceptionThrown` assigned but never used.
- **CS8601:** WorkflowEngineServiceTests – possible null reference assignments (lines 435–436).
- **CS1573:** IAdminUserService XML doc comments – some parameters missing `<param>` tags (pre-existing).

None of these are caused by the stabilization pass or v1.2; they can be handled in separate cleanup work.

---

## Summary

- **Root cause:** OrderService constructor gained `IOrderPayoutSnapshotService`; OrderServiceIntegrationTests was not updated.
- **Fix:** Add a Moq mock for `IOrderPayoutSnapshotService` with minimal stubs and pass it into `CreateOrderService()`. Harden one Auth test to avoid same-entity hash comparison.
- **Result:** Solution compiles; OrderServiceIntegrationTests, AuthServiceTests, and AdminUserServiceTests run successfully; v1.2 behavior and tests remain intact.
