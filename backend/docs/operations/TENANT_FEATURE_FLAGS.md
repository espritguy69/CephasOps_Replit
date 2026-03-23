# Tenant Feature Flags

**Date:** 2026-03-13  
**Purpose:** Control SaaS plan features per tenant; tenant admins cannot enable platform-only features.

## Overview

- **Service:** `IFeatureFlagService` / `FeatureFlagService` (Application/Platform/FeatureFlags).
- **Storage:** **TenantFeatureFlags** (TenantId, FeatureKey, IsEnabled, UpdatedAtUtc).

## Usage

```csharp
// Check before allowing a feature
if (!await _featureFlags.IsEnabledAsync("AdvancedReports", companyId, cancellationToken))
    throw new FeatureNotEnabledException("AdvancedReports");

// Or require and throw
await _featureFlags.RequireEnabledAsync("AdvancedReports", companyId, cancellationToken);
```

## Rules

- **Tenant admins** cannot enable features whose key starts with `Platform.` or `Admin.` (platform-only).
- **Platform admins** may set any flag via `SetFlagAsync(..., isPlatformAdmin: true)`.
- All resolution is tenant-scoped; no cross-tenant access.

## Safety

- Feature keys are tenant-scoped; no cross-tenant visibility.
- Platform-only keys are enforced in `SetFlagAsync`; tenant scope cannot elevate to platform features.
