#!/usr/bin/env pwsh

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..' '..')).Path
$workflowRoot = Join-Path $repositoryRoot '.github/workflows'
$ciPath = Join-Path $workflowRoot 'ci.yml'
$releasePath = Join-Path $workflowRoot 'release.yml'
$solutionPath = Join-Path $repositoryRoot 'Hexalith.Projects.CI.slnx'
$releaseConfigPath = Join-Path $repositoryRoot 'release.config.cjs'
$trustedPublishingRunbookPath = Join-Path $repositoryRoot 'docs/runbooks/nuget-trusted-publishing.md'
$directoryBuildPropsPath = Join-Path $repositoryRoot 'Directory.Build.props'
$directoryPackagesPath = Join-Path $repositoryRoot 'Directory.Packages.props'
$frontComposerGatePath = Join-Path $scriptRoot 'run-frontcomposer-inspect-gate.ps1'
$openApiGatePath = Join-Path $scriptRoot 'run-openapi-fingerprint-gate.ps1'
$managedE2EPath = Join-Path $repositoryRoot 'tests/e2e/run-live-apphost.sh'
$failures = [System.Collections.Generic.List[string]]::new()
$buildsExecutionSha = 'cb91511794c8898b738d85dc6c751f82b832cbc9'
$releaseBuildsExecutionSha = 'a07078ad74d3727bc5a6b6d85d47d56a6e5c9fec'
$nugetLoginSha = '8d196754b4036150537f80ac539e15c2f1028841'
$expectedReleasePackageIds = @(
    'Hexalith.Projects.Contracts',
    'Hexalith.Projects',
    'Hexalith.Projects.Client',
    'Hexalith.Projects.Testing',
    'Hexalith.Projects.ServiceDefaults'
)

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

function Get-WorkflowJobBlocks {
    param([string] $Text)

    $jobsMarker = [regex]::Match($Text, '(?m)^jobs:\r?$')
    if (-not $jobsMarker.Success) {
        return @()
    }

    $jobsText = $Text.Substring($jobsMarker.Index + $jobsMarker.Length).TrimStart("`r", "`n")
    return @(
        [regex]::Matches($jobsText, '(?ms)^  (?<name>[A-Za-z0-9_-]+):\r?\n.*?(?=^  [A-Za-z0-9_-]+:\r?$|\z)') |
            ForEach-Object {
                [pscustomobject]@{
                    Name = $_.Groups['name'].Value
                    Text = $_.Value
                    Index = $_.Index
                }
            }
    )
}

function Get-NamedStepBlocks {
    param([string] $JobText)

    return @(
        [regex]::Matches($JobText, '(?ms)^      - name:\s*(?<name>[^\r\n#]+?)\s*\r?\n.*?(?=^      - |\z)') |
            ForEach-Object {
                [pscustomobject]@{
                    Name = $_.Groups['name'].Value.Trim()
                    Text = $_.Value
                    Index = $_.Index
                }
            }
    )
}

function Get-RequiredNamedStep {
    param(
        [object[]] $Steps,
        [string] $Name,
        [string] $Owner
    )

    $matches = @($Steps | Where-Object { $_.Name -ceq $Name })
    if ($matches.Count -ne 1) {
        $failures.Add("$Owner must contain exactly one '$Name' step; found $($matches.Count).")
        return $null
    }

    return $matches[0]
}

function Get-UsesReferences {
    param([string] $Text)

    $results = [System.Collections.Generic.List[object]]::new()
    $lineNumber = 0
    foreach ($line in $Text -split "`r?`n") {
        $lineNumber++
        if ($line -notmatch '^(?<indent>\s*)(?<dash>-\s+)?uses:\s*(?<reference>[^\s#]+)\s*(?:#.*)?$') {
            continue
        }

        $results.Add([pscustomobject]@{
            Reference = $Matches['reference']
            Indent = $Matches['indent'].Length
            HasDash = -not [string]::IsNullOrEmpty($Matches['dash'])
            LineNumber = $lineNumber
        })
    }

    return @($results)
}

function Get-ActiveWorkflowText {
    param([string] $Text)

    return (($Text -split "`r?`n" | Where-Object { $_ -notmatch '^\s*#' }) -join "`n")
}

if (-not (Test-Path $ciPath)) {
    throw "CI workflow not found at $ciPath."
}

