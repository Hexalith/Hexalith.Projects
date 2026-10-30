param([string]$Receipt, [switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Receipt 'extracted-tool/tools/net10.0/any/NuGet.Versioning.dll')
$selected = (Get-Content -LiteralPath (Join-Path $Receipt 'tuple-identity.json') -Raw | ConvertFrom-Json -AsHashtable).evaluated_package_versions
$metadata = Get-Content -LiteralPath (Join-Path $Receipt 'dependency-publications.json') -Raw | ConvertFrom-Json -AsHashtable
$results = @()
$unresolved = @()
$total = 0
foreach ($package in $metadata) {
    foreach ($group in $package.dependencyGroups) {
        if ($group.targetFramework -cne 'net10.0') { continue }
        foreach ($dependency in $group.dependencies) {
            $total++
            if (-not $selected.ContainsKey($dependency.id)) {
                $unresolved += [ordered]@{package=$package.id; dependency=$dependency.id; required=$dependency.range; reason='No exact selected version in the pinned catalog; fresh restored closure required.'}
                continue
            }
            $version = [NuGet.Versioning.NuGetVersion]::Parse($selected[$dependency.id])
            $range = [NuGet.Versioning.VersionRange]::Parse($dependency.range)
            $passed = $range.Satisfies($version)
            $results += [ordered]@{ package=$package.id; dependency=$dependency.id; required=$dependency.range; selected=$version.ToNormalizedString(); passed=$passed }
        }
    }
}
$failed = @($results | Where-Object { -not $_.passed })
$report = [ordered]@{totalConstraints=$total; checkedConstraints=$results.Count; failedConstraints=$failed.Count; unresolvedConstraints=$unresolved.Count; results=$results; unresolved=$unresolved; scope='Observed net10.0 metadata only; no restored transitive graph or runtime acceptance.'}
if (-not $CheckOnly) {
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $Receipt 'dependency-range-final.json') -Encoding utf8
}
$report | ConvertTo-Json -Depth 10
if ($failed.Count -ne 0 -or $results.Count -eq 0) { exit 1 }
