#!/usr/bin/env pwsh

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..' '..')).Path
$workflowRoot = Join-Path $repositoryRoot '.github/workflows'
$ciPath = Join-Path $workflowRoot 'ci.yml'
$releasePath = Join-Path $workflowRoot 'release.yml'
$solutionPath = Join-Path $repositoryRoot 'Hexalith.Projects.CI.slnx'
$releaseConfigPath = Join-Path $repositoryRoot 'release.config.cjs'
$frontComposerGatePath = Join-Path $scriptRoot 'run-frontcomposer-inspect-gate.ps1'
$openApiGatePath = Join-Path $scriptRoot 'run-openapi-fingerprint-gate.ps1'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-Match {
    param(
        [string] $Text,
        [string] $Pattern,
        [string] $Message
    )

    if (-not [regex]::IsMatch($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        $failures.Add($Message)
    }
}

function Forbid-Match {
    param(
        [string] $Text,
        [string] $Pattern,
        [string] $Message
    )

    if ([regex]::IsMatch($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        $failures.Add($Message)
    }
}

if (-not (Test-Path $ciPath)) {
    throw "CI workflow not found at $ciPath."
}

$ci = Get-Content -Path $ciPath -Raw
$solution = Get-Content -Path $solutionPath -Raw
$releaseConfig = Get-Content -Path $releaseConfigPath -Raw
$frontComposerGate = Get-Content -Path $frontComposerGatePath -Raw
$openApiGate = Get-Content -Path $openApiGatePath -Raw
$workflowFiles = @(
    Get-ChildItem -Path $workflowRoot -File |
        Where-Object { $_.Extension -in @('.yml', '.yaml') }
)
$allWorkflows = ($workflowFiles | ForEach-Object { Get-Content -Path $_.FullName -Raw }) -join "`n"

if (Test-Path $releasePath) {
    $failures.Add('release.yml must remain retired; release belongs behind the tested SHA in ci.yml.')
}

Require-Match $ci '^\s*push:\s*$' 'CI must run on pushes.'
Require-Match $ci '^\s*pull_request:\s*$' 'CI must run on pull requests.'
Require-Match $ci '^\s*schedule:\s*$' 'CI must include a scheduled lane.'
Require-Match $ci "dapr-version:\s*'1\.18(?:\.0)?'" 'CI and release must use the supported Dapr 1.18 baseline.'
Require-Match $ci 'HexalithCommonsRoot=\$\{\{ github\.workspace \}\}/references/Hexalith\.Commons' 'Reusable CI and release must receive the root Commons path as a global MSBuild property.'
Require-Match $ci '^\s*integration-test-projects:\s*\|' 'The reusable CI workflow must run Integration.Tests separately.'
Require-Match $ci '^\s*release:\s*$' 'The tested CI workflow must own the release job.'
Require-Match $ci "github\.event_name == 'push' && github\.ref == 'refs/heads/main'" 'Release must be restricted to the tested main push event.'
Require-Match $ci '^\s*permissions:\s*$' 'The release job must declare explicit write permissions.'
Require-Match $ci '^\s*contents:\s*write\s*$' 'Release must scope contents: write at the job level.'
Require-Match $ci '^\s*cancel-in-progress:\s*\$\{\{ github\.event_name != ''push'' \|\| github\.ref != ''refs/heads/main'' \}\}\s*$' 'Main push/release workflows must never be cancelled by a newer run.'
Require-Match $ci '^\s*package-gates:\s*$' 'CI must run the package dependency/restore gate.'
Require-Match $ci '^\s*e2e:\s*$' 'CI must include the scheduled E2E job.'
Require-Match $ci "if:\s*\$\{\{ github\.event_name == 'schedule' \}\}" 'E2E must be limited to the scheduled lane.'
Require-Match $ci 'npm --prefix tests/e2e ci --ignore-scripts' 'E2E must use the lockfile with lifecycle scripts disabled.'
Require-Match $ci 'npm --prefix tests/e2e run install:browsers' 'E2E browser installation must be explicit.'
Require-Match $ci 'npm --prefix tests/e2e run typecheck' 'E2E must typecheck before Playwright.'
Require-Match $ci 'npm --prefix tests/e2e test -- --workers=1' 'E2E must run deterministically with one worker.'
Require-Match $ci '^\s*if:\s*failure\(\)\s*$' 'E2E failure evidence must be uploaded on failure.'

$testProjects = @(
    'Hexalith.Projects.Contracts.Tests',
    'Hexalith.Projects.Client.Tests',
    'Hexalith.Projects.Tests',
    'Hexalith.Projects.Server.Tests',
    'Hexalith.Projects.UI.Tests',
    'Hexalith.Projects.Mcp.Tests',
    'Hexalith.Projects.Cli.Tests',
    'Hexalith.Projects.Integration.Tests'
)
foreach ($testProject in $testProjects) {
    Require-Match $ci ([regex]::Escape("tests/$testProject")) "CI does not list $testProject."
    Require-Match $solution ([regex]::Escape("tests/$testProject/$testProject.csproj")) "CI solution does not include $testProject."
}

Require-Match $solution 'src/Hexalith\.Projects\.AppHost/Hexalith\.Projects\.AppHost\.csproj' 'CI solution must include AppHost so Integration.Tests has complete Release output.'
Require-Match $frontComposerGate '--configuration Release' 'FrontComposer gate must inspect Release output.'
Require-Match $frontComposerGate '--build' 'FrontComposer gate must build its own inspection inputs.'
Require-Match $openApiGate '--configuration Release' 'OpenAPI gate must build its compatibility owner in Release.'
Require-Match $openApiGate '-warnaserror' 'OpenAPI gate must fail on build warnings.'
Require-Match $releaseConfig 'run-package-dependency-gate\.ps1' 'Semantic release must validate prepared packages before publication.'

Forbid-Match $allWorkflows '^\s*submodules:\s*(true|recursive)\s*$' 'Recursive or implicit recursive submodule checkout is forbidden.'
Forbid-Match $allWorkflows 'git\s+[^\r\n]*submodule\s+[^\r\n]*--recursive' 'Recursive submodule commands are forbidden.'
Forbid-Match $allWorkflows 'npm\s+(?:--prefix\s+\S+\s+)?install(?:\s|$)' 'Workflow dependency installation must not use npm install.'

foreach ($workflowFile in $workflowFiles) {
    $lineNumber = 0
    foreach ($line in Get-Content -Path $workflowFile.FullName) {
        $lineNumber++
        if ($line -notmatch '^\s*uses:\s*([^\s#]+)') {
            continue
        }

        $reference = $Matches[1]
        if ($reference.StartsWith('./', [System.StringComparison]::Ordinal)) {
            continue
        }

        if ($reference -match '^Hexalith/Hexalith\.Builds/.+@main$') {
            continue
        }

        if ($reference -notmatch '@[0-9a-fA-F]{40}$') {
            $failures.Add("$($workflowFile.Name):$lineNumber uses a mutable or unreviewed action reference: $reference")
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error "ci-workflow-gate: $failure"
    }

    exit 1
}

Write-Host "ci-workflow-gate: PASSED — $($workflowFiles.Count) workflow file(s), immutable third-party refs, exact-SHA release routing, safe E2E, and root-only submodule policy validated."
exit 0
