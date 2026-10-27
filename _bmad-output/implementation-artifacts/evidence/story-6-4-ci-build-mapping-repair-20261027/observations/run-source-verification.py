import json,os,subprocess,time
from pathlib import Path
evidence=Path('/tmp/story64-ci-repair-20261027')
workspace=evidence/'source-workspace/references/Hexalith.Conversations'
prepared=json.loads((evidence/'verified-source-prepare.json').read_text())
assert prepared['status']=='prepared'
environment=os.environ.copy(); environment.update(prepared['buildPreparation']['environment'])
command=['dotnet','build','tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj','-c','Debug','-m:1','-p:NuGetAudit=false','-p:MinVerVersionOverride=1.0.0']
start=time.time()
with (evidence/'current-source-contracts-build.log').open('w') as log: result=subprocess.run(command,cwd=workspace,env=environment,stdout=log,stderr=subprocess.STDOUT)
receipt=dict(command=command,cwd=str(workspace),environment=prepared['buildPreparation']['environment'],exit=result.returncode,seconds=round(time.time()-start,3))
(evidence/'current-source-contracts-build.json').write_text(json.dumps(receipt,indent=2)+'\n')
raise SystemExit(result.returncode)
