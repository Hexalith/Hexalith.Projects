"""Root-owned full verification using the existing owned private source and Dapr session."""
import argparse,json,os,subprocess
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--rollback',action='store_true');args=parser.parse_args()
out=Path(__file__).resolve().parent
context=json.loads((out/'runner-context.json').read_text());root=Path(context['sourceRoot']);scratch=Path(context['scratchRoot']);artifacts=Path(context['artifactsPath'])
env=os.environ.copy();env.update(json.loads((out/'private-source-mapping.log').read_text())['buildPreparation']['environment']);env.update(context['environment'])
env.update(TMPDIR=str(scratch),TMP=str(scratch),TEMP=str(scratch))
commands=[]
def run(label,command):
 path=out/(label+'.log');assert not path.exists(),path
 with path.open('w') as log:result=subprocess.run(command,cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT)
 commands.append({'label':label,'command':command,'cwd':str(root),'exitCode':result.returncode})
 target=out/('enclosing-rollback-commands.json' if args.rollback else 'enclosing-full-commands.json');target.write_text(json.dumps(commands,indent=2)+'\n')
 print(label,'exit',result.returncode,flush=True);print(path.read_text()[-1500:],flush=True)
 if result.returncode:raise SystemExit(result.returncode)
for suite in (['Rollback'] if args.rollback else ['Domain','Contracts','Client','UI','Server']):
 label=suite+'-final-1';name='Hexalith.Projects'+('' if suite=='Domain' else '.Server' if suite=='Rollback' else '.'+suite)+'.Tests'
 env.update(STORY67_AUTHENTICATED_OBSERVATIONS=str(out/(label+'-observations.json')),STORY67_MATRIX_OBSERVATIONS=str(out/(label+'-matrix.json')))
 run(label+'-build',['dotnet','build','tests/'+name+'/'+name+'.csproj','--configuration','Debug','-m:1','--disable-build-servers','--artifacts-path',str(artifacts)])
 command=['dotnet',str(artifacts/'bin'/name/'debug'/(name+'.dll')),'-xml',str(out/(label+'.xml'))]
 if args.rollback:command+=['-method','Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests.NativeConversationMembershipUsesTheRealGatewayOwnerAndPersistedNativeSource']
 run(label+'-tests',command)
print('ALL JOBS PASSED',flush=True)
