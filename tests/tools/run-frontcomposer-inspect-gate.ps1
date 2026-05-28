#!/usr/bin/env pwsh
# FrontComposer staleness gate (FS-7) for Hexalith.Projects.
#
# Behaviour (Story 1.3 onward): INPUT-PRESENCE-GATED REAL gate. The real gate runs
# `frontcomposer inspect --fail-on-warning` over the module's FrontComposer-annotated contracts
# ([Projection]/[Command] attributes). Story 1.3 authors the OpenAPI Contract Spine (AR-15), NOT the
# FrontComposer [Projection]/[Command] annotations (AR-17) — those land with the projection/surface
# stories (Epic 5 / FrontComposer console). So this gate legitimately stays in skip-clean mode here:
# the detection below finds no [Projection]/[Command] contracts, which is CORRECT for this story.
#
# The decision is documented in Story 1.3 Dev Notes: input-presence-gated, no annotations added here.
# The false-green `exit 1` placeholder in the "annotations present" branch has been replaced with the
# REAL `frontcomposer inspect --fail-on-warning` invocation so the gate auto-activates for real the
# moment a story adds seed [Command]/[Projection] annotations, with no CI edit required.

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptRoot '..' '..')

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

# Real invocation: fail the gate on any FrontComposer regeneration/staleness warning.
& dotnet frontcomposer inspect --fail-on-warning --project (Join-Path $contractsRoot 'Hexalith.Projects.Contracts.csproj')
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Error "frontcomposer-inspect-gate: FAILED — 'frontcomposer inspect --fail-on-warning' reported regeneration/staleness warnings. Regenerate the FrontComposer surfaces and commit the updated output."
    exit $exitCode
}

Write-Host 'frontcomposer-inspect-gate: PASSED — frontcomposer inspect reported no warnings.'
exit 0
