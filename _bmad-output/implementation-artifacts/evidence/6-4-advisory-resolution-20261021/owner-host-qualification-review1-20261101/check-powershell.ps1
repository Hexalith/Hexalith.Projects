$tokens = $null
$errors = $null
[void][System.Management.Automation.Language.Parser]::ParseFile('/home/administrator/projects/hexalith/projects/references/Hexalith.Folders/tests/tools/run-production-observability-gates.ps1', [ref] $tokens, [ref] $errors)
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Output 'Production observability gate PowerShell syntax: valid'
