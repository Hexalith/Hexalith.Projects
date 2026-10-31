param([string] $SourceRoot, [string] $CorrectedRoot)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Get-ManifestFromScript([string] $ScriptPath) {
    $tokens = $null; $parseErrors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($ScriptPath, [ref] $tokens, [ref] $parseErrors)
    if ($parseErrors.Count -gt 0) { throw 'Script parse failed.' }
    foreach ($name in @('Get-Sha256Text', 'Get-FixtureManifest')) {
        $function = $ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
        if ($function.Count -ne 1) { throw "Expected exactly one $name function." }
        . ([scriptblock]::Create($function[0].Extent.Text))
    }
    Get-FixtureManifest -Directory (Join-Path $SourceRoot 'test/fixtures') -Mode 'repository-tracked'
}
$selected = Get-ManifestFromScript (Join-Path $SourceRoot 'Tools/test-g4-tool-package-contracts.ps1')
$corrected = Get-ManifestFromScript (Join-Path $CorrectedRoot 'Tools/test-g4-tool-package-contracts.ps1')
$hidden = 'package-consumer/.config/dotnet-tools.json'
if ($selected.files.Count -ne 292 -or $corrected.files.Count -ne 293) { throw "Unexpected enumeration counts: $($selected.files.Count), $($corrected.files.Count)." }
if ($selected.files.file -contains $hidden -or $corrected.files.file -notcontains $hidden) { throw 'Hidden-manifest regression failed.' }
[ordered]@{ scope = 'Focused future-patch enumeration control; never substitutes for producer qualification'; selectedCount = $selected.files.Count; correctedCount = $corrected.files.Count; selectedManifestSha256 = $selected.sha256; correctedManifestSha256 = $corrected.sha256; extraFile = $hidden; extraSha256 = ($corrected.files | Where-Object file -ceq $hidden).sha256; hiddenFixtureConsumedByQualifier = $false; consumerToolManifest = 'Generated independently from Version at Tools/test-g4-tool-package-contracts.ps1:960-982' } | ConvertTo-Json -Depth 6