$ci = Get-Content -Path $ciPath -Raw
$solution = Get-Content -Path $solutionPath -Raw
$packageManifestPath = Join-Path $repositoryRoot 'tools/release-packages.json'
$releaseConfig = Get-Content -Path $releaseConfigPath -Raw
if (-not (Test-Path $trustedPublishingRunbookPath)) {
    $failures.Add('The NuGet Trusted Publishing migration runbook must exist.')
    $trustedPublishingRunbook = ''
}
else {
    $trustedPublishingRunbook = Get-Content -Path $trustedPublishingRunbookPath -Raw
}
$directoryBuildProps = Get-Content -Path $directoryBuildPropsPath -Raw
$directoryPackages = Get-Content -Path $directoryPackagesPath -Raw
[xml] $directoryBuildPropsXml = $directoryBuildProps
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

    $packageFixtureStepMatches = [regex]::Matches(
        $workflowGates,
        '(?ms)^      - name: Validate release package tool fixtures\r?\n.*?(?=^      - |\z)'
    )
    if ($packageFixtureStepMatches.Count -ne 1) {
        $failures.Add('workflow-gates must run exactly one release package tool fixture step before restore-capable jobs.')
    }
    else {
        $packageFixtureStep = ($packageFixtureStepMatches[0].Value -replace "`r`n", "`n").TrimEnd([char[]] "`r`n")
        $expectedPackageFixtureStep = @(
            '      - name: Validate release package tool fixtures'
            '        env:'
            '          PYTHONDONTWRITEBYTECODE: ''1'''
            '        run: python3 -m unittest tests/tools/test_release_package_tools.py -v'
        ) -join "`n"
        if ($packageFixtureStep -cne $expectedPackageFixtureStep) {
            $failures.Add('The release package fixture must be an exact blocking name/env/run step with bytecode disabled.')
        }

        $ciInvariantIndex = $workflowGates.IndexOf('      - name: Validate CI/CD invariants', [System.StringComparison]::Ordinal)
        if ($packageFixtureStepMatches[0].Index -ge $ciInvariantIndex -or $ciInvariantIndex -lt 0) {
            $failures.Add('The hermetic package fixtures must run before CI/CD invariant validation.')
        }
    }

    $releaseWorkflowStepMatches = [regex]::Matches(
        $workflowGates,
        '(?ms)^      - name: Validate release workflow shell behavior\r?\n.*?(?=^      - |\z)'
    )
    if ($releaseWorkflowStepMatches.Count -ne 1) {
        $failures.Add('workflow-gates must run exactly one release workflow shell behavior step.')
    }
    else {
        $releaseWorkflowStep = ($releaseWorkflowStepMatches[0].Value -replace "`r`n", "`n").TrimEnd([char[]] "`r`n")
        $expectedReleaseWorkflowStep = @(
            '      - name: Validate release workflow shell behavior'
            '        env:'
            '          PYTHONDONTWRITEBYTECODE: ''1'''
            '        run: python3 -m unittest tests/tools/test_release_workflow.py -v'
        ) -join "`n"
        if ($releaseWorkflowStep -cne $expectedReleaseWorkflowStep) {
            $failures.Add('The release workflow shell behavior fixture must be an exact blocking name/env/run step with bytecode disabled.')
        }

        $ciInvariantIndex = $workflowGates.IndexOf('      - name: Validate CI/CD invariants', [System.StringComparison]::Ordinal)
        if ($releaseWorkflowStepMatches[0].Index -ge $ciInvariantIndex -or $ciInvariantIndex -lt 0) {
            $failures.Add('The release workflow shell behavior fixture must run before CI/CD invariant validation.')
        }
    }
}

# Release is an operator-dispatched caller-owned workflow behind a verified green
# main SHA. The protected job owns the OIDC subject so NuGet never has to trust a
# reusable-workflow identity.
if (-not (Test-Path $releasePath)) {
    $failures.Add('release.yml must exist; release is an operator-dispatched workflow in every Hexalith module.')
    $release = ''
}
else {
    $release = Get-Content -Path $releasePath -Raw
}

$releaseJobBlocks = @(Get-WorkflowJobBlocks -Text $release)
$expectedReleaseJobNames = @('verify-source', 'release')
$releaseJobNames = @($releaseJobBlocks | ForEach-Object { $_.Name })
if ($releaseJobNames.Count -ne $expectedReleaseJobNames.Count -or
    @(Compare-Object -ReferenceObject $expectedReleaseJobNames -DifferenceObject $releaseJobNames).Count -ne 0) {
    $failures.Add('Release must define exactly the verify-source and caller-owned release jobs; additional jobs are forbidden.')
}

$releaseJobCandidates = @($releaseJobBlocks | Where-Object { $_.Name -ceq 'release' })
$verifySourceJobCandidates = @($releaseJobBlocks | Where-Object { $_.Name -ceq 'verify-source' })
$releaseJob = if ($releaseJobCandidates.Count -eq 1) { $releaseJobCandidates[0].Text } else { '' }
$verifySourceJob = if ($verifySourceJobCandidates.Count -eq 1) { $verifySourceJobCandidates[0].Text } else { '' }
$releaseSteps = @(Get-NamedStepBlocks -JobText $releaseJob)
$verifySourceSteps = @(Get-NamedStepBlocks -JobText $verifySourceJob)
$verifySourceStep = Get-RequiredNamedStep -Steps $verifySourceSteps -Name 'Require current main with successful exact-source CI' -Owner 'verify-source'

foreach ($jobBlock in $releaseJobBlocks) {
    foreach ($usesReference in @(Get-UsesReferences -Text $jobBlock.Text)) {
        if ($usesReference.Indent -eq 4 -and -not $usesReference.HasDash) {
            $failures.Add("Release job '$($jobBlock.Name)' must be caller-owned; job-level reusable call '$($usesReference.Reference)' is forbidden.")
        }
    }
}

foreach ($sharedWorkflow in @('codeql.yml', 'commitlint.yml', 'dependency-review.yml')) {
    $sharedWorkflowPath = Join-Path $workflowRoot $sharedWorkflow
    if (-not (Test-Path $sharedWorkflowPath)) {
        $failures.Add("$sharedWorkflow must exist; every Hexalith module runs the shared $($sharedWorkflow -replace '\.yml$', '') lane.")
    }
    else {
        $sharedWorkflowText = Get-Content -Path $sharedWorkflowPath -Raw
        $expectedMutableReference = "Hexalith/Hexalith.Builds/.github/workflows/$sharedWorkflow@main"
        $actualSharedReferences = @(
            Get-UsesReferences -Text $sharedWorkflowText |
                Where-Object { $_.Indent -eq 4 -and -not $_.HasDash -and $_.Reference -ceq $expectedMutableReference }
        )
        if ($actualSharedReferences.Count -ne 1) {
            $failures.Add("$sharedWorkflow must contain exactly one actual job-level uses line for $expectedMutableReference.")
        }
    }
}

Require-Match $release '^on:\s*\r?\n\s*workflow_dispatch:\s*$' 'Release must be dispatch-only so publication stays an explicit operator action.'
Forbid-Match $release '^\s*(push|pull_request|schedule):\s*$' 'Release must never be triggered by push, pull_request, or schedule.'
if ($null -eq $verifySourceStep) {
    $verifySourceStepText = ''
}
else {
    $verifySourceStepText = $verifySourceStep.Text
}
Require-Match $verifySourceStepText 'No successful push CI run exists for the exact current main SHA' 'The named exact-source preflight must prove a successful push CI run for the dispatched SHA.'
Require-Match $verifySourceStepText 'The dispatched source is no longer the live main tip' 'The named exact-source preflight must prove the dispatch selected the live main tip.'
Require-Match $releaseJob '^\s{4}needs:\s*verify-source\s*$' 'The release job must depend on the exact-source preflight.'
Require-Match $releaseJob '^\s{4}runs-on:\s*ubuntu-latest\s*$' 'The release job must run as a caller-owned job.'
Require-Match $releaseJob '^\s{4}environment:\s*production\s*$' 'Release must enter the protected production environment.'
Require-Match $releaseJob '^\s{4}timeout-minutes:\s*60\s*$' 'The protected release job must preserve its 60-minute timeout.'
Require-Match $release '^\s*cancel-in-progress:\s*false\s*$' 'An in-flight release must never be cancelled by a newer dispatch.'

# The caller job needs OIDC only for the official NuGet login action and retains
# the scopes used by the existing semantic-release lifecycle.
foreach ($scope in @('actions: read', 'contents: write', 'id-token: write', 'issues: write', 'pull-requests: write')) {
    Require-Match $releaseJob ("^\s{6}" + [regex]::Escape($scope) + "\s*$") "The caller-owned release job must grant '$scope'."
}
Forbid-Match $releaseJob '^\s{6}attestations:\s*write\s*$' 'The non-governed Projects release must not retain the reusable workflow attestation scope.'

$checkoutBuildsStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Checkout approved Builds actions' -Owner 'release'
$validateBuildsStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Validate approved Builds actions identity' -Owner 'release'
$initializeBuildStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Initialize root-declared submodules' -Owner 'release'
$installNpmStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Install npm dependencies' -Owner 'release'
$verifyNpmStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Verify npm dependency provenance and signatures' -Owner 'release'
$restoreStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Restore release solution' -Owner 'release'
$buildStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Build release solution' -Owner 'release'
$manifestStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Validate five-package manifest' -Owner 'release'
$freezeStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Resolve release publication freeze' -Owner 'release'
$sourceStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Revalidate current source before NuGet login' -Owner 'release'
$loginStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Exchange GitHub OIDC token for temporary NuGet API key' -Owner 'release'
$semanticReleaseStep = Get-RequiredNamedStep -Steps $releaseSteps -Name 'Semantic Release' -Owner 'release'

