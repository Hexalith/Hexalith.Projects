param([Parameter(Mandatory = $true)] [string] $Payload, [Parameter(Mandatory = $true)] [string] $Workspace)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$null = [Reflection.Assembly]::LoadFrom((Join-Path $Payload 'NuGet.Versioning.dll'))
$tooling = [Reflection.Assembly]::LoadFrom((Join-Path $Payload 'Hexalith.Builds.Tooling.dll'))
$pathsType = $tooling.GetType('Hexalith.Builds.Tooling.Runtime.CompositionAspireHostPaths', $true)
$pinsType = $tooling.GetType('Hexalith.Builds.Tooling.Runtime.CompositionToolchainPins', $true)
$resolved = $null
$unavailable = $pathsType::TryResolve($pathsType::DefaultPackagesRoot(), $pinsType::AspireAppHostSdkVersion, $pathsType::CurrentRuntimeIdentifier(), [ref]$resolved)
if ($null -ne $unavailable) {
    [Console]::Error.WriteLine("$($unavailable.RuleId) $($unavailable.Message)")
    exit 3
}
$created = $resolved.CreateEnvironment($Workspace)
$environment = [ordered]@{}
foreach ($key in $created.Keys) { $environment[$key] = $created[$key] }
$environment | ConvertTo-Json -Compress