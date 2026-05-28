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
            Path   = 'tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj'
            Filter = 'FullyQualifiedName~Hexalith.Projects.Contracts.Tests.OpenApi'
        },
        @{
            Path   = 'tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj'
            Filter = 'FullyQualifiedName~Hexalith.Projects.Client.Tests.ClientGenerationTests'
        }
    )

    $aggregateExitCode = 0
    foreach ($project in $projects) {
        dotnet test $project.Path @restoreArgs --filter $project.Filter
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
