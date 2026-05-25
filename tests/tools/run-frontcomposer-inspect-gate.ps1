#!/usr/bin/env pwsh
# FrontComposer staleness gate (FS-7) for Hexalith.Projects.
#
# Behaviour today (Story 1.1): INPUT-GATED NO-OP, not an unconditional pass.
# The real gate runs `frontcomposer inspect --fail-on-warning` over the module's
# FrontComposer-annotated contracts. Those inputs ([Projection]/[Command]-annotated
# contracts and the generated UI wiring) do not exist until Story 1.3 lands the
# OpenAPI/contract spine. Until then the gate detects "inputs absent" and skips clean (exit 0).
#
# TODO(1.3): when the contract spine + FrontComposer annotations land, flip this to run the real
#            `frontcomposer inspect --fail-on-warning`. Do NOT delete this gate before then, and do
#            NOT change the skip condition to an unconditional exit 0 — the skip is input-presence
#            based so the gate auto-activates the moment 1.3 adds inputs, with no CI edit required.

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
    Write-Host 'frontcomposer-inspect-gate: SKIPPED (clean) — no [Projection]/[Command] contracts present yet. Inputs land in Story 1.3.'
    exit 0
}

Write-Host "frontcomposer-inspect-gate: inputs detected ($($annotatedContracts.Count) annotated contract file(s)) — running real gate."
# TODO(1.3): replace the line below with the real invocation, e.g.:
#   dotnet frontcomposer inspect --fail-on-warning --project src/Hexalith.Projects.Contracts
Write-Error 'frontcomposer-inspect-gate: inputs are present but the real gate is not wired yet (Story 1.3). Failing so this is never a silent false-green.'
exit 1
