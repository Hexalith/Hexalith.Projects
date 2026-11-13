"""Fresh owned Aspire/DCP graph classification. Raw graph/environment/arguments never persisted."""
from pathlib import Path
import os,subprocess,time
from common import *

def session(not_before_epoch,launcher_pid,launcher_ticks,evidence):
 if not same(launcher_pid,launcher_ticks):return None
 eligible=[]
 for path in Path('/tmp').glob('supported-project-reads-private-*/apphost-session.json'):
  try:
   if path.is_symlink()or path.stat().st_uid!=os.geteuid()or path.stat().st_mtime<not_before_epoch-2:continue
   owned=read_json(path)
   if not isinstance(owned,dict):continue
   valid=True
   for key in ['appHost','cli']:
    pid=owned.get(key+'Pid');ticks=owned.get(key+'StartTicks')
    if type(pid)is not int or not str(ticks).isdigit()or int(ticks)<int(launcher_ticks)or not same(pid,ticks):valid=False;break
   if not valid:continue # Pre-existing/unrelated identities never have argv/env read.
   app=owned['appHostPid'];exe=Path('/proc',str(app),'exe').readlink()
   if not(exe==APPHOST or exe.name=='dotnet'and mapped(app,Path(str(APPHOST)+'.dll'))):continue
   cli=owned['cliPid'];cli_exe=Path('/proc',str(cli),'exe').readlink();pin=read_json(OPS/'launcher-plan.json')
   if cli_exe!=Path(pin['aspireExecutablePath'])or sha(cli_exe)!=pin['aspireExecutableSha256']:continue
   try:
    known_env(app,{'SUPPORTED_PROJECT_READ_EVIDENCE':str(evidence)})
    known_env(cli,{'SUPPORTED_PROJECT_READ_EVIDENCE':str(evidence)})
   except Guard:continue # Nonmatching contemporaneous runs never have private configuration read.
   config=Path(owned['_configuration']);directory=config.parent.lstat()
   if config.parent!=path.parent or not config.parent.name.startswith('supported-project-reads-private-')or not stat.S_ISDIR(directory.st_mode)or stat.S_ISLNK(directory.st_mode)or directory.st_uid!=os.geteuid()or stat.S_IMODE(directory.st_mode)!=0o700:raise Guard('owned-private-directory-invalid')
   if sha(config)!=owned['configurationSha256']:raise Guard('owner-config-hash-mismatch')
   known_env(app,{'Projects__SupportedReads__ConfigurationFile':str(config),'NAMESPACE':owned['_isolation'],'SUPPORTED_PROJECT_READ_EVIDENCE':str(evidence)})
   if not same(app,owned['appHostStartTicks']):continue
   eligible.append((path,owned))
  except(FileNotFoundError,ProcessLookupError):continue
 if len(eligible)>1:raise Guard('ambiguous-fresh-owned-session')
 return eligible[0]if eligible else None

def describe(owned,evidence):
 cli=owned['cliPid'];ticks=owned['cliStartTicks']
 if not same(cli,ticks):raise Guard('owned-cli-start-changed')
 exe=Path('/proc',str(cli),'exe').readlink();pin=read_json(OPS/'launcher-plan.json')
 if exe!=Path(pin['aspireExecutablePath'])or sha(exe)!=pin['aspireExecutableSha256']:raise Guard('owned-cli-executable-mismatch')
 known_env(cli,{'Projects__SupportedReads__ConfigurationFile':owned['_configuration'],'NAMESPACE':owned['_isolation'],'SUPPORTED_PROJECT_READ_EVIDENCE':str(evidence)})
 if not same(cli,ticks):raise Guard('owned-cli-start-changed')
 env=os.environ.copy();env['Projects__SupportedReads__ConfigurationFile']=owned['_configuration'];env['NAMESPACE']=owned['_isolation']
 command=[str(exe),'describe','--apphost',str(ROOT/'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'),'--format','Json','--non-interactive']
 result=subprocess.run(command,capture_output=True,env=env,timeout=15)
 if not same(cli,ticks):raise Guard('owned-cli-start-changed')
 if result.returncode:return None
 try:graph=json.loads(result.stdout)
 except ValueError:return None
 output={}
 for role in roles():
  items=[r for r in graph.get('resources',[])if r.get('displayName')==role['resource']]
  if len(items)>1:raise Guard('ambiguous-source-resource')
  if not items:return None
  properties=items[0].get('properties',{})
  if properties.get('project.path')!=role['sourceProject']:raise Guard('source-resource-project-mismatch')
  wrapper=properties.get('executable.pid');value=process(wrapper)if type(wrapper)is int else None
  if value is None:return None
  output[role['role']]={'wrapperPid':wrapper,'wrapperStartTicks':value['startTicks'],'sourceProjectMatches':True}
 return{'graphSha256':hashlib.sha256(result.stdout).hexdigest(),'appHostPid':owned['appHostPid'],'appHostStartTicks':owned['appHostStartTicks'],'configurationSha256':owned['configurationSha256'],'roles':output}

