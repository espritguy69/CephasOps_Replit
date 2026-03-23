# CephasOps deploy script (Docker Compose or Kubernetes).
# Usage:
#   Docker: .\deploy.ps1 -Mode Docker -EnvFile .env
#   K8s:    .\deploy.ps1 -Mode Kubernetes -Namespace cephasops
param(
    [ValidateSet('Docker', 'Kubernetes')]
    [string] $Mode = 'Docker',
    [string] $EnvFile = '',
    [string] $Namespace = 'cephasops',
    [switch] $Build
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Deploy-Docker {
    if ($Build) {
        docker compose -f (Join-Path $RepoRoot 'infra/docker/docker-compose.yml') build
        if ($LASTEXITCODE -ne 0) { throw 'Docker build failed' }
    }
    $envArgs = @()
    if ($EnvFile -and (Test-Path $EnvFile)) { $envArgs = @('--env-file', (Resolve-Path $EnvFile)) }
    & docker compose -f (Join-Path $RepoRoot 'infra/docker/docker-compose.yml') @envArgs up -d
    if ($LASTEXITCODE -ne 0) { throw 'Docker compose up failed' }
    Write-Host 'Deploy (Docker) complete. Check: docker compose -f infra/docker/docker-compose.yml ps'
}

function Deploy-Kubernetes {
    $k8sDir = Join-Path $RepoRoot 'infra/k8s'
    if (-not (Test-Path $k8sDir)) { throw 'infra/k8s not found' }
    & kubectl create namespace $Namespace 2>$null
    & kubectl apply -f $k8sDir -n $Namespace
    if ($LASTEXITCODE -ne 0) { throw 'kubectl apply failed' }
    Write-Host "Deploy (Kubernetes) complete. Check: kubectl get pods -n $Namespace"
}

switch ($Mode) {
    Docker     { Deploy-Docker }
    Kubernetes { Deploy-Kubernetes }
}
