#!/usr/bin/env pwsh
# Fast test lane for Hexalith.Projects (AC-4 green gate).
#
# Runs the pure Tier-1 domain-core tests and the Contracts placeholder tests. Tier-3
# (Hexalith.Projects.Integration.Tests) and the Playwright e2e workspace are intentionally
# EXCLUDED from this lane — they exist as compiling placeholders and run in dedicated lanes.

param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptRoot '..' '..')
Push-Location $repositoryRoot
try {
    $buildArgs = @()
    if ($NoBuild) {
        $buildArgs += '--no-build'
    }

    $projects = @(
        'tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj',
        'tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj'
    )

    $aggregateExitCode = 0
    foreach ($project in $projects) {
        dotnet test $project @buildArgs
        if ($LASTEXITCODE -ne 0 -and $aggregateExitCode -eq 0) {
            $aggregateExitCode = $LASTEXITCODE
        }
    }

    if ($aggregateExitCode -ne 0) {
        exit $aggregateExitCode
    }
}
finally {
    Pop-Location
}
