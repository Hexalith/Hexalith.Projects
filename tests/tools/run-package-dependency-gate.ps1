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
$rootCommons = Join-Path $repositoryRoot 'references/Hexalith.Commons'
$solutionPath = Join-Path $repositoryRoot 'Hexalith.Projects.CI.slnx'
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $PackageDirectory = Join-Path $repositoryRoot 'artifacts/ci-package-gate'
}
elseif (-not [System.IO.Path]::IsPathRooted($PackageDirectory)) {
    $PackageDirectory = Join-Path $repositoryRoot $PackageDirectory
}

$packageProjects = @(
    'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj',
    'src/Hexalith.Projects/Hexalith.Projects.csproj',
    'src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj',
    'src/Hexalith.Projects.Testing/Hexalith.Projects.Testing.csproj',
    'src/Hexalith.Projects.ServiceDefaults/Hexalith.Projects.ServiceDefaults.csproj'
)
$packageIds = @(
    'Hexalith.Projects.Contracts',
    'Hexalith.Projects',
    'Hexalith.Projects.Client',
    'Hexalith.Projects.Testing',
    'Hexalith.Projects.ServiceDefaults'
)
$expectedInternalDependencies = @{
    'Hexalith.Projects' = @('Hexalith.Projects.Contracts')
    'Hexalith.Projects.Client' = @('Hexalith.Projects.Contracts')
    'Hexalith.Projects.Testing' = @('Hexalith.Projects', 'Hexalith.Projects.Contracts')
}
$expectedExternalContractDependencies = [ordered]@{
    'Hexalith.EventStore.Contracts' = '3.100.1'
    'Hexalith.Conversations.Contracts' = '1.0.0'
    'Hexalith.FrontComposer.Contracts' = '4.2.0'
    'Hexalith.FrontComposer.Shell' = '4.2.0'
}

