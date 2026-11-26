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
$ownedPackageDirectory = $null
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $ownedPackageDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('projects-package-' + [System.Guid]::NewGuid().ToString('N'))
    $PackageDirectory = $ownedPackageDirectory
}
elseif (-not [System.IO.Path]::IsPathRooted($PackageDirectory)) {
    $PackageDirectory = Join-Path $repositoryRoot $PackageDirectory
}

try {
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

# AD-16: the packable Contracts project must not reference FrontComposer in either
# reference mode, so CLI and MCP consumers never inherit Blazor through it.
$contractsProjectPath = Join-Path $repositoryRoot 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj'
[xml] $contractsProject = Get-Content -Path $contractsProjectPath -Raw
$frontComposerItems = @($contractsProject.SelectNodes('//*[local-name()="ItemGroup"]/*') | Where-Object {
    $_.GetAttribute('Include') -match 'Hexalith\.FrontComposer\.' -or $_.GetAttribute('Update') -match 'Hexalith\.FrontComposer\.'
})
if ($frontComposerItems.Count -ne 0) {
    throw "Projects.Contracts must not reference Hexalith.FrontComposer.*; UI-only references belong to Projects.UI.Contracts: $(@($frontComposerItems | ForEach-Object { $_.LocalName + ' ' + $_.GetAttribute('Include') + $_.GetAttribute('Update') }) -join ', ')"
}

# SourceTools is a build-time analyzer only: it must remain package-mode-only,
# private, and absent from every published dependency graph.
$uiContractsProjectPath = Join-Path $repositoryRoot 'src/Hexalith.Projects.UI.Contracts/Hexalith.Projects.UI.Contracts.csproj'
[xml] $uiContractsProject = Get-Content -Path $uiContractsProjectPath -Raw
$sourceToolsReferences = @($uiContractsProject.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Hexalith.FrontComposer.SourceTools' })
if ($sourceToolsReferences.Count -ne 1) {
    throw 'Projects.UI.Contracts must declare exactly one package-mode Hexalith.FrontComposer.SourceTools analyzer reference.'
}

$sourceToolsReference = $sourceToolsReferences[0]
if ($sourceToolsReference.ParentNode.Condition -ne "'`$(UseHexalithProjectReferences)' != 'true'" -or
    $sourceToolsReference.PrivateAssets -ne 'all' -or
    'analyzers' -notin @($sourceToolsReference.IncludeAssets -split ';' | ForEach-Object { $_.Trim() })) {
    throw 'Hexalith.FrontComposer.SourceTools must remain package-mode-only, private, and analyzer-enabled.'
}

if (-not $SkipPack) {
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

Write-Host "package-dependency-gate: PASSED — manifest inventory, exact metadata and dependency graph, provenance, compile assets, and six real API consumers validated."
}
finally {
    if ($ownedPackageDirectory -and (Test-Path -LiteralPath $ownedPackageDirectory)) {
        Remove-Item -LiteralPath $ownedPackageDirectory -Recurse -Force
    }
}
exit 0
