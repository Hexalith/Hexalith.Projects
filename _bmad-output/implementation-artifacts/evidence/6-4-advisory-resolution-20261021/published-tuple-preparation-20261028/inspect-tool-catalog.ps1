param([string]$ToolingAssembly)
$assembly = [System.Reflection.Assembly]::LoadFile($ToolingAssembly)
$stream = $assembly.GetManifestResourceStream("Hexalith.Builds.Tooling.Manifest.platform-version-catalog.json")
$reader = [System.IO.StreamReader]::new($stream)
try { $reader.ReadToEnd() } finally { $reader.Dispose() }
