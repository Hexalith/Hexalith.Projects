"""External same-body metrics observation. All target/session/graph/config identities are RAM-only."""
from common import *
import argparse,datetime


def session(args,pins,identity):
 if not same(args.launcher_pid,args.launcher_start):return None
 selected=[];apphost=ROOT/'src/Hexalith.Projects.AppHost/bin/Debug/net10.0/Hexalith.Projects.AppHost'
 for path in Path('/tmp').glob('supported-project-reads-private-*/apphost-session.json'):
  try:
   info=path.lstat();parent=path.parent.lstat()
   if not stat.S_ISREG(info.st_mode)or info.st_uid!=os.getuid()or stat.S_IMODE(info.st_mode)!=0o600 or info.st_size>65536 or info.st_mtime<args.not_before_epoch-2:continue
   if not stat.S_ISDIR(parent.st_mode)or parent.st_uid!=os.getuid()or stat.S_IMODE(parent.st_mode)!=0o700:continue
   owned=read(path)
   if any(type(owned.get(k+'Pid'))is not int or not str(owned.get(k+'StartTicks')).isdigit()or int(owned[k+'StartTicks'])<int(args.launcher_start)or not same(owned[k+'Pid'],owned[k+'StartTicks'])for k in ['appHost','cli']):continue
   app=owned['appHostPid'];cli=owned['cliPid'];app_exe=Path('/proc',str(app),'exe').readlink();cli_exe=Path('/proc',str(cli),'exe').readlink()
   if not(app_exe==apphost or app_exe==Path(pins['dotnetExecutablePath'])and mapped(app,Path(str(apphost)+'.dll'))):continue
   if cli_exe!=Path(pins['aspireExecutablePath'])or sha(cli_exe)!=pins['aspireExecutableSha256']:continue
   # Only classified fresh source AppHost/CLI processes have known environment keys read.
   config=Path(owned['_configuration'])
   if config.parent!=path.parent or sha(config)!=owned['configurationSha256']:continue
   for pid in [app,cli]:
    raw=Path('/proc',str(pid),'environ').read_bytes().split(b'\0')
    expected={'Projects__SupportedReads__ConfigurationFile':str(config),'NAMESPACE':owned['_isolation'],'SUPPORTED_PROJECT_READ_EVIDENCE':str(args.evidence)}
    if any(raw.count((key+'='+value).encode())!=1 for key,value in expected.items()):break
   else:
    owned['_sessionPath']=path;owned['_sessionStat']=identity.stat_key(info);owned['_configStat']=identity.stat_key(config.lstat());owned['_appExe']=app_exe;owned['_appExeStat']=identity.stat_key(app_exe.stat());owned['_cliExeStat']=identity.stat_key(cli_exe.stat())
    owned['_appExeSha256']=sha(app_exe);owned['_appDllSha256']=sha(Path(str(apphost)+'.dll'))
    if not same(app,owned['appHostStartTicks'])or not same(cli,owned['cliStartTicks']):continue
    selected.append(owned)
  except(FileNotFoundError,ProcessLookupError,KeyError,ValueError):continue
 require(len(selected)<=1,'ambiguous-owned-session');return selected[0]if selected else None


def session_guard(args,owned,pins,identity):
 require(same(args.launcher_pid,args.launcher_start),'ordinary-launcher-closed')
 for key in ['appHost','cli']:require(same(owned[key+'Pid'],owned[key+'StartTicks']),'owned-session-start-changed')
 require(identity.stat_key(owned['_sessionPath'].lstat())==owned['_sessionStat']and identity.stat_key(Path(owned['_configuration']).lstat())==owned['_configStat'],'owned-session-file-changed')
 require(Path('/proc',str(owned['appHostPid']),'exe').readlink()==owned['_appExe']and identity.stat_key(owned['_appExe'].stat())==owned['_appExeStat'],'owned-apphost-executable-changed')
 cli=Path(pins['aspireExecutablePath']);require(Path('/proc',str(owned['cliPid']),'exe').readlink()==cli and identity.stat_key(cli.stat())==owned['_cliExeStat'],'owned-cli-executable-changed')
 return True


def describe(args,owned,pins,identity):
 session_guard(args,owned,pins,identity)
 env=os.environ.copy();env['Projects__SupportedReads__ConfigurationFile']=owned['_configuration'];env['NAMESPACE']=owned['_isolation']
 command=[pins['aspireExecutablePath'],'describe','--apphost',str(ROOT/'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'),'--format','Json','--non-interactive']
 result=subprocess.run(command,capture_output=True,env=env,timeout=15)
 session_guard(args,owned,pins,identity);require(result.returncode==0 and len(result.stdout)<=8*1024*1024,'owned-graph-unobservable')
 graph=json.loads(result.stdout);require(isinstance(graph,dict)and isinstance(graph.get('resources'),list)and len(graph['resources'])<=512,'owned-graph-schema')
 for role,project in RUNTIME_PROJECTS.items():
  rows=[r for r in graph['resources']if r.get('displayName')==role]
  require(len(rows)==1 and rows[0].get('properties',{}).get('project.path')==str(ROOT/project),'owned-graph-source-project')
 return graph


def collect(args,pins):
 # Imports happen only after Root release validation. No monkeypatching or launcher main/execute/run calls.
 sys.path.insert(0,str(CORE))
 import sidecar_identity as identity
 import collect_window as collector
 launcher=load(LAUNCHER,'ordinary_receiver_configurations_readonly')
 require(launcher.RUNTIME_PROJECTS==RUNTIME_PROJECTS,'original-six-source-projects-changed')
 owned=session(args,pins,identity);require(owned is not None,'owned-session-unavailable')
 guard=lambda:session_guard(args,owned,pins,identity)
 graph=describe(args,owned,pins,identity)
 modes=launcher.observe_runtime_modes(graph,owned);launcher.require_runtime_modes(modes)
 before=ordinary_current();source_before=full_source(args.evidence)
 require(len(before['generations']['currentApp147Sha256'])+len(before['generations']['additionalHexalithAssemblyFilesSha256'])==149,'ordinary-app149-census')
 configuration=Path(owned['_configuration']);private=configuration.parent;registry=private/'discovery.sqlite'
 configurations,receipts=launcher.receiver_configurations(private/'discovery.json',registry,owned['_isolation'])
 require(set(configurations)==set(RUNTIME_PROJECTS),'original-config-six-role-set')
 binary=None;binary_closed=False;identities={};availability=[];postchecks={'passed':False}
 try:
  binary=identity.open_binary(pins['daprdSha256']);rows=identity.proc_snapshot()
  for role in sorted(RUNTIME_PROJECTS):
   path=configurations[role];record={'kind':'owned-dapr-runtime-configuration','catalogDirectory':receipts['catalogDirectory'],'path':str(path),'sha256':receipts['identities'][str(path)]['sha256']}
   try:
    identities[role]=identity.classify(owned,graph,role,record,binary,guard,rows)
    availability.append({'role':role,'category':'owned-sidecar-and-listener-classified'})
   except identity.Refused as refusal:
    reason=refusal.args[0]if len(refusal.args)==1 and refusal.args[0]in collector.REASONS else'collector-guard-fault'
    availability.append({'role':role,'category':reason})
  rows=None;graph=None
  # Trigger is re-evaluated after ALL slow graph/file/hash/proc preflight and before any HTTP.
  require(decision(observations(args.evidence))=='failed-window','trigger-no-longer-eligible')
  def live():return guard()is True and all(same(v['pid'],v['startTicks'])and same(v['wrapperPid'],v['wrapperStartTicks'])for v in modes.values())
  def refresh(role):return identity.recheck(identities[role],owned,binary,guard)
  def postflight():
   nonlocal binary_closed
   passed=False
   try:
    guard()
    for role in identities:refresh(role)
    current_configs,current_receipts=launcher.receiver_configurations(private/'discovery.json',registry,owned['_isolation'])
    require(current_configs==configurations and current_receipts==receipts,'original-config-receipt-changed')
    require(ordinary_current()==before and full_source(args.evidence)==source_before,'post-window-source-or-generation-changed')
    require(sha(Path(pins['aspireExecutablePath']))==pins['aspireExecutableSha256']and sha(owned['_appExe'])==owned['_appExeSha256']and sha(Path(str(ROOT/'src/Hexalith.Projects.AppHost/bin/Debug/net10.0/Hexalith.Projects.AppHost')+'.dll'))==owned['_appDllSha256'],'post-window-session-executable-changed')
    guard();passed=True
   finally:
    try:binary_ok=identity.finish_binary(binary)
    finally:binary_closed=True
    postchecks['passed']=passed and binary_ok
   return postchecks['passed']
  value=collector.collect_window(identities,refresh,live,postflight)
  # Projection only: core11 stays byte-exact. Its elapsed includes the postflight callback.
  value['collectorElapsedIncludingPostflightMilliseconds']=value.pop('httpWindowElapsedMilliseconds')
  return value,{'classifiedRoles':len(identities),'roleAvailability':availability,'source65Match':True,'fullSourceManifestCount':source_before[1],'app149CurrentAtPreflight':True,'graphSourceRolesVerifiedCount':6,'nativeSourceRuntimeRolesVerifiedCount':len(modes),'originalReceiverConfigurationRolesVerifiedCount':len(configurations),'postflightSourcesGenerationsConfigurationAndBinaryAccepted':postchecks['passed'],'privateGraphIdentitiesConfigsEndpointsAndLabelsPersisted':False}
 finally:
  if binary is not None and not binary_closed:
   try:identity.finish_binary(binary)
   finally:binary_closed=True


