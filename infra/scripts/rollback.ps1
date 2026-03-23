# CephasOps rollback script (Docker Compose or Kubernetes).
# Usage:
#   Docker: .\rollback.ps1 -Mode Docker
#   K8s:    .\rollback.ps1 -Mode Kubernetes -Namespace cephasops [-Revision 0]
param(
    [ValidateSet('Docker', 'Kubernetes')]
    [string] $Mode = 'Docker',
    [string] $Namespace = 'cephasops',
    [int] $Revision = 0
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Rollback-Docker {
    & docker compose -f (Join-Path $RepoRoot 'infra/docker/docker-compose.yml') down
    if ($LASTEXITCODE -ne 0) { throw 'Docker compose down failed' }
    Write-Host 'Rollback (Docker): stack stopped. To restore previous image, re-run deploy with -Build and previous tag.'
}

function Rollback-Kubernetes {
    & kubectl rollout undo deployment/cephasops-api -n $Namespace
    if ($Revision -gt 0) {
        & kubectl rollout undo deployment/cephasops-api -n $Namespace --to-revision=$Revision
    }
    if ($LASTEXITCODE -ne 0) { throw 'kubectl rollout undo failed' }
    & kubectl rollout status deployment/cephasops-api -n $Namespace
    Write-Host "Rollback (Kubernetes) complete. Check: kubectl get pods -n $Namespace"
}

switch ($Mode) {
    Docker     { Rollback-Docker }
    Kubernetes { Rollback-Kubernetes }
}
