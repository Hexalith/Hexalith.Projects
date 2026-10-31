param([Parameter(Mandatory=$true)][string] $Path, [Parameter(Mandatory=$true)][string] $OutputDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Inspection output must be new.' }
$null = New-Item -ItemType Directory -Path $OutputDirectory
$assembly = [Reflection.Assembly]::LoadFile([IO.Path]::GetFullPath($Path))
$resource = 'Hexalith.Builds.Tooling.Manifest.platform-version-catalog.json'
$stream = $assembly.GetManifestResourceStream($resource)
if ($null -eq $stream) { throw "Missing $resource" }
try {
    $destination = [IO.File]::Create((Join-Path $OutputDirectory 'embedded-catalog.json'))
    try { $stream.CopyTo($destination) } finally { $destination.Dispose() }
} finally { $stream.Dispose() }
$information = @($assembly.GetCustomAttributesData() | Where-Object { $_.AttributeType.FullName -eq 'System.Reflection.AssemblyInformationalVersionAttribute' })
if ($information.Count -ne 1) { throw 'Expected one informational version.' }
$result = [ordered]@{
    path = [IO.Path]::GetFullPath($Path)
    sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    identity = $assembly.GetName().FullName
    informationalVersion = [string]$information[0].ConstructorArguments[0].Value
    resource = $resource
    embeddedCatalogSha256 = (Get-FileHash -LiteralPath (Join-Path $OutputDirectory 'embedded-catalog.json') -Algorithm SHA256).Hash.ToLowerInvariant()
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'tooling.json') -Encoding utf8
$result | ConvertTo-Json -Depth 5