def main(args):
 os.umask(0o077);pins=verify_preparation(args.release,args.release_sha256,released=True)
 require(args.evidence==LIVE and args.output==OUTPUT,'fixed-integration-output-required');args.output.mkdir(mode=0o700,exist_ok=False)
 receipt={'diagnosticOnly':True,'featureAcceptance':False,'phase':'waiting-for-completed-failed-multiple','targetSignalsSent':False,'coreCollectionCompleted':False,'rawPrivateRuntimeValuesPersisted':False,'profilerAttached':False};deadline=time.monotonic()+1100
 try:
  while time.monotonic()<deadline:
   if not same(args.launcher_pid,args.launcher_start):receipt['phase']='ordinary-launcher-closed';break
   rows=observations(args.evidence);choice=decision(rows)
   if choice in ['positive-skip','late-skip']:receipt['phase']=choice;break
   if choice=='failed-window':
    value,proof=collect(args,pins);write(args.output/'metrics-window.json',value);receipt.update(phase='metrics-window-closed',coreCollectionCompleted=True,guardProof=proof,wholeWindowPostflightAccepted=value['wholeWindowPostflightAccepted']);break
   completed=read(args.evidence/'run.json')if(args.evidence/'run.json').is_file()else None
   if isinstance(completed,dict)and completed.get('result')in ['failed','passed']:receipt['phase']='ordinary-run-ended-before-window';break
   time.sleep(.2)
  else:receipt['phase']='external-watcher-deadline'
 except Exception:
  # No raw exception messages, process/config/socket coordinates or scrape bodies cross this boundary.
  receipt.update(phase='external-guard-refused',fixedReason='observer-guard-or-ordinary-session-unobservable')
 finally:write(args.output/'watcher-receipt.json',receipt)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--release',type=Path,required=True);p.add_argument('--release-sha256',required=True);p.add_argument('--evidence',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--not-before-utc',required=True);p.add_argument('--launcher-pid',type=int,required=True);p.add_argument('--launcher-start',required=True);a=p.parse_args();a.not_before_epoch=datetime.datetime.fromisoformat(a.not_before_utc.replace('Z','+00:00')).timestamp();main(a)