# Caller-owned setup still consumes the approved root-submodule initializer from
# one immutable Hexalith.Builds revision. Every assertion is bound to its named step.
Require-Match $checkoutBuildsStep.Text '^\s{8}uses:\s*actions/checkout@[0-9a-f]{40}\s*(?:#.*)?$' 'The Builds checkout step must use an immutable checkout action.'
Require-Match $checkoutBuildsStep.Text '^\s{10}repository:\s*Hexalith/Hexalith\.Builds\s*$' 'The Builds checkout step must target Hexalith.Builds.'
Require-Match $checkoutBuildsStep.Text ('^\s{10}ref:\s*' + $releaseBuildsExecutionSha + '\s*$') 'The Builds checkout step must use the approved immutable execution SHA.'
Require-Match $validateBuildsStep.Text ('^\s{10}BUILDS_EXECUTION_SHA:\s*' + $releaseBuildsExecutionSha + '\s*$') 'The Builds identity step must validate the approved execution SHA.'
Require-Match $initializeBuildStep.Text '^\s{8}uses:\s*\./\.hexalith/builds-execution/Github/initialize-build\s*$' 'The initialization step must use the approved checked-out Builds action.'
Require-Match $installNpmStep.Text '^\s{8}run:\s*npm ci --ignore-scripts\s*$' 'The npm install step must use the lockfile with lifecycle scripts disabled.'
Require-Match $verifyNpmStep.Text '^\s{8}run:\s*npm audit signatures\s*$' 'The npm provenance step must verify registry signatures.'
Require-Match $restoreStep.Text '^\s{8}run:\s*dotnet restore Hexalith\.Projects\.CI\.slnx\s*$' 'The restore step must use the package-only CI solution.'
Require-Match $buildStep.Text '^\s{8}run:\s*dotnet build Hexalith\.Projects\.CI\.slnx --no-restore --configuration Release -warnaserror\s*$' 'The build step must use the package-only CI solution in Release with warnings as errors.'

# The declared package count is the fail-closed gate: it must agree with both the
# manifest it is checked against and the count semantic-release publishes.
if (-not (Test-Path $packageManifestPath)) {
    $failures.Add('tools/release-packages.json must exist; the release publication identity is frozen from it.')
}
else {
    $manifest = Get-Content -Path $packageManifestPath -Raw | ConvertFrom-Json
    $manifestPackageIds = @($manifest.packages | ForEach-Object { $_.id })
    $manifestCount = $manifestPackageIds.Count
    if ($manifestCount -ne $expectedReleasePackageIds.Count -or
        @(Compare-Object -ReferenceObject $expectedReleasePackageIds -DifferenceObject $manifestPackageIds).Count -ne 0) {
        $failures.Add('tools/release-packages.json must contain exactly the five approved Projects package IDs.')
    }
    Require-Match $manifestStep.Text ("^\s{10}EXPECTED_PACKAGE_COUNT:\s*'" + $manifestCount + "'\s*$") "The named manifest step count gate must equal the $manifestCount package(s) declared in tools/release-packages.json."
    Require-Match $manifestStep.Text '\.packages \| length == \$expected' 'The named manifest step must compare the package count with its independent expected count.'
    Require-Match $releaseConfig ('-eq ' + $manifestCount + "'") "release.config.cjs must publish exactly $manifestCount package(s)."
}

# Matrix row: frozen publication builds successfully but reaches neither the OIDC
# exchange nor Semantic Release. The runbook requires a repository-level override
# so the effective vars context cannot fall through to an organization value.
Require-Match $freezeStep.Text '^\s{10}HEXALITH_RELEASE_PUBLISH_ENABLED:\s*\$\{\{ vars\.HEXALITH_RELEASE_PUBLISH_ENABLED \}\}\s*$' 'The freeze step must bind the effective Actions variable for an exact shell comparison.'
Require-Match $freezeStep.Text ([regex]::Escape('if [ "${HEXALITH_RELEASE_PUBLISH_ENABLED-}" = "true" ]; then')) 'The freeze step must require the exact case-sensitive, untrimmed true value.'
Require-Match $freezeStep.Text 'publish-enabled=false' 'The named freeze step must produce an explicit false verdict.'
Require-Match $freezeStep.Text 'NuGet login and Semantic Release are skipped' 'The named freeze step must report that credential exchange and publication were skipped.'

# Matrix rows: stale source fails before login, while a valid source reaches the
# pinned official login action. A missing username or claim mismatch is therefore
# a login failure before Semantic Release receives any credential.
Require-Match $sourceStep.Text 'The release source became stale during setup' 'The named source-revalidation step must fail when main advances.'
Require-Match $sourceStep.Text '^\s{10}SOURCE_BRANCH:\s*main\s*$' 'The named source-revalidation step must be bound to main.'
Require-Match $sourceStep.Text '^\s{10}PUBLISH_ENABLED:\s*\$\{\{ steps\.publish-gate\.outputs\.publish-enabled \}\}\s*$' 'The source-revalidation step must consume the freeze-gate verdict.'
Require-Match $sourceStep.Text ([regex]::Escape('publish_enabled="${PUBLISH_ENABLED-true}"')) 'An absent source-gate verdict must fail toward revalidation, not skip it.'
Require-Match $loginStep.Text ('^\s{8}uses:\s*NuGet/login@' + $nugetLoginSha + '\s*#\s*v1\.2\.0\s*$') 'The named login step must use the reviewed NuGet/login v1.2.0 commit.'
Require-Match $loginStep.Text '^\s{8}id:\s*nuget-login\s*$' 'The named login step must expose its temporary output through the reviewed step id.'
Require-Match $loginStep.Text '^\s{10}user:\s*\$\{\{ vars\.NUGET_USER \}\}\s*$' 'The named login step must receive the configured NUGET_USER profile name.'
$publishStepGuardPattern = '^\s{8}if:\s*\$\{\{ steps\.publish-gate\.outputs\.publish-enabled == ''true'' \}\}\s*$'
Require-Match $loginStep.Text $publishStepGuardPattern 'The named login step must require the exact enabled verdict.'
Require-Match $semanticReleaseStep.Text $publishStepGuardPattern 'The named Semantic Release step must require the exact enabled verdict.'
Require-Match $semanticReleaseStep.Text '^\s{10}NUGET_API_KEY:\s*\$\{\{ steps\.nuget-login\.outputs\.NUGET_API_KEY \}\}\s*$' 'The named Semantic Release step must receive only the short-lived login output.'
Require-Match $semanticReleaseStep.Text '^\s{8}run:\s*npm exec --no -- semantic-release\s*$' 'The named Semantic Release step must run the locked lifecycle.'
$activeWorkflowText = (($workflowFiles | ForEach-Object { Get-ActiveWorkflowText -Text (Get-Content -Path $_.FullName -Raw) }) -join "`n")
$legacyNuGetSecretPattern = 'secrets\s*(?:\.\s*NUGET_API_KEY|\[\s*(?:''NUGET_API_KEY''|"NUGET_API_KEY")\s*\])'
Forbid-Match $activeWorkflowText $legacyNuGetSecretPattern 'No active workflow line may read the legacy NUGET_API_KEY secret through dot or bracket syntax.'
if ([regex]::Matches((Get-ActiveWorkflowText -Text $releaseJob), '(?m)^.*NUGET_API_KEY.*$').Count -ne 1) {
    $failures.Add('The temporary NUGET_API_KEY output must be bound only to the Semantic Release process environment.')
}

$orderedReleaseSteps = @(
    'Install npm dependencies',
    'Verify npm dependency provenance and signatures',
    'Restore release solution',
    'Build release solution',
    'Validate five-package manifest',
    'Resolve release publication freeze',
    'Revalidate current source before NuGet login',
    'Exchange GitHub OIDC token for temporary NuGet API key',
    'Semantic Release'
)
$previousStepIndex = -1
foreach ($stepName in $orderedReleaseSteps) {
    $matches = @($releaseSteps | Where-Object { $_.Name -ceq $stepName })
    if ($matches.Count -ne 1) {
        $failures.Add("Release ordering requires exactly one '$stepName' step.")
        continue
    }

    $stepIndex = $matches[0].Index
    if ($stepIndex -le $previousStepIndex) {
        $failures.Add("Release step '$stepName' is out of the required guarded order.")
    }
    $previousStepIndex = $stepIndex
}
$allReleaseStepStarts = @([regex]::Matches($releaseJob, '(?m)^      - '))
if ($null -ne $loginStep -and $null -ne $semanticReleaseStep) {
    $nextStep = @($allReleaseStepStarts | Where-Object { $_.Index -gt $loginStep.Index } | Select-Object -First 1)
    if ($nextStep.Count -ne 1 -or $nextStep[0].Index -ne $semanticReleaseStep.Index) {
        $failures.Add('NuGet/login must be immediately before Semantic Release so the one-hour credential is acquired as late as possible.')
    }
}

