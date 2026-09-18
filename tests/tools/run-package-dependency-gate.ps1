#!/usr/bin/env pwsh

param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string] $Version = '91.92.93-ci.1',
    [string] $PackageDirectory = '',
    [switch] $SkipPack
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..' '..')).Path
$solutionPath = Join-Path $repositoryRoot 'Hexalith.Projects.CI.slnx'
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $PackageDirectory = Join-Path $repositoryRoot 'artifacts/ci-package-gate'
}
elseif (-not [System.IO.Path]::IsPathRooted($PackageDirectory)) {
    $PackageDirectory = Join-Path $repositoryRoot $PackageDirectory
}

function Invoke-PythonTool {
    param([string[]] $Arguments)

    Write-Host "package-dependency-gate: python3 $($Arguments -join ' ')"
    & python3 @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Package tool failed with exit code $LASTEXITCODE."
    }
}

function Invoke-Dotnet {
    param([string[]] $Arguments)

    Write-Host "package-dependency-gate: dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

# SourceTools is a build-time analyzer only: it must remain package-mode-only,
# private, and absent from every published dependency graph.
$contractsProjectPath = Join-Path $repositoryRoot 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj'
[xml] $contractsProject = Get-Content -Path $contractsProjectPath -Raw
$sourceToolsReferences = @($contractsProject.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Hexalith.FrontComposer.SourceTools' })
if ($sourceToolsReferences.Count -ne 1) {
    throw 'Projects.Contracts must declare exactly one package-mode Hexalith.FrontComposer.SourceTools analyzer reference.'
}

$sourceToolsReference = $sourceToolsReferences[0]
if ($sourceToolsReference.ParentNode.Condition -ne "'`$(UseHexalithProjectReferences)' != 'true'" -or
    $sourceToolsReference.PrivateAssets -ne 'all' -or
    'analyzers' -notin @($sourceToolsReference.IncludeAssets -split ';' | ForEach-Object { $_.Trim() })) {
    throw 'Hexalith.FrontComposer.SourceTools must remain package-mode-only, private, and analyzer-enabled.'
}

if (-not $SkipPack) {
    Invoke-Dotnet @(
        'restore',
        $solutionPath,
        '-p:Configuration=Release',
        '-p:UseHexalithProjectReferences=false',
        '/m:1',
        '/nr:false'
    )
    Invoke-Dotnet @(
        'build',
        $solutionPath,
        '--no-restore',
        '--configuration',
        'Release',
        '-warnaserror',
        '-p:UseHexalithProjectReferences=false',
        '/m:1',
        '/nr:false'
    )
    Invoke-PythonTool @(
        (Join-Path $repositoryRoot 'scripts/pack-release-packages.py'),
        $PackageDirectory,
        $Version
    )
}
elseif (-not (Test-Path $PackageDirectory -PathType Container)) {
    throw "Prepared package directory does not exist: $PackageDirectory"
}

$validationArguments = @(
    (Join-Path $repositoryRoot 'scripts/validate-nuget-packages.py'),
    $PackageDirectory,
    '--expected-version',
    $Version
)
if ($SkipPack) {
    # Semantic release creates these trusted candidates with direct dotnet-pack
    # commands. Normalize the internal lower-bound ranges before strict validation.
    $validationArguments += '--normalize-internal-ranges'
}

Invoke-PythonTool $validationArguments
Invoke-PythonTool @(
    (Join-Path $repositoryRoot 'scripts/validate-consumer-package-references.py'),
    $PackageDirectory
)

Write-Host "package-dependency-gate: PASSED — manifest inventory, exact metadata and dependency graph, provenance, compile assets, and five real API consumers validated."
exit 0
