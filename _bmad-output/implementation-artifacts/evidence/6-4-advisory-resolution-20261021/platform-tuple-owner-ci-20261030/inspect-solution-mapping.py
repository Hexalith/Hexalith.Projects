#!/usr/bin/env python3
"""Inspect actual MSBuild reference assignment; does not restore or build assets."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

OUT = Path(__file__).resolve().parent
ROOT = Path('/tmp/story64-platform-tuple-candidate-20261030')
BUILD_ROOT = Path('/home/administrator/projects/hexalith/projects/references/Hexalith.Builds')
fixture = 'Hexalith.Platform.Secrets.ArchiveProcessFixture.csproj'
checks = {}
for stage in ['before', 'after']:
    output = (OUT / f'solution-configuration-{stage}.stdout').read_text()
    configuration = re.search(r'CurrentSolutionConfigurationContents = (<SolutionConfiguration>.*?</SolutionConfiguration>)', output, re.S).group(1)
    argv = ['dotnet', 'msbuild', 'tests/Hexalith.Platform.Secrets.Runtime.Tests/Hexalith.Platform.Secrets.Runtime.Tests.csproj',
            '-nologo', '-target:AssignProjectConfiguration', '-p:Configuration=Release', '-p:BuildingSolutionFile=true',
            '-p:UseHexalithProjectReferences=false', '-p:HexalithBuildsRoot=' + str(BUILD_ROOT),
            '-p:CurrentSolutionConfigurationContents=' + configuration, '-getItem:ProjectReferenceWithConfiguration']
    process = subprocess.run(argv, cwd=ROOT, text=True, capture_output=True)
    (OUT / f'assign-{stage}.stdout').write_text(process.stdout)
    (OUT / f'assign-{stage}.stderr').write_text(process.stderr)
    (OUT / f'assign-{stage}.command.json').write_text(json.dumps({'argv': argv, 'cwd': str(ROOT), 'exit_code': process.returncode,
       'purpose': 'Read-only MSBuild configuration assignment; no restore or compilation; existing sibling catalog used for project evaluation only.'}, indent=2) + '\n')
    assert process.returncode == 0, process.stderr
    items = json.loads(process.stdout)['Items']['ProjectReferenceWithConfiguration']
    item = next(x for x in items if x['Identity'].endswith(fixture))
    checks[stage] = {k: item.get(k) for k in ['Identity', 'SetConfiguration', 'SetPlatform', 'GlobalPropertiesToRemove']}
assert 'Configuration' in checks['before']['GlobalPropertiesToRemove']
assert checks['after']['SetConfiguration'] == 'Configuration=Release'
assert checks['after']['GlobalPropertiesToRemove'] is None or 'Configuration' not in checks['after']['GlobalPropertiesToRemove']
sdk = Path('/home/administrator/.dotnet/sdk/10.0.403/Microsoft.Common.CurrentVersion.targets')
checks['sdkAssignmentTargetSha256'] = hashlib.sha256(sdk.read_bytes()).hexdigest()
checks['releaseAssetsBuiltLocally'] = False
(OUT / 'solution-mapping-result.json').write_text(json.dumps(checks, indent=2) + '\n')
print(json.dumps(checks, indent=2))