# The operator runbook freezes exact policy rows and package bullets. HTML
# comments are removed first so comment-only decoys cannot satisfy the gate.
$runbookPolicyText = [regex]::Replace($trustedPublishingRunbook, '(?s)<!--.*?-->', '')
$expectedPolicyRows = @(
    '| Policy owner | Organization `Hexalith` |',
    '| Repository owner | `Hexalith` |',
    '| Repository | `Hexalith.Projects` |',
    '| Workflow file | `release.yml` (file name only) |',
    '| Environment | `production` |'
)
foreach ($policyRow in $expectedPolicyRows) {
    if ([regex]::Matches($runbookPolicyText, ('(?m)^' + [regex]::Escape($policyRow) + '\r?$')).Count -ne 1) {
        $failures.Add("The Trusted Publishing runbook must contain exactly the policy table row: $policyRow")
    }
}

$scopeStart = $runbookPolicyText.IndexOf('Grant publication of new versions only for these five exact package ID', [System.StringComparison]::Ordinal)
$scopeEnd = if ($scopeStart -ge 0) { $runbookPolicyText.IndexOf('Do not grant a new-package scope.', $scopeStart, [System.StringComparison]::Ordinal) } else { -1 }
if ($scopeStart -lt 0 -or $scopeEnd -le $scopeStart) {
    $failures.Add('The Trusted Publishing runbook must contain the exact five-package policy-scope section.')
}
else {
    $scopeSection = $runbookPolicyText.Substring($scopeStart, $scopeEnd - $scopeStart)
    $runbookPackageIds = @(
        [regex]::Matches($scopeSection, '(?m)^- `(?<id>[^`]+)`\r?$') |
            ForEach-Object { $_.Groups['id'].Value }
    )
    if ($runbookPackageIds.Count -ne $expectedReleasePackageIds.Count -or
        @(Compare-Object -ReferenceObject $expectedReleasePackageIds -DifferenceObject $runbookPackageIds).Count -ne 0) {
        $failures.Add('The Trusted Publishing runbook policy scope must contain exactly the five manifest package-ID bullets, with no prefixes or extras.')
    }
}

foreach ($runbookContract in @(
    'gh variable set HEXALITH_RELEASE_PUBLISH_ENABLED --repo Hexalith/Hexalith.Projects --body false',
    'npm exec --no -- semantic-release --dry-run',
    'gh run list --repo Hexalith/Hexalith.Projects --workflow release.yml',
    '_bmad-output/implementation-artifacts/release-evidence/nuget-trusted-publishing-',
    'run-package-dependency-gate.ps1 -Version "$RELEASE_VERSION" -PackageDirectory "$PACKAGE_DIRECTORY" -SkipPack',
    'dotnet nuget verify --all'
)) {
    Require-Match $runbookPolicyText ([regex]::Escape($runbookContract)) "The Trusted Publishing runbook must include the executable contract '$runbookContract'."
}
Require-Match $runbookPolicyText '(?im)^## Rollback\r?$' 'The Trusted Publishing runbook must include an explicit rollback section.'
Require-Match $runbookPolicyText '(?im)^## Retire the legacy credential\r?$' 'The Trusted Publishing runbook must include an explicit legacy-secret retirement section.'
Require-Match $runbookPolicyText 'permanently active' 'The Trusted Publishing runbook must require permanent policy activation before secret retirement.'

