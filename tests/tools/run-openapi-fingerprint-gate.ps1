#!/usr/bin/env pwsh
# OpenAPI fingerprint / compatibility gate (FS-2) for Hexalith.Projects.
#
# Behaviour today (Story 1.1): INPUT-GATED NO-OP, not an unconditional pass.
# The real gate fingerprints the OpenAPI contract spine and checks backward compatibility of the
# generated typed client. The spine (src/Hexalith.Projects.Contracts/openapi/*.yaml) does not exist
# until Story 1.3. Until then the gate detects "spine absent" and skips clean (exit 0).
#
# TODO(1.3): when src/Hexalith.Projects.Contracts/openapi/*.yaml lands, flip this to compute and
#            compare the fingerprint / run the compatibility check. Do NOT delete this gate before
#            then, and do NOT change the skip condition to an unconditional exit 0 — the skip is
#            input-presence based so the gate auto-activates the moment 1.3 adds the spine.

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptRoot '..' '..')

$openApiDir = Join-Path $repositoryRoot 'src/Hexalith.Projects.Contracts/openapi'
$specs = @()
if (Test-Path $openApiDir) {
    $specs = Get-ChildItem -Path $openApiDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue
}

if (-not $specs -or $specs.Count -eq 0) {
    Write-Host 'openapi-fingerprint-gate: SKIPPED (clean) — no src/Hexalith.Projects.Contracts/openapi/*.yaml spine present yet. Inputs land in Story 1.3.'
    exit 0
}

Write-Host "openapi-fingerprint-gate: spine detected ($($specs.Count) spec file(s)) — running real gate."
# TODO(1.3): replace the line below with the real fingerprint/compatibility check.
Write-Error 'openapi-fingerprint-gate: spine is present but the real gate is not wired yet (Story 1.3). Failing so this is never a silent false-green.'
exit 1
