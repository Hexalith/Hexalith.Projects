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
$managedE2EPath = Join-Path $repositoryRoot 'tests/e2e/run-live-apphost.sh'
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
$packageManifestPath = Join-Path $repositoryRoot 'tools/release-packages.json'
$releaseConfig = Get-Content -Path $releaseConfigPath -Raw
$frontComposerGate = Get-Content -Path $frontComposerGatePath -Raw
$openApiGate = Get-Content -Path $openApiGatePath -Raw
if (-not (Test-Path $managedE2EPath)) {
    $failures.Add('The managed AppHost E2E runner must exist.')
    $managedE2E = ''
}
else {
    $managedE2E = Get-Content -Path $managedE2EPath -Raw
}
$workflowFiles = @(
    Get-ChildItem -Path $workflowRoot -File |
        Where-Object { $_.Extension -in @('.yml', '.yaml') }
)
$allWorkflows = ($workflowFiles | ForEach-Object { Get-Content -Path $_.FullName -Raw }) -join "`n"
$workflowGatesMatch = [regex]::Match(
    $ci,
    '(?ms)^  workflow-gates:\r?\n.*?(?=^  [A-Za-z0-9_-]+:\r?$|\z)'
)

if (-not $workflowGatesMatch.Success) {
    $failures.Add('CI must define the workflow-gates job.')
}
else {
    $workflowGates = $workflowGatesMatch.Value
    $rendererStepMatches = [regex]::Matches(
        $workflowGates,
        '(?ms)^      - name: Validate shared skill renderer\r?\n.*?(?=^      - |\z)'
    )
    if ($rendererStepMatches.Count -ne 1) {
        $failures.Add('workflow-gates must contain exactly one shared skill renderer step.')
    }
    else {
        $rendererStep = ($rendererStepMatches[0].Value -replace "`r`n", "`n").TrimEnd([char[]] "`r`n")
        $expectedRendererStep = @(
            '      - name: Validate shared skill renderer'
            '        env:'
            '          PYTHONDONTWRITEBYTECODE: ''1'''
            '        run: python3 -m unittest tests/tools/test_render_skill.py -v'
        ) -join "`n"
        if ($rendererStep -cne $expectedRendererStep) {
            $failures.Add('The shared skill renderer must be an exact blocking name/env/run step with bytecode disabled.')
        }
    }

    $ownershipStepMatches = [regex]::Matches(
        $workflowGates,
        '(?ms)^      - name: Validate Build Auto workspace ownership\r?\n.*?(?=^      - |\z)'
    )
    if ($ownershipStepMatches.Count -ne 1) {
        $failures.Add('workflow-gates must contain exactly one Build Auto workspace ownership step.')
    }
    else {
        $ownershipStep = ($ownershipStepMatches[0].Value -replace "`r`n", "`n").TrimEnd([char[]] "`r`n")
        $expectedOwnershipStep = @(
            '      - name: Validate Build Auto workspace ownership'
            '        env:'
            '          PYTHONDONTWRITEBYTECODE: ''1'''
            '        run: python3 .agents/skills/bmad-build-auto/scripts/tests/test_workspace_ownership.py'
        ) -join "`n"
        if ($ownershipStep -cne $expectedOwnershipStep) {
            $failures.Add('The Build Auto workspace ownership fixture must be an exact blocking name/env/run step with bytecode disabled.')
        }
    }
}

# Release is an operator-dispatched workflow behind a verified green main SHA,
# matching the Hexalith.EventStore / Hexalith.Tenants module standard.
if (-not (Test-Path $releasePath)) {
    $failures.Add('release.yml must exist; release is an operator-dispatched workflow in every Hexalith module.')
    $release = ''
}
else {
    $release = Get-Content -Path $releasePath -Raw
}

foreach ($sharedWorkflow in @('codeql.yml', 'commitlint.yml', 'dependency-review.yml')) {
    if (-not (Test-Path (Join-Path $workflowRoot $sharedWorkflow))) {
        $failures.Add("$sharedWorkflow must exist; every Hexalith module runs the shared $($sharedWorkflow -replace '\.yml$', '') lane.")
    }
}