Require-Match $ci '^\s*push:\s*$' 'CI must run on pushes.'
Require-Match $ci '^\s*pull_request:\s*$' 'CI must run on pull requests.'
Require-Match $ci '^\s*schedule:\s*$' 'CI must include a scheduled lane.'
Require-Match $ci "dapr-version:\s*'1\.18(?:\.0)?'" 'CI must use the supported Dapr 1.18 baseline.'
Require-Match $ci "dapr-runtime-version:\s*'1\.18\.2'" 'CI must use the approved Dapr 1.18.2 runtime exception.'
Require-Match $ci '^\s*integration-test-projects:\s*\|' 'The reusable CI workflow must run Integration.Tests separately.'
Require-Match $ci '^\s*- name:\s*Validate accepted G-6 runtime/toolchain packet\s*$' 'CI must run the accepted G-6 packet validator after root submodules initialize.'
Require-Match $ci 'validate-runtime-toolchain-evidence\.py\s*\r?\n\s*--workspace \.\s*\r?\n\s*--baseline references/Hexalith\.Builds/Tools/runtime-toolchain-baseline\.json\s*\r?\n\s*--packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/packet\.json' 'CI must validate the real bound G-6 packet.'
Require-Match $ci ('uses:\s*Hexalith/Hexalith\.Builds/\.github/workflows/domain-ci\.yml@' + $buildsExecutionSha) 'CI must call the accepted package-aware domain-ci workflow SHA.'
Require-Match $ci '(?ms)^  ci:\r?\n\s+needs:\s*workflow-gates\r?\n\s+uses:' 'The reusable restore/build job must wait for independent workflow and package fixtures.'
Forbid-Match $ci 'uses:\s*Hexalith/Hexalith\.Builds/.+@main' 'CI must not execute mutable Hexalith.Builds actions or workflows.'
foreach ($buildsCall in [regex]::Matches($ci, 'uses:\s*Hexalith/Hexalith\.Builds/[^\s]+@([^\s#]+)')) {
    if ($buildsCall.Groups[1].Value -cne $buildsExecutionSha) {
        $failures.Add("CI executes Hexalith.Builds at '$($buildsCall.Groups[1].Value)' instead of '$buildsExecutionSha'.")
    }
}
Require-Match $ci '^\s*test-platform:\s*microsoft-testing-platform\s*$' 'CI must explicitly select Microsoft.Testing.Platform.'
Require-Match $ci '^\s*run-consumer-validation:\s*true\s*$' 'CI must enable shared package and real-consumer validation.'
Require-Match $ci '^\s*build-timeout-minutes:\s*30\s*$' 'CI must preserve the accepted 30-minute shared build timeout.'

# The reusable CI callee runs `dotnet restore "$SOLUTION"`: the input is one quoted
# argument, so an embedded MSBuild switch becomes part of the project path (MSB1009).
# `github.workspace` is also empty when a reusable-workflow `with:` block is evaluated,
# so any path built from it silently resolves to the filesystem root. Release is now
# caller-owned and its literal package-only restore/build commands are checked above.
foreach ($caller in @(@{ Name = 'ci.yml'; Text = $ci })) {
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

Forbid-Match $ci '^\s*solution:.*-p:' 'A solution input must not embed MSBuild switches; the callee quotes it as a single argument.'
Forbid-Match $ci '^\s*(solution|.*-p:.*):.*\$\{\{ github\.workspace \}\}' 'github.workspace is empty when a reusable-workflow `with:` is evaluated; never build a path from it there.'
Require-Match $ci '^\s*cancel-in-progress:\s*\$\{\{ github\.event_name != ''push'' \|\| github\.ref != ''refs/heads/main'' \}\}\s*$' 'Main push/release workflows must never be cancelled by a newer run.'
Forbid-Match $ci '^\s*package-gates:\s*$' 'CI package creation and validation must stay in the accepted reusable workflow.'
Require-Match $ci '^\s*e2e:\s*$' 'CI must include the scheduled E2E job.'
Require-Match $ci "if:\s*\$\{\{ github\.event_name == 'schedule' \}\}" 'E2E must be limited to the scheduled lane.'
Require-Match $ci 'npm --prefix tests/e2e ci --ignore-scripts' 'E2E must use the lockfile with lifecycle scripts disabled.'
Require-Match $ci 'npm --prefix tests/e2e run install:browsers' 'E2E browser installation must be explicit.'
Require-Match $ci 'uses:\s*\./references/Hexalith\.Builds/Github/dapr-init' 'Scheduled E2E must initialize Dapr through the reviewed root dependency.'
Require-Match $ci "runtime-version:\s*'1\.18\.2'" 'Scheduled E2E must initialize the approved Dapr 1.18.2 runtime.'
Require-Match $ci 'dotnet tool install --global Aspire\.Cli --version 13\.5\.4' 'Scheduled E2E must install the repository-supported Aspire CLI version.'
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

# Release and CI always consume sibling libraries as packages. Only local Debug
# builds may default back to source projects; AppHost executable resources are
# intentionally outside this library policy.
Require-Match $directoryBuildProps ([regex]::Escape("'`$(CI)' == 'true'") + '">false<') 'CI must force UseHexalithProjectReferences=false.'
Require-Match $directoryBuildProps ([regex]::Escape("'`$(Configuration)' == 'Debug'") + '">true<') 'Only local Debug builds may default to sibling project references.'
$referenceModeTargets = @($directoryBuildPropsXml.Project.Target | Where-Object { $_.Name -eq 'RejectUnsafeHexalithProjectReferenceMode' })
$expectedReferenceModeCondition = "'`$(UseHexalithProjectReferences)' == 'true' and ('`$(CI)' == 'true' or '`$(Configuration)' != 'Debug')"
if ($referenceModeTargets.Count -ne 1 -or
    $referenceModeTargets[0].BeforeTargets -ne 'Restore;PrepareForBuild' -or
    $referenceModeTargets[0].Condition -ne $expectedReferenceModeCondition -or
    $null -eq $referenceModeTargets[0].Error) {
    $failures.Add('Directory.Build.props must reject explicit source mode in CI and every non-Debug configuration before restore/build.')
}
foreach ($packageId in @('Hexalith.Conversations.Client', 'Hexalith.Conversations.Contracts', 'Hexalith.Folders.Client', 'Hexalith.Folders.Contracts')) {
    Require-Match $directoryPackages ('<PackageVersion Include="' + [regex]::Escape($packageId) + '" Version="1\.0\.0"\s*/>') "$packageId must remain pinned to the unpublished 1.0.0 blocker version."
}

$projectFiles = Get-ChildItem -Path $repositoryRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '[\\/]references[\\/]' }
foreach ($projectFile in $projectFiles) {
    [xml] $projectXml = Get-Content -Path $projectFile.FullName -Raw
    foreach ($reference in @($projectXml.Project.ItemGroup.ProjectReference)) {
        if ($null -eq $reference -or $reference.Include -notlike '$(Hexalith*.csproj') {
            continue
        }

        $normalizedInclude = $reference.Include -replace '\\', '/'
        $isAppHostExecutable = $projectFile.Name -eq 'Hexalith.Projects.AppHost.csproj' -and
            $normalizedInclude -in @('$(HexalithEventStoreRoot)/src/Hexalith.EventStore/Hexalith.EventStore.csproj', '$(HexalithTenantsRoot)/src/Hexalith.Tenants/Hexalith.Tenants.csproj')
        $condition = if (-not [string]::IsNullOrWhiteSpace($reference.Condition)) { $reference.Condition } else { $reference.ParentNode.Condition }
        if (-not $isAppHostExecutable -and $condition -ne "'`$(UseHexalithProjectReferences)' == 'true'") {
            $failures.Add("$($projectFile.FullName.Substring($repositoryRoot.Length + 1)) has an unconditional sibling library ProjectReference: $($reference.Include)")
        }

        if (-not $isAppHostExecutable) {
            $packageId = [System.IO.Path]::GetFileNameWithoutExtension($normalizedInclude)
            $matchingPackageReferences = @($projectXml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageId })
            $hasPackageModeReference = $false
            foreach ($packageReference in $matchingPackageReferences) {
                $packageCondition = if (-not [string]::IsNullOrWhiteSpace($packageReference.Condition)) { $packageReference.Condition } else { $packageReference.ParentNode.Condition }
                if ($packageCondition -eq "'`$(UseHexalithProjectReferences)' != 'true'") {
                    $hasPackageModeReference = $true
                }
            }

            if (-not $hasPackageModeReference) {
                $failures.Add("$($projectFile.FullName.Substring($repositoryRoot.Length + 1)) has no package-mode $packageId reference for sibling project $($reference.Include)")
            }
        }
    }
}

