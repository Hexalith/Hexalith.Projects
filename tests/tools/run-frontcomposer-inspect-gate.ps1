#!/usr/bin/env pwsh
# Inspect retained generator output from the shared Release/package build.

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
$generatedRoot = Join-Path $contractsRoot 'obj/Release'
if (-not (Test-Path -LiteralPath $generatedRoot -PathType Container)) {
    throw 'The shared Release build must retain FrontComposer generator output before inspection.'
}
$generatedOutputs = @(Get-ChildItem -LiteralPath $generatedRoot -Recurse -Filter '*.g.cs' -File |
    Where-Object { $_.FullName -match '[/\\]generated[/\\]HexalithFrontComposer[/\\]' })
if ($generatedOutputs.Count -eq 0) {
    throw 'The shared Release build retained no FrontComposer generator output.'
}
Push-Location $repositoryRoot
try {
    & dotnet tool run frontcomposer -- inspect --project $contractsProject --configuration Release --fail-on-warning
    if ($LASTEXITCODE -ne 0) {
        throw "FrontComposer generated-output inspection failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
Write-Host 'frontcomposer-inspect-gate: PASSED — retained Release generator output has no warnings.'