Require-Match $release '^on:\s*\r?\n\s*workflow_dispatch:\s*$' 'Release must be dispatch-only so publication stays an explicit operator action.'
Forbid-Match $release '^\s*(push|pull_request|schedule):\s*$' 'Release must never be triggered by push, pull_request, or schedule.'
Require-Match $release '^\s*verify-source:\s*$' 'Release must run the unprotected exact-source preflight before the protected environment.'
Require-Match $release 'No successful push CI run exists for the exact current main SHA' 'The preflight must prove a successful push CI run for the exact dispatched SHA.'
Require-Match $release 'The dispatched source is no longer the live main tip' 'The preflight must prove the dispatch selected the live main tip.'
Require-Match $release '^\s*needs:\s*verify-source\s*$' 'The release job must depend on the exact-source preflight.'
Require-Match $release '^\s*environment-name:\s*production\s*$' 'Release must enter the protected production environment.'
Require-Match $release "dapr-version:\s*'1\.18(?:\.0)?'" 'CI and release must use the supported Dapr 1.18 baseline.'
Require-Match $release '^\s*cancel-in-progress:\s*false\s*$' 'An in-flight release must never be cancelled by a newer dispatch.'

# GitHub validates the maximum permissions of EVERY job in a called workflow at
# workflow startup, including the skipped governed-release job. Granting less than
# that superset fails the whole run with `startup_failure` before a single job runs.
foreach ($scope in @('actions: read', 'attestations: write', 'contents: write', 'id-token: write', 'issues: write', 'pull-requests: write')) {
    Require-Match $release ("^\s*" + [regex]::Escape($scope) + "\s*$") "The release job must grant '$scope' to satisfy domain-release.yml startup permission validation."
}