Forbid-Match $ci '^\s*release:\s*$' 'CI must not own a release job; release.yml owns publication.'
Forbid-Match $allWorkflows '^\s*submodules:\s*(true|recursive)\s*$' 'Recursive or implicit recursive submodule checkout is forbidden.'
Forbid-Match $allWorkflows 'git\s+[^\r\n]*submodule\s+[^\r\n]*--recursive' 'Recursive submodule commands are forbidden.'
Forbid-Match $allWorkflows 'npm\s+(?:--prefix\s+\S+\s+)?install(?:\s|$)' 'Workflow dependency installation must not use npm install.'

foreach ($workflowFile in $workflowFiles) {
    $workflowText = Get-Content -Path $workflowFile.FullName -Raw
    foreach ($usesReference in @(Get-UsesReferences -Text $workflowText)) {
        $reference = $usesReference.Reference
        if ($reference.StartsWith('./', [System.StringComparison]::Ordinal)) {
            continue
        }

        $legacySharedWorkflowReference = "Hexalith/Hexalith.Builds/.github/workflows/$($workflowFile.Name)@main"
        if ($workflowFile.Name -in @('codeql.yml', 'commitlint.yml', 'dependency-review.yml') -and
            $reference -ceq $legacySharedWorkflowReference) {
            continue
        }

        if ($reference -notmatch '@[0-9a-fA-F]{40}$') {
            $failures.Add("$($workflowFile.Name):$($usesReference.LineNumber) uses a mutable or unreviewed action reference: $reference")
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error "ci-workflow-gate: $failure"
    }

    exit 1
}

Write-Host "ci-workflow-gate: PASSED — $($workflowFiles.Count) workflow file(s), caller-owned Trusted Publishing, immutable release action refs, safe E2E, and root-only submodule policy validated."
exit 0