def target(role,owned,graph):
 if not same(owned['appHostPid'],owned['appHostStartTicks'])or not same(owned['cliPid'],owned['cliStartTicks']):raise Guard('owned-session-start-changed')
 if graph['appHostPid']!=owned['appHostPid']or str(graph['appHostStartTicks'])!=str(owned['appHostStartTicks'])or graph['configurationSha256']!=owned['configurationSha256']:raise Guard('graph-owned-session-mismatch')
 wrapper=graph['roles'][role['role']];pid=wrapper['wrapperPid']
 if not same(pid,wrapper['wrapperStartTicks']):raise Guard('owned-wrapper-start-changed')
 pending=[pid];seen=set();candidates=[]
 while pending:
  item=pending.pop()
  if item in seen:raise Guard('ambiguous-owned-ancestry')
  seen.add(item);current=process(item)
  if current is None:continue
  try:
   exe=Path('/proc',str(item),'exe').readlink()
   if exe==Path(role['nativeApphost'])or exe.name=='dotnet'and mapped(item,Path(role['entryDll'])):candidates.append((item,current['startTicks'],exe))
   pending.extend(int(v)for v in Path('/proc',str(item),'task',str(item),'children').read_text().split())
  except(FileNotFoundError,ProcessLookupError):continue
 if len(candidates)>1:raise Guard('ambiguous-owned-runtime')
 if not candidates:return None
 pid,ticks,exe=candidates[0];config=Path(owned['_configuration'])
 expected={'NAMESPACE':owned['_isolation'],'XDG_DATA_HOME':str(config.parent/('data-'+role['role'])),'EventStore__DomainService__AppId':role['role'],'ASPNETCORE_ENVIRONMENT':'Development',**FLAGS,'EventStore__RouteCatalog__ReuseHeldMetadataDispatch':'true'if role['role']=='folders'else'false'}
 known_env(pid,expected,absent=['DOTNET_PROCESSOR_COUNT'])
 if sha(config)!=owned['configurationSha256']or not same(pid,ticks)or not same(wrapper['wrapperPid'],wrapper['wrapperStartTicks']):raise Guard('owned-runtime-or-config-changed')
 return{'pid':pid,'startTicks':ticks,'executablePath':str(exe),'wrapperPid':wrapper['wrapperPid'],'wrapperStartTicks':wrapper['wrapperStartTicks']}

def identity(role,owned,graph,value,evidence,source_proof=None):
 fresh=target(role,owned,graph)
 if fresh!=value:raise Guard('owned-runtime-replaced-before-attach')
 files,proof=generation(role,value['pid'],value['startTicks'],evidence,source_proof)
 files +=[{'Label':'owner-config','Path':owned['_configuration'],'Sha256':owned['configurationSha256']},{'Label':'source-audit','Path':str(OPS/'source-controls.json'),'Sha256':sha(OPS/'source-controls.json')}]
 for index,(rel,digest)in enumerate(sorted(read_json(OPS/'source-controls.json')['sourceSha256'].items())):files.append({'Label':f'source-{index:03d}','Path':str(ROOT/rel),'Sha256':digest})
 # /proc/PID/exe is a symlink; hash only the resolved, already classified executable, with before/after start guards.
 if not same(value['pid'],value['startTicks']):raise Guard('owned-target-start-changed')
 exe=Path(value['executablePath']);exehash=sha(exe)
 if not same(value['pid'],value['startTicks']):raise Guard('owned-target-start-changed')
 token=role['nativeApphost']if exe==Path(role['nativeApphost'])else role['entryDll']
 return{'Role':role['role'],'Resource':role['resource'],'SourceProject':role['sourceProject'],'Pid':value['pid'],'StartTicks':int(value['startTicks']),'RequiredCommandToken':token,'ExecutableSha256':exehash,'Files':files},proof