function Invoke-Dotnet {
    param([string[]] $Arguments)

    Write-Host "package-dependency-gate: dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Read-Nuspec {
    param([System.IO.FileInfo] $Package)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Package.FullName)
    try {
        $entry = $archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $entry) {
            throw "Package $($Package.Name) contains no nuspec."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            return [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Uses-SimulatedVersion {
    param([string] $Range)

    $trimmed = $Range.Trim()
    return $trimmed -eq $Version -or
        $trimmed -eq "[$Version]" -or
        $trimmed -match "^\[$([regex]::Escape($Version)),\s*\)$"
}

function Uses-CatalogVersion {
    param(
        [string] $Range,
        [string] $ExpectedVersion
    )

    $trimmed = $Range.Trim()
    return $trimmed -eq $ExpectedVersion -or
        $trimmed -eq "[$ExpectedVersion]" -or
        $trimmed -match "^\[$([regex]::Escape($ExpectedVersion)),\s*\)$"
}

$contractsProjectPath = Join-Path $repositoryRoot 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj'
[xml] $contractsProject = Get-Content -Path $contractsProjectPath -Raw
$sourceToolsReferences = @($contractsProject.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Hexalith.FrontComposer.SourceTools' })
if ($sourceToolsReferences.Count -ne 1) {
    throw "Projects.Contracts must declare exactly one package-mode Hexalith.FrontComposer.SourceTools analyzer reference."
}

$sourceToolsReference = $sourceToolsReferences[0]
if ($sourceToolsReference.ParentNode.Condition -ne "'`$(UseHexalithProjectReferences)' != 'true'" -or
    $sourceToolsReference.PrivateAssets -ne 'all' -or
    'analyzers' -notin @($sourceToolsReference.IncludeAssets -split ';' | ForEach-Object { $_.Trim() })) {
    throw "Hexalith.FrontComposer.SourceTools must remain package-mode-only, private, and analyzer-enabled."
}

if (-not $SkipPack) {
    if (Test-Path $PackageDirectory) {
        Remove-Item -Path $PackageDirectory -Recurse -Force
    }

    New-Item -Path $PackageDirectory -ItemType Directory -Force | Out-Null
    $commonProperty = "-p:HexalithCommonsRoot=$rootCommons"
    $versionProperty = "-p:HexalithProjectsPackageVersion=$Version"
    Invoke-Dotnet @('restore', $solutionPath, $commonProperty, $versionProperty)
    Invoke-Dotnet @('build', $solutionPath, '--no-restore', '--configuration', 'Release', '-warnaserror', $commonProperty, $versionProperty, '/m:1', '/nr:false')

    foreach ($project in $packageProjects) {
        Invoke-Dotnet @(
            'pack',
            (Join-Path $repositoryRoot $project),
            '--no-build',
            '--configuration',
            'Release',
            '--output',
            $PackageDirectory,
            $versionProperty,
            $commonProperty,
            '/m:1',
            '/nr:false'
        )
    }
}
elseif (-not (Test-Path $PackageDirectory)) {
    throw "Prepared package directory does not exist: $PackageDirectory"
}

$packages = @(Get-ChildItem -Path $PackageDirectory -Filter '*.nupkg' -File | Where-Object { $_.Name -notlike '*.symbols.*' })
if ($packages.Count -ne $packageIds.Count) {
    throw "Expected exactly $($packageIds.Count) release packages, found $($packages.Count) in $PackageDirectory."
}

$dependenciesByPackage = @{}
foreach ($package in $packages) {
    $nuspec = Read-Nuspec $package
    $idNode = $nuspec.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='id']")
    $versionNode = $nuspec.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='version']")
    if ($null -eq $idNode -or $null -eq $versionNode) {
        throw "Package $($package.Name) has incomplete nuspec metadata."
    }

    $id = $idNode.InnerText
    if ($id -notin $packageIds) {
        throw "Unexpected release package id '$id' in $($package.Name)."
    }

    if ($versionNode.InnerText -ne $Version) {
        throw "Package $id has version '$($versionNode.InnerText)' instead of simulated release version '$Version'."
    }

    $dependencies = @{}
    foreach ($dependency in $nuspec.SelectNodes("//*[local-name()='dependency']")) {
        $dependencies[$dependency.id] = $dependency.version
        if ($dependency.id -in $packageIds) {
            if (-not (Uses-SimulatedVersion $dependency.version)) {
                throw "Internal dependency $id -> $($dependency.id) has '$($dependency.version)' instead of the simulated Projects version '$Version'."
            }
        }
        elseif (Uses-SimulatedVersion $dependency.version) {
            throw "External dependency $id -> $($dependency.id) incorrectly inherited the Projects release version '$Version'."
        }
    }

    $dependenciesByPackage[$id] = $dependencies
}

foreach ($packageId in $expectedInternalDependencies.Keys) {
    foreach ($dependencyId in $expectedInternalDependencies[$packageId]) {
        if (-not $dependenciesByPackage[$packageId].ContainsKey($dependencyId)) {
            throw "Package $packageId does not declare required internal dependency $dependencyId."
        }
    }
}

foreach ($expectedDependency in $expectedExternalContractDependencies.GetEnumerator()) {
    $dependencyId = $expectedDependency.Key
    if (-not $dependenciesByPackage['Hexalith.Projects.Contracts'].ContainsKey($dependencyId)) {
        throw "Contracts package does not declare required external dependency $dependencyId."
    }

    $actualVersion = $dependenciesByPackage['Hexalith.Projects.Contracts'][$dependencyId]
    if (-not (Uses-CatalogVersion $actualVersion $expectedDependency.Value)) {
        throw "Contracts package dependency $dependencyId has '$actualVersion' instead of catalog version '$($expectedDependency.Value)'."
    }
}

if ($dependenciesByPackage['Hexalith.Projects.Contracts'].ContainsKey('Hexalith.FrontComposer.SourceTools')) {
    throw "The private SourceTools analyzer must not leak into the Projects.Contracts package dependency graph."
}

$consumerRoot = Join-Path ([System.IO.Path]::GetTempPath()) "hexalith-projects-package-gate-$([guid]::NewGuid().ToString('N'))"
New-Item -Path $consumerRoot -ItemType Directory -Force | Out-Null
try {
    $references = ($packageIds | ForEach-Object { "    <PackageReference Include=`"$_`" Version=`"[$Version]`" />" }) -join "`n"
    $consumerProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RestorePackagesPath>$consumerRoot/packages</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
$references
  </ItemGroup>
</Project>
"@
    $consumerProjectPath = Join-Path $consumerRoot 'PackageGate.Consumer.csproj'
    Set-Content -Path $consumerProjectPath -Value $consumerProject -Encoding utf8NoBOM
    try {
        Invoke-Dotnet @(
            'restore',
            $consumerProjectPath,
            '--no-cache',
            '--source',
            $PackageDirectory,
            '--source',
            'https://api.nuget.org/v3/index.json'
        )
    }
    catch {
        throw "Prepared Projects packages do not restore as package consumers. Keep publication blocked; an unavailable external dependency requires a separately authorized package publication/migration rather than a weaker gate. $($_.Exception.Message)"
    }
}
finally {
    Remove-Item -Path $consumerRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "package-dependency-gate: PASSED — five packages use '$Version' internally, preserve external versions, and restore together as packages."
exit 0
