#!/usr/bin/env pwsh
# Build fresh Release generator inputs and inspect them with the packaged tool.

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..' '..')).Path
# Detect FrontComposer inputs: any [Projection]/[Command] attributes inside src/Hexalith.Projects.UI.Contracts,
# the non-packable project that owns the FrontComposer descriptors and runs the generator (AD-16).
$contractsRoot = Join-Path $repositoryRoot 'src/Hexalith.Projects.UI.Contracts'
$annotatedContracts = @()
if (Test-Path $contractsRoot) {
    $annotatedContracts = Get-ChildItem -Path $contractsRoot -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue |
        Where-Object { Select-String -Path $_.FullName -Pattern '\[\s*(Projection|Command)\b' -Quiet }
}

if (-not $annotatedContracts -or $annotatedContracts.Count -eq 0) {
    Write-Error "frontcomposer-inspect-gate: FAILED — no [Projection]/[Command] files found under $contractsRoot. src/Hexalith.Projects.UI.Contracts is the required annotated FrontComposer input; a missing or empty project would silently disable this gate."
    exit 1
}

Write-Host "frontcomposer-inspect-gate: inputs detected ($($annotatedContracts.Count) annotated contract file(s)) — running real gate."

$contractsProject = Join-Path $contractsRoot 'Hexalith.Projects.UI.Contracts.csproj'
$inspectionExitCode = 0
Push-Location $repositoryRoot
try {
    & dotnet tool run frontcomposer -- inspect --project $contractsProject --configuration Release --build --fail-on-warning
    $inspectionExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}
if ($inspectionExitCode -ne 0) {
    Write-Error -ErrorAction Continue "FrontComposer Release build or generated-output inspection failed with exit code $inspectionExitCode."
    exit $inspectionExitCode
}
Write-Host 'frontcomposer-inspect-gate: PASSED — fresh Release generator output has no warnings.'
