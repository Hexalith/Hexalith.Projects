#!/usr/bin/env python3
"""Exercise the actual SDK mapping task without SDK project initial restore targets."""
import json
from pathlib import Path
import re
import subprocess

OUT = Path(__file__).resolve().parent
checks = {}
for stage in ['before', 'after']:
    output = (OUT / f'solution-configuration-{stage}.stdout').read_text()
    configuration = re.search(r'CurrentSolutionConfigurationContents = (<SolutionConfiguration>.*?</SolutionConfiguration>)', output, re.S).group(1)
    argv = ['dotnet', 'msbuild', str(OUT / 'reference-mapping.proj'), '-nologo', '-target:Inspect',
            '-p:CurrentSolutionConfigurationContents=' + configuration, '-getItem:Mapped']
    process = subprocess.run(argv, text=True, capture_output=True)
    (OUT / f'task-assign-{stage}.stdout').write_text(process.stdout)
    (OUT / f'task-assign-{stage}.stderr').write_text(process.stderr)
    (OUT / f'task-assign-{stage}.command.json').write_text(json.dumps({'argv': argv, 'cwd': str(Path.cwd()),
       'exit_code': process.returncode, 'purpose': 'Local SDK configuration-task diagnostic only; no restore or asset build.'}, indent=2) + '\n')
    assert process.returncode == 0, process.stderr
    item = json.loads(process.stdout)['Items']['Mapped'][0]
    checks[stage] = {k: item.get(k) for k in ['Identity', 'SetConfiguration', 'SetPlatform', 'GlobalPropertiesToRemove']}
assert 'Configuration' in checks['before']['GlobalPropertiesToRemove']
assert checks['after']['SetConfiguration'] == 'Configuration=Release'
assert not checks['after']['GlobalPropertiesToRemove'] or 'Configuration' not in checks['after']['GlobalPropertiesToRemove']
(OUT / 'solution-mapping-result.json').write_text(json.dumps(checks, indent=2) + '\n')
print(json.dumps(checks, indent=2))
