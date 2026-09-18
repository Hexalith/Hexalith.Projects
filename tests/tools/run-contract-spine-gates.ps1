#!/usr/bin/env pwsh
# Filtered contract-spine test lane for Hexalith.Projects (Story 1.3).
#
# Runs the OpenAPI spine-shape tests (Contracts.Tests ...OpenApi) and the generation/provenance/
# fingerprint/hasher tests (Client.Tests ...ClientGenerationTests). These are the focused, fast
# contract-stability assertions the OpenAPI fingerprint gate depends on.

param(
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptRoot '..' '..')
Push-Location $repositoryRoot
try {
    $restoreArgs = @()
    if ($NoRestore) {
        $restoreArgs += '--no-restore'
    }

    $projects = @(
        @{
            Path     = 'tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj'
            Assembly = 'tests/Hexalith.Projects.Contracts.Tests/bin/Release/net10.0/Hexalith.Projects.Contracts.Tests.dll'
            Class    = 'Hexalith.Projects.Contracts.Tests.OpenApi.OpenApiContractSpineTests'
        },
        @{
            Path     = 'tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj'
            Assembly = 'tests/Hexalith.Projects.Client.Tests/bin/Release/net10.0/Hexalith.Projects.Client.Tests.dll'
            Class    = 'Hexalith.Projects.Client.Tests.ClientGenerationTests'
        }
    )

    $aggregateExitCode = 0
    foreach ($project in $projects) {
        dotnet build $project.Path --configuration Release @restoreArgs -warnaserror
        $projectExitCode = $LASTEXITCODE
        if ($projectExitCode -eq 0) {
            $testOutput = @(& dotnet $project.Assembly -class $project.Class 2>&1)
            $projectExitCode = $LASTEXITCODE
            $testOutput | ForEach-Object { Write-Host $_ }
            if ($projectExitCode -eq 0 -and ($testOutput -join "`n") -notmatch 'Total:\s*[1-9][0-9]*\b') {
                Write-Host "contract-spine-gate: FAILED — selector '$($project.Class)' executed zero tests." -ForegroundColor Red
                $projectExitCode = 1
            }
        }

        if ($projectExitCode -ne 0 -and $aggregateExitCode -eq 0) {
            $aggregateExitCode = $projectExitCode
        }
    }

    if ($aggregateExitCode -ne 0) {
        exit $aggregateExitCode
    }
}
finally {
    Pop-Location
}
