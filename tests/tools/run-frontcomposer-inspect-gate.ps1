#!/usr/bin/env pwsh
# FrontComposer staleness gate (FS-7) for Hexalith.Projects. The gate builds and
# inspects Release output so it does not depend on Debug artifacts from an earlier step.

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..' '..')).Path
$rootCommons = Join-Path $repositoryRoot 'references/Hexalith.Commons'
if ([string]::IsNullOrWhiteSpace($env:HexalithCommonsRoot)) {
    $env:HexalithCommonsRoot = $rootCommons
}

# Detect FrontComposer inputs: any [Projection]/[Command] attributes inside src/Hexalith.Projects.Contracts.
$contractsRoot = Join-Path $repositoryRoot 'src/Hexalith.Projects.Contracts'
$annotatedContracts = @()
if (Test-Path $contractsRoot) {
    $annotatedContracts = Get-ChildItem -Path $contractsRoot -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue |
        Where-Object { Select-String -Path $_.FullName -Pattern '\[\s*(Projection|Command)\b' -Quiet }
}

if (-not $annotatedContracts -or $annotatedContracts.Count -eq 0) {
    Write-Host 'frontcomposer-inspect-gate: SKIPPED (clean) — no [Projection]/[Command] contracts present yet. Story 1.3 authors the OpenAPI spine, not FrontComposer annotations (those land in Epic 5).'
    exit 0
}

Write-Host "frontcomposer-inspect-gate: inputs detected ($($annotatedContracts.Count) annotated contract file(s)) — running real gate."

$contractsProject = Join-Path $contractsRoot 'Hexalith.Projects.Contracts.csproj'
$cliProject = Join-Path $repositoryRoot 'references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj'

if (-not (Test-Path $cliProject)) {
    Write-Error "frontcomposer-inspect-gate: FAILED — repo-local FrontComposer CLI not found at $cliProject. Initialize only the root-declared Hexalith.FrontComposer submodule."
    exit 1
}

Write-Host 'frontcomposer-inspect-gate: building and inspecting deterministic Release output with the repo-local CLI.'
& dotnet run --project $cliProject --configuration Release -- `
    inspect `
    --project $contractsProject `
    --configuration Release `
    --build `
    --fail-on-warning

$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Error "frontcomposer-inspect-gate: FAILED — the Release build or 'frontcomposer inspect --fail-on-warning' failed. Regenerate the FrontComposer surfaces and commit the updated output."
    exit $exitCode
}

Write-Host 'frontcomposer-inspect-gate: PASSED — frontcomposer inspect reported no warnings.'
exit 0
