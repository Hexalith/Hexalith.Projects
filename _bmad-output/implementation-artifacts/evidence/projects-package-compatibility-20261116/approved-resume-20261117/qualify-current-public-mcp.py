"""Consume the signed public prerequisite at the current Projects McpCli pin."""
from pathlib import Path
import os,subprocess,json,hashlib,datetime,importlib.util,sys,time
OUT=Path(__file__).resolve().parent/'current-public-mcp';OUT.mkdir(exist_ok=False);R=OUT.parent;ROOT=Path(__file__).resolve().parents[5];SCRATCH=Path(os.environ['TMPDIR']);WORK=SCRATCH/'workspace';WORK.mkdir()
state=json.loads((R/'active-owner-source-workspace.json').read_text());spec=importlib.util.spec_from_file_location('runtime',Path(state['work'])/'tools/delivery_runtime.py');rt=importlib.util.module_from_spec(spec);sys.modules[spec.name]=rt;spec.loader.exec_module(rt);reader=rt.Git(SCRATCH/'projects.git');reader.init();source=rt.Transport().module(reader,'Hexalith.Projects');assert source=='78633a653c1a6768a795678203d836b3caac2c12','Source advanced; qualify its exact current graph first';entries=reader.items(source)
record={'kind':'current-source-public-feed-qualification','source':source,'started_at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'feed':'https://api.nuget.org/v3/index.json','commands':[],'sources':{},'nested_submodule_initialization':False,'hosted_acceptance':False}
def save():(OUT/'qualification.json').write_text(json.dumps(record,indent=2)+'\n')
for name in ['Hexalith.McpCli','Hexalith.Builds']:
 revision=entries['references/'+name][2];destination=WORK/'references'/name;destination.parent.mkdir(parents=True,exist_ok=True)
 for command in [['git','clone','--shared','--no-checkout',str(ROOT/'references'/name),str(destination)],['git','-C',str(destination),'remote','set-url','origin','https://github.com/Hexalith/'+name+'.git'],['git','-C',str(destination),'-c','submodule.recurse=false','checkout','--quiet','--detach',revision]]:
  result=subprocess.run(command,capture_output=True,timeout=120);assert result.returncode==0,result.stderr.decode()
 record['sources'][name]={'revision':revision,'tree':subprocess.check_output(['git','rev-parse','HEAD^{tree}'],cwd=destination,text=True).strip(),'origin':'https://github.com/Hexalith/'+name+'.git'}
config=WORK/'NuGet.Config';config.write_text('<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3"/></packageSources></configuration>\n')
if (WORK/'references/Hexalith.McpCli/global.json').exists():(WORK/'global.json').write_bytes((WORK/'references/Hexalith.McpCli/global.json').read_bytes())
env={k:v for k,v in os.environ.items() if k not in ['GH_TOKEN','GITHUB_TOKEN','CI'] and not k.startswith('NuGetPackageSourceCredentials_')};env.update(NUGET_PACKAGES=str(SCRATCH/'packages'),NUGET_HTTP_CACHE_PATH=str(SCRATCH/'http-cache'),NUGET_SCRATCH=str(SCRATCH/'nuget-scratch'),DOTNET_CLI_HOME=str(SCRATCH/'dotnet-home'),DOTNET_CLI_USE_MSBUILD_SERVER='0');project='references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj'
commands=[('restore',['dotnet','restore',project,'-p:UseHexalithProjectReferences=true','-p:HexalithConversationsFromSource=false','--configfile',str(config)]),('release-build',['dotnet','build',project,'--no-restore','--configuration','Release','-warnaserror','-m:1','--disable-build-servers']),('strict-projects-discovery',['dotnet','references/Hexalith.McpCli/src/Hexalith.McpCli/bin/Release/net11.0/Hexalith.McpCli.dll','operations','projects','--strict'])]
for name,cmd in commands:
 start=time.monotonic()
 with (OUT/(name+'.log')).open('wb') as log:v=subprocess.run(cmd,cwd=WORK,env=env,stdout=log,stderr=subprocess.STDOUT,timeout=240)
 record['commands'].append({'name':name,'command':cmd,'exit':v.returncode,'seconds':round(time.monotonic()-start,3),'log_sha256':hashlib.sha256((OUT/(name+'.log')).read_bytes()).hexdigest()});save();print(name,v.returncode,flush=True)
 if v.returncode:break
assets=WORK/'references/Hexalith.McpCli/src/Hexalith.McpCli/obj/project.assets.json'
if assets.exists():
 value=json.loads(assets.read_text());record['projects_contracts']=value['libraries'].get('Hexalith.Projects.Contracts/2.0.0-story66.3');record['resolved_packages']=sorted(k for k,v in value['libraries'].items() if v['type']=='package')
public=SCRATCH/'packages/hexalith.projects.contracts/2.0.0-story66.3/hexalith.projects.contracts.2.0.0-story66.3.nupkg'
if public.exists():
 digest=hashlib.sha256(public.read_bytes()).hexdigest();record['restored_public_archive_sha256']=digest;assert digest=='81888d8267dbf6593ca2cfd8c63bdd6da2732351a67400602058612dc70e5b8b'
record['all_commands_passed']=len(record['commands'])==3 and all(c['exit']==0 for c in record['commands']);record['status']='passed' if record['all_commands_passed'] else 'failed';record['ended_at']=datetime.datetime.now(datetime.timezone.utc).isoformat();save();raise SystemExit(not record['all_commands_passed'])
