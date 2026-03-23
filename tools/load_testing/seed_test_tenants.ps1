# Load-test seed script: provisions multiple tenants (and documents user/order/job/file targets).
# Targets: 50 tenants, 200 users, 1000 orders, background jobs, files.
# Requires: Backend API running; SuperAdmin JWT in $env:LOAD_TEST_JWT or pass -Token.
# Usage:
#   $env:LOAD_TEST_JWT = "eyJ..."; .\seed_test_tenants.ps1 -BaseUrl "http://localhost:5000"
#   .\seed_test_tenants.ps1 -BaseUrl "http://localhost:5000" -Token "eyJ..."

param(
    [Parameter(Mandatory = $true)]
    [string] $BaseUrl,
    [string] $Token = $env:LOAD_TEST_JWT,
    [int] $TenantCount = 50,
    [switch] $WhatIf
)

$ErrorActionPreference = "Stop"
$api = $BaseUrl.TrimEnd("/")

if (-not $Token) {
    Write-Host "Provide JWT via -Token or env LOAD_TEST_JWT (SuperAdmin). Login first: POST $api/api/auth/login with SuperAdmin credentials."
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type"  = "application/json"
}

$provisionUrl = "$api/api/platform/tenants/provision"
$created = 0
$failed = 0

for ($i = 1; $i -le $TenantCount; $i++) {
    $code = "LOAD" + $i.ToString("00")
    $body = @{
        CompanyName   = "Load Test Company $i"
        CompanyCode   = $code
        Slug          = "load-test-$i"
        AdminFullName = "Admin $i"
        AdminEmail    = "loadadmin$i@loadtest.local"
        PlanSlug      = "trial"
        TrialDays     = 365
        InitialStatus = "Active"
    } | ConvertTo-Json

    if ($WhatIf) {
        Write-Host "WhatIf: would provision tenant $code"
        $created++
        continue
    }

    try {
        $resp = Invoke-RestMethod -Uri $provisionUrl -Method Post -Headers $headers -Body $body
        if ($resp.Success) {
            $created++
            Write-Host "Provisioned $code (TenantId: $($resp.Data.TenantId))"
        } else {
            $failed++
            Write-Warning "Provision $code failed: $($resp.Message)"
        }
    } catch {
        $failed++
        Write-Warning "Provision $code error: $_"
    }
}

Write-Host "`nDone. Created: $created, Failed: $failed"

# Targets for full load test data (users, orders, jobs, files) — use API or DB seed as needed:
# - 200 users: ~4 users per tenant via POST /api/users (tenant-scoped) or user invite flow
# - 1000 orders: ~20 orders per tenant via Orders API or import
# - Background jobs: enqueue via app (e.g. report export, sync) or JobExecutionEnqueuer
# - Files: upload via POST /api/files per tenant
# See docs/saas_scaling/LOAD_TEST_PLAN.md for scenarios.
