#!/usr/bin/env pwsh
# OpenAPI fingerprint / compatibility gate (FS-2) for Hexalith.Projects.
#
# Behaviour (Story 1.3 onward): REAL gate. The OpenAPI Contract Spine
# (src/Hexalith.Projects.Contracts/openapi/*.yaml) and the generated typed client now exist, so
# this gate fingerprints (spine, nswag.json, generated helpers) and fails on incompatible drift.
#
# The real check is executed by the Client.Tests contract-stability assertions, which call
# HexalithProjectsGeneratedArtifacts.VerifyCurrentDetailed(...) against the checked-in artifacts and
# also prove the fingerprint DETECTS drift (negative test). Running that filtered test set is the
# fingerprint/compatibility check: if the checked-in generated output ever drifts from the spine
# inputs, VerifyCurrentDetailed returns false and the test (hence this gate) fails.
#
# The input-presence skip branch below is retained for safety (it never triggers once the spine
# exists) but is NO LONGER a no-op false-green: when the spine is present the gate runs the real
# verification.

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptRoot '..' '..')

$openApiDir = Join-Path $repositoryRoot 'src/Hexalith.Projects.Contracts/openapi'
$specs = @()
if (Test-Path $openApiDir) {
    $specs = Get-ChildItem -Path $openApiDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue
}

if (-not $specs -or $specs.Count -eq 0) {
    Write-Host 'openapi-fingerprint-gate: SKIPPED (clean) — no src/Hexalith.Projects.Contracts/openapi/*.yaml spine present yet.'
    exit 0
}

Write-Host "openapi-fingerprint-gate: spine detected ($($specs.Count) spec file(s)) — running real fingerprint/compatibility check."

$clientTests = Join-Path $repositoryRoot 'tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj'
if (-not (Test-Path $clientTests)) {
    Write-Error "openapi-fingerprint-gate: Client.Tests project not found at $clientTests — cannot run the fingerprint check."
    exit 1
}

# The ClientGenerationTests class carries the spine-driven generation, provenance, and drift-detection
# assertions (VerifyCurrentDetailed current + IsCurrent drift detection). Running it IS the gate.
Push-Location $repositoryRoot
try {
    dotnet test $clientTests --filter 'FullyQualifiedName~Hexalith.Projects.Client.Tests.ClientGenerationTests'
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exitCode -ne 0) {
    Write-Error "openapi-fingerprint-gate: FAILED — generated client/helpers drifted from the Contract Spine inputs (fingerprint mismatch). Regenerate via 'dotnet build src/Hexalith.Projects.Client' and commit the updated Generated/*.g.cs."
    exit $exitCode
}

Write-Host 'openapi-fingerprint-gate: PASSED — generated artifacts match the Contract Spine fingerprint.'
exit 0