# The release callee is pinned: an unreviewed drift on Builds@main is exactly what
# broke workflow startup once already.
$releaseCallMatch = [regex]::Match($release, 'uses:\s*Hexalith/Hexalith\.Builds/\.github/workflows/domain-release\.yml@([0-9a-f]{40})\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
if (-not $releaseCallMatch.Success) {
    $failures.Add('Release must call domain-release.yml pinned to an exact 40-character commit SHA.')
}
else {
    $pinnedSha = $releaseCallMatch.Groups[1].Value
    Require-Match $release ('^\s*builds-execution-sha:\s*' + $pinnedSha + '\s*$') 'builds-execution-sha must match the pinned domain-release.yml SHA.'
}

# The declared package count is the fail-closed gate: it must agree with both the
# manifest it is checked against and the count semantic-release publishes.
if (-not (Test-Path $packageManifestPath)) {
    $failures.Add('tools/release-packages.json must exist; the release publication identity is frozen from it.')
}
else {
    $manifestCount = @((Get-Content -Path $packageManifestPath -Raw | ConvertFrom-Json).packages).Count
    Require-Match $release ('^\s*expected-package-count:\s*' + $manifestCount + '\s*$') "expected-package-count must equal the $manifestCount package(s) declared in tools/release-packages.json."
    Require-Match $releaseConfig ('-eq ' + $manifestCount + "'") "release.config.cjs must publish exactly $manifestCount package(s)."
}

Require-Match $ci '^\s*push:\s*$' 'CI must run on pushes.'
Require-Match $ci '^\s*pull_request:\s*$' 'CI must run on pull requests.'
Require-Match $ci '^\s*schedule:\s*$' 'CI must include a scheduled lane.'
Require-Match $ci "dapr-version:\s*'1\.18(?:\.0)?'" 'CI must use the supported Dapr 1.18 baseline.'
Require-Match $ci '^\s*integration-test-projects:\s*\|' 'The reusable CI workflow must run Integration.Tests separately.'

# The reusable callees run `dotnet restore "$SOLUTION"`: the input is one quoted
# argument, so an embedded MSBuild switch becomes part of the project path (MSB1009).
# `github.workspace` is also empty when a reusable-workflow `with:` block is evaluated,
# so any path built from it silently resolves to the filesystem root. Commons is
# auto-detected by Directory.Build.props and needs no override here.
foreach ($caller in @(@{ Name = 'ci.yml'; Text = $ci }, @{ Name = 'release.yml'; Text = $release })) {
    $solutionInputs = [regex]::Matches($caller.Text, '(?m)^\s*solution:\s*(.+?)\s*$')
    foreach ($solutionInput in $solutionInputs) {
        $value = $solutionInput.Groups[1].Value
        if ($value -match '^>-|^\|') {
            $failures.Add("$($caller.Name) passes a multi-line solution input; it must be a bare solution path.")
        }
        elseif ($value -notmatch '^Hexalith\.Projects\.CI\.slnx$') {
            $failures.Add("$($caller.Name) solution input must be the bare 'Hexalith.Projects.CI.slnx', not '$value'.")
        }
    }
    if ($solutionInputs.Count -eq 0) {
        $failures.Add("$($caller.Name) must pass a solution input to the reusable workflow.")
    }
}

Forbid-Match ($ci + "`n" + $release) '^\s*solution:.*-p:' 'A solution input must not embed MSBuild switches; the callee quotes it as a single argument.'
Forbid-Match ($ci + "`n" + $release) '^\s*(solution|.*-p:.*):.*\$\{\{ github\.workspace \}\}' 'github.workspace is empty when a reusable-workflow `with:` is evaluated; never build a path from it there.'
Require-Match $ci '^\s*cancel-in-progress:\s*\$\{\{ github\.event_name != ''push'' \|\| github\.ref != ''refs/heads/main'' \}\}\s*$' 'Main push/release workflows must never be cancelled by a newer run.'
Require-Match $ci '^\s*package-gates:\s*$' 'CI must run the package dependency/restore gate.'
Require-Match $ci '^\s*e2e:\s*$' 'CI must include the scheduled E2E job.'
Require-Match $ci "if:\s*\$\{\{ github\.event_name == 'schedule' \}\}" 'E2E must be limited to the scheduled lane.'
Require-Match $ci 'npm --prefix tests/e2e ci --ignore-scripts' 'E2E must use the lockfile with lifecycle scripts disabled.'
Require-Match $ci 'npm --prefix tests/e2e run install:browsers' 'E2E browser installation must be explicit.'
Require-Match $ci 'uses:\s*\./references/Hexalith\.Builds/Github/dapr-init' 'Scheduled E2E must initialize Dapr through the reviewed root dependency.'
Require-Match $ci 'dotnet tool install --global Aspire\.Cli --version 13\.4\.6' 'Scheduled E2E must install the repository-supported Aspire CLI version.'
Require-Match $ci 'npm --prefix tests/e2e run test:live:managed' 'Scheduled E2E must use the managed AppHost lifecycle runner.'
Require-Match $ci '^\s*TEST_USER_PASSWORD:\s*\$\{\{ secrets\.[A-Z0-9_]+ \}\}\s*$' 'Scheduled E2E credentials must come from a GitHub secret.'
Require-Match $ci '^\s*if:\s*always\(\)\s*$' 'Scheduled E2E must unconditionally run exact-AppHost teardown.'
Require-Match $ci 'aspire stop --apphost "\$GITHUB_WORKSPACE/src/Hexalith\.Projects\.AppHost/Hexalith\.Projects\.AppHost\.csproj" --non-interactive' 'Scheduled E2E teardown must target the exact Projects AppHost.'
Require-Match $ci '^\s*if:\s*failure\(\)\s*$' 'E2E failure evidence must be uploaded on failure.'

Require-Match $managedE2E '^trap cleanup EXIT\s*$' 'The managed E2E runner must unconditionally trap exact-AppHost cleanup.'
Require-Match $managedE2E 'Projects__E2E__LiveFixtures=1' 'The managed E2E runner must explicitly enable the fixture profile.'
Require-Match $managedE2E 'for resource in security eventstore tenants projects projects-workers projects-ui conversations folders memories live-fixtures' 'The managed E2E runner must wait for every required AppHost resource.'
$describeMatches = [regex]::Matches($managedE2E, '(?m)^aspire describe --apphost ').Count
if ($describeMatches -ne 1) {
    $failures.Add("The managed E2E runner must describe the AppHost exactly once; found $describeMatches calls.")
}
Require-Match $managedE2E 'npx playwright test[\s\S]*live-apphost-startup\.spec\.ts[\s\S]*projects-authentication\.spec\.ts[\s\S]*--workers 2' 'The managed E2E runner must run startup/auth smoke with two workers.'
Require-Match $managedE2E 'npx playwright test\s*\\\s*\r?\n\s*--project chromium\s*\\\s*\r?\n\s*--workers 2' 'The managed E2E runner must run the full Chromium lane with two workers.'
Forbid-Match $managedE2E 'aspire stop --all' 'The managed E2E runner must never stop unrelated AppHosts.'

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

Forbid-Match $ci '^\s*release:\s*$' 'CI must not own a release job; release.yml owns publication.'
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
