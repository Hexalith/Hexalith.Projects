from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch
import importlib.util,json,tempfile,shutil,hashlib,datetime,os
BASE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('watcher_smoke_module',BASE/'watch_live.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
actual=json.loads(Path('/tmp/story65-release-binding-allocation-wrapper-resource.json').read_text())
owned={'appHostPid':2240707,'appHostStartTicks':'6732227','cliPid':2233934,'cliStartTicks':'6727265','configurationSha256':'a'*64,'_configuration':'/tmp/owned-smoke-only/config.json','_isolation':'owned-smoke-only'}
wrapper=actual['wrapperPid'];wrapper_ticks=actual['wrapperStartTicks'];target=99991
schema={'resources':[{'displayName':'eventstore','properties':{'project.path':str(m.OWNER/'src/Hexalith.EventStore/Hexalith.EventStore.csproj'),'executable.pid':wrapper}}]}
starts={owned['cliPid']:(owned['cliStartTicks'],1),wrapper:(wrapper_ticks,1),target:('9999999',wrapper)}
results=[]
def process(pid):return starts.get(pid)
def described(graph):return SimpleNamespace(returncode=0,stdout=json.dumps(graph).encode())
def source_case(name,graph,expected=None):
 with patch.object(m,'process',side_effect=process),patch.object(Path,'readlink',return_value=Path('/tmp/owned-smoke-aspire')),patch.object(m.subprocess,'run',return_value=described(graph)) as run:
  try:value=m.source_resource(owned)
  except m.Guard as error:
   assert str(error)==expected;results.append({'case':name,'passed':True,'fixedReason':str(error)});return
  assert expected is None and value is not None and value['wrapperPid']==wrapper
  argv=run.call_args.args[0];env=run.call_args.kwargs['env'];assert argv[1]=='describe' and env['Projects__SupportedReads__ConfigurationFile']==owned['_configuration'] and env['NAMESPACE']==owned['_isolation']
  results.append({'case':name,'passed':True,'onlyMockedDescribe':True})
  return value
resource=source_case('unique-actual-resource-schema',schema)
wrong=json.loads(json.dumps(schema));wrong['resources'][0]['properties']['project.path']='/tmp/different.csproj';source_case('wrong-source-project-rejected',wrong,'owned-resource-source-project-mismatch')
ambiguous=json.loads(json.dumps(schema));ambiguous['resources']*=2;source_case('ambiguous-eventstore-rejected',ambiguous,'ambiguous-owned-resource-graph')
seen=[];real_readlink=Path.readlink;real_text=Path.read_text

def fake_readlink(path):
 text=str(path)
 if text==f'/proc/{wrapper}/exe':return Path('/tmp/owned-smoke-wrapper')
 if text==f'/proc/{target}/exe':return m.ENTRY
 raise AssertionError('unexpected process metadata read')
def fake_text(path,*args,**kwargs):
 text=str(path);seen.append(text)
 if text==f'/proc/{wrapper}/task/{wrapper}/children':return str(target)
 if text==f'/proc/{target}/task/{target}/children':return ''
 return real_text(path,*args,**kwargs)
with patch.object(m,'process',side_effect=process),patch.object(Path,'readlink',new=fake_readlink),patch.object(Path,'read_text',new=fake_text):
 value=m.runtime(owned,resource);assert value[0]==target;assert not any(f'/proc/{p}/task/' in text for p in (owned['appHostPid'],owned['cliPid']) for text in seen)
 results.append({'case':'detached-dcp-wrapper-descendant-found-without-apphost-os-ancestry','passed':True})
positive={'multiple':True,'attempt':1,'result':'MultipleCandidates','incomplete':False,'candidateCount':2,'positiveCandidatesVerified':True,'elapsedMilliseconds':1.0}
failed={'multiple':True,'attempt':1,'result':None,'incomplete':True,'candidateCount':0,'positiveCandidatesVerified':False,'elapsedMilliseconds':2.0}
parent=Path(tempfile.mkdtemp(prefix='story65-watcher-regression-smoke-'));parent.chmod(0o700)
try:
 def main_case(name,observations,expect,allow_identity):
  directory=parent/name;directory.mkdir();evidence=directory/'ordinary';evidence.mkdir();output=directory/'watch'
  assert not(evidence/'run.json').exists()
  args=SimpleNamespace(watch_output=output,timeout=2,not_before_epoch=datetime.datetime.now(datetime.timezone.utc).timestamp(),evidence=evidence,wrapper=BASE/'run_probe.py',tool=BASE/'bin/Debug/net10.0/AllocationProbe.dll',owner_receipt=BASE/'mock-owner.json',owner_receipt_sha256='0'*64,gateway_sha256='1'*64)
  phases=[];old_write=m.write
  def record(path,value):
   if path.name=='watcher-receipt.json':phases.append(value['phase'])
   old_write(path,value)
  fake_identity={'Role':'gateway','Resource':'eventstorehost','Pid':target,'StartTicks':9999999,'Files':[{}]*45}
  with patch.object(m,'first_multiple',side_effect=observations),patch.object(m,'owned_session',return_value=(Path('/tmp/mock-ownership'),owned)) as own,patch.object(m,'source_resource',return_value=resource) as graph,patch.object(m,'runtime',return_value=(target,'9999999',wrapper,m.ENTRY)),patch.object(m,'identity',return_value=fake_identity) as ident,patch.object(m,'write',side_effect=record),patch.object(m.subprocess,'run',return_value=SimpleNamespace(returncode=0)) as external:
   m.main(args);receipt=json.loads((output/'watcher-receipt.json').read_text());assert receipt['phase']==expect
   if not allow_identity:assert ident.call_count==0 and external.call_count==0 and own.call_count==0
   if expect=='profile-finished':assert graph.call_count==1 and external.call_count==1 and phases.index('ready-waiting-first-multiple')<phases.index('failed-multiple-profile-requested') and receipt['privateIdentityRemoved']
   assert not(evidence/'run.json').exists()
   results.append({'case':name,'passed':True,'endReportAbsent':True,'realSubprocesses':0,'targetAttachments':0,'phaseSequence':phases})
 main_case('absent-end-report-detached-resource-ready-then-failed-gate',[None,failed],'profile-finished',True)
 main_case('genuine-first-multiple-positive-skips-all-profiler-identity-and-execution',[positive],'skipped-positive-multiple',False)
 main_case('failed-first-multiple-before-published-ready-refuses-attachment',[failed],'guard-refused-no-attach',False)
finally:shutil.rmtree(parent)
old=[]
for pid,ticks in [(2212154,'6710660'),(2238966,'6730864'),(2245892,'6734576')]:
 value=m.process(pid);absent=value is None or value[0]!=ticks;assert absent;old.append({'pid':pid,'recordedStartTicks':ticks,'recordedIdentityAbsent':absent})
receipt={'scratchDiagnosticRegressionOnly':True,'watcherSha256':hashlib.sha256((BASE/'watch_live.py').read_bytes()).hexdigest(),'actualSanitizedResourceProofSha256':hashlib.sha256(Path('/tmp/story65-release-binding-allocation-wrapper-resource.json').read_bytes()).hexdigest(),'allPassed':all(x['passed'] for x in results),'cases':results,'noRealSubprocessStartedBySmoke':True,'noTargetAttached':True,'noNativeOrBrowserAcceptanceCases':True,'allOldWatchersAbsent':old}
(BASE/'watcher-regression-smoke.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt,indent=2))
