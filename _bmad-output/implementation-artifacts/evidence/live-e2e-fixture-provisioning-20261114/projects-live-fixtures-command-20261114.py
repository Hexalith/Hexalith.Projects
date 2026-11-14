import json, os, subprocess, sys
from pathlib import Path
root=Path('/home/administrator/projects/hexalith/projects')
env=dict(os.environ)
env.update(json.loads(Path('/tmp/projects-live-fixtures-source-20261114.json').read_text())['buildPreparation']['environment'])
for name in ('EventStore','Tenants','Folders','Commons','Conversations','Memories','FrontComposer','Parties','McpCli','Builds','Platform'):
    env['Hexalith'+name+'Root']=str(root/'references'/('Hexalith.'+name))
env.update(UseHexalithProjectReferences='true',UseNuGetDeps='false',HexalithTenantsBasePath=str(root/'references/Hexalith.Tenants/src'))
raise SystemExit(subprocess.run(sys.argv[1:],env=env,cwd=root).returncode)
