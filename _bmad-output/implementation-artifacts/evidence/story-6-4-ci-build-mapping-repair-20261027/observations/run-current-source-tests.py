import json,os,subprocess,time
from pathlib import Path
e=Path('/tmp/story64-ci-repair-20261027'); root=e/'source-workspace/references/Hexalith.Conversations'; env=os.environ.copy(); env.update(json.loads((e/'verified-source-prepare.json').read_text())['buildPreparation']['environment'])
checks=[('current-source-mapping-tests',['dotnet','tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0/Hexalith.Conversations.Contracts.Tests.dll','-class','Hexalith.Conversations.Contracts.Tests.PlatformEnrollmentMappingTest','-result-xml',str(e/'current-source-mapping-tests.xml'),'-noLogo']),('current-source-server-build',['dotnet','build','tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj','-c','Debug','-m:1','-p:NuGetAudit=false','-p:MinVerVersionOverride=1.0.0'])]
receipts=[]
for label,args in checks:
 t=time.time()
 with (e/(label+'.log')).open('w') as log: result=subprocess.run(args,cwd=root,env=({name: value for name, value in env.items() if name not in json.loads((e/'verified-source-prepare.json').read_text())['buildPreparation']['environment']} if label.endswith('tests') else env),stdout=log,stderr=subprocess.STDOUT)
 receipts.append(dict(label=label,command=args,cwd=str(root),exit=result.returncode,seconds=round(time.time()-t,3)))
 (e/'current-source-checks.json').write_text(json.dumps(receipts,indent=2)+'\n')
 if result.returncode: raise SystemExit(result.returncode)
