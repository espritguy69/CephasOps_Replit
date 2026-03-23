# Generate tenant-safety architecture diagram and intentional-exceptions block from manifest.
# Run from repo root: ./tools/architecture/generate_tenant_safety_diagram.ps1
# Replaces only the content between <!-- BEGIN GENERATED: tenant_safety_diagram --> and <!-- END GENERATED: tenant_safety_diagram --> in SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md.
# See backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md and tools/architecture/tenant_safety_architecture.json

param(
    [string]$RepoRoot = "",
    [string]$ManifestPath = "",
    [string]$DocPath = ""
)

$ErrorActionPreference = "Stop"

$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = if ($RepoRoot) { $RepoRoot } else { (Resolve-Path (Join-Path $ScriptDir "..\..")).Path }
$ManifestPath = if ($ManifestPath) { $ManifestPath } else { Join-Path $RepoRoot "tools/architecture/tenant_safety_architecture.json" }
$DocPath = if ($DocPath) { $DocPath } else { Join-Path $RepoRoot "backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md" }

$BeginMarker = "<!-- BEGIN GENERATED: tenant_safety_diagram -->"
$EndMarker = "<!-- END GENERATED: tenant_safety_diagram -->"

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Manifest not found: $ManifestPath"
    exit 2
}
if (-not (Test-Path $DocPath)) {
    Write-Error "Doc not found: $DocPath"
    exit 2
}

$manifest = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json

# ----- Build Mermaid flowchart -----
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("flowchart TB")
[void]$sb.AppendLine("    subgraph EXT[`"External inputs`"]")
[void]$sb.AppendLine("        direction LR")
foreach ($in in $manifest.external_inputs) {
    [void]$sb.AppendLine("        $($in.id)[`"$($in.label)`"]")
}
[void]$sb.AppendLine("    end")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    subgraph RES[`"$($manifest.identity_resolution.subgraph_label)`"]")
foreach ($n in $manifest.identity_resolution.nodes) {
    [void]$sb.AppendLine("        $($n.id)[`"$($n.label)`"]")
}
foreach ($e in $manifest.identity_resolution.edges) {
    [void]$sb.AppendLine("        $e")
}
[void]$sb.AppendLine("    end")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    subgraph EXEC[`"$($manifest.execution.subgraph_label)`"]")
[void]$sb.AppendLine("        $($manifest.execution.boundary.id)[`"$($manifest.execution.boundary.label)`"]")
foreach ($m in $manifest.execution.modes) {
    [void]$sb.AppendLine("        $($m.id)[`"$($m.label)`"]")
    [void]$sb.AppendLine("        $($manifest.execution.boundary.id) --> $($m.id)")
}
[void]$sb.AppendLine("    end")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    subgraph GUARD[`"$($manifest.guards.subgraph_label)`"]")
foreach ($g in $manifest.guards.items) {
    [void]$sb.AppendLine("        $($g.id)[`"$($g.label)`"]")
}
[void]$sb.AppendLine("    end")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    subgraph PERS[`"$($manifest.persistence.subgraph_label)`"]")
foreach ($n in $manifest.persistence.nodes) {
    [void]$sb.AppendLine("        $($n.id)[`"$($n.label)`"]")
}
foreach ($e in $manifest.persistence.edges) {
    [void]$sb.AppendLine("        $e")
}
[void]$sb.AppendLine("    end")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    subgraph DATA[`"$($manifest.data_layer.subgraph_label)`"]")
foreach ($n in $manifest.data_layer.nodes) {
    [void]$sb.AppendLine("        $($n.id)[`"$($n.label)`"]")
}
[void]$sb.AppendLine("    end")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    subgraph OBS[`"$($manifest.observability.subgraph_label)`"]")
foreach ($n in $manifest.observability.nodes) {
    [void]$sb.AppendLine("        $($n.id)[`"$($n.label)`"]")
}
[void]$sb.AppendLine("    end")
[void]$sb.AppendLine("")
foreach ($e in $manifest.flow_edges) {
    [void]$sb.AppendLine("    $e")
}
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    subgraph EXCEPTIONS[`"Intentional exceptions (not normal runtime)`"]")
[void]$sb.AppendLine("        direction TB")
$exNodes = @()
for ($i = 0; $i -lt $manifest.intentional_exceptions.Count; $i++) {
    $ex = $manifest.intentional_exceptions[$i]
    $nodeId = if ($i -eq 0) { "SEED" } else { "FACTORY" }
    $shortLabel = $ex.name -replace "\.CreateDbContext$", ".CreateDbContext - design-time EF only"
    if ($shortLabel -eq $ex.name -and $ex.name -eq "DatabaseSeeder") {
        $shortLabel = "DatabaseSeeder - bootstrap / setup only"
    } elseif ($ex.name -match "ApplicationDbContextFactory") {
        $shortLabel = "ApplicationDbContextFactory.CreateDbContext - design-time EF only"
    } else {
        $shortLabel = $ex.name + " - " + ($ex.purpose -replace '^[^.]*\. ', '')
        if ($shortLabel.Length -gt 55) { $shortLabel = $ex.name + " - (see table)" }
    }
    [void]$sb.AppendLine("        $nodeId[`"$shortLabel`"]")
}
[void]$sb.AppendLine("    end")

$mermaidBlock = $sb.ToString().TrimEnd()

# ----- Build intentional exceptions table -----
$tableLines = @()
$tableLines += ""
$tableLines += "## Intentional exceptions"
$tableLines += ""
$tableLines += "Two places still use manual **EnterPlatformBypass** (and Exit where applicable) by design. They are **not** normal runtime flows:"
$tableLines += ""
$tableLines += "| Exception | Purpose |"
$tableLines += "|-----------|---------|"
foreach ($ex in $manifest.intentional_exceptions) {
    $tableLines += "| **$($ex.name)** | $($ex.purpose) |"
}
$tableLines += ""
$tableLines += $manifest.exceptions_footer
$tableLines += ""

$exceptionsBlock = $tableLines -join "`n"

# ----- Assemble generated content (between markers) -----
# Use 6 literal backticks for Mermaid fence (here-string: `` = one backtick)
$fence = "``````"
$generatedContent = @"
*The diagram and exceptions table below are generated from ``tools/architecture/tenant_safety_architecture.json``. Run ``./tools/architecture/generate_tenant_safety_diagram.ps1`` to refresh.*

$fence`mermaid
$mermaidBlock
$fence

$exceptionsBlock
"@

# ----- Replace content in doc -----
$docContent = Get-Content -Path $DocPath -Raw
$beginIdx = $docContent.IndexOf($BeginMarker)
$endIdx = $docContent.IndexOf($EndMarker)
if ($beginIdx -lt 0) {
    Write-Error "Begin marker not found in doc. Add: $BeginMarker"
    exit 2
}
if ($endIdx -lt 0) {
    Write-Error "End marker not found in doc. Add: $EndMarker"
    exit 2
}
if ($endIdx -le $beginIdx) {
    Write-Error "End marker must appear after Begin marker."
    exit 2
}

$before = $docContent.Substring(0, $beginIdx + $BeginMarker.Length)
$after = $docContent.Substring($endIdx)
$newDoc = $before + "`n`n" + $generatedContent + "`n`n" + $after

# Normalize Mermaid fence to three backticks for Markdown
$newDoc = $newDoc -replace '``````mermaid', '```mermaid'
$newDoc = $newDoc -replace '``````', '```'

Set-Content -Path $DocPath -Value $newDoc -NoNewline
Write-Host "Generated diagram and exceptions block in $DocPath"
exit 0
