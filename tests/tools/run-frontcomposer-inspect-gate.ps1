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
$cliProject = Join-Path $repositoryRoot 'references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj'

if (-not (Test-Path $cliProject)) {
    Write-Error "frontcomposer-inspect-gate: FAILED — repo-local FrontComposer CLI not found at $cliProject. Initialize only the root-declared Hexalith.FrontComposer submodule."
    exit 1
}

Write-Host 'frontcomposer-inspect-gate: building and inspecting deterministic Release output with the repo-local CLI.'
$targetFramework = (& dotnet msbuild $contractsProject -nologo -p:Configuration=Release -getProperty:TargetFramework | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $targetFramework -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
    throw 'The UI.Contracts target framework could not be resolved from MSBuild.'
}

# An earlier ordinary build can be up to date without retaining generator output.
# Emit a fresh set for the exact framework before inspecting it.
$generatedDirectory = Join-Path $contractsRoot "obj/Release/$targetFramework/generated/HexalithFrontComposer"
if (Test-Path -LiteralPath $generatedDirectory) {
    Remove-Item -LiteralPath $generatedDirectory -Recurse -Force
}
& dotnet build $contractsProject --configuration Release --framework $targetFramework --no-incremental -m:1 -warnaserror `
    '-p:EmitCompilerGeneratedFiles=true' `
    "-p:CompilerGeneratedFilesOutputPath=$generatedDirectory"
if ($LASTEXITCODE -ne 0) {
    throw 'The Release UI.Contracts build failed before generated-output inspection.'
}

& dotnet run --project $cliProject --configuration Release -- `
    inspect `
    --project $contractsProject `
    --configuration Release `
    --framework $targetFramework `
    --build `
    --fail-on-warning

$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Error "frontcomposer-inspect-gate: FAILED — the Release build or 'frontcomposer inspect --fail-on-warning' failed. Regenerate the FrontComposer surfaces and commit the updated output."
    exit $exitCode
}

Write-Host 'frontcomposer-inspect-gate: PASSED — frontcomposer inspect reported no warnings.'
exit 0
