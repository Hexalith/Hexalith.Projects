"""Pure offline guard/control-flow tests. No app/tool loading, profiling, live processes or target signals."""
from pathlib import Path
from unittest.mock import patch
import argparse,copy,io,json,os,stat,tempfile,time,types
import common,ownership,coordinate,run_one

class Child:
 next_pid=700000
 def __init__(self,command,events):
  Child.next_pid+=1;self.pid=Child.next_pid;self.command=command;self.returncode=None;self.stdout=io.BytesIO();self.stderr=io.BytesIO();self.events=events;events.append(('spawn',command[command.index('--stage')+1]if'--stage'in command else'child',self.pid))
 def poll(self):return self.returncode
 def wait(self,timeout=None):self.returncode=0 if self.returncode is None else self.returncode;self.events.append(('wait',self.pid));return self.returncode
 def terminate(self):raise AssertionError('PidfdRequiredInSmoke')
 def kill(self):raise AssertionError('PidfdRequiredInSmoke')

def main(output):
 results=[]
 def check(name,callback):
  try:callback();results.append({'case':name,'passed':True})
  except Exception as error:results.append({'case':name,'passed':False,'errorType':type(error).__name__})
 def require(value):assert value
 base={'multiple':True,'attempt':1,'httpStatus':200,'incomplete':True,'candidateCount':0,'positiveCandidatesVerified':False,'elapsedMilliseconds':2001.0,'result':None,'actualPublicGateway':True,'fabricatedProjectionWrite':False,'nonBootstrapCredential':True,'runtimeAuthoritySupplied':False}
 def parsed(rows):
  with tempfile.TemporaryDirectory(prefix='story65-receiver-guard-')as tmp:
   p=Path(tmp);(p/'browser-advisory-seed-readiness.jsonl').write_text(''.join(json.dumps(x)+'\n'for x in rows));return common.observations(p)
 check('genuine-completed-http200-failure',lambda:require(common.decision(parsed([base]))=='failed-attach'))
 positive={**base,'result':'MultipleCandidates','incomplete':False,'candidateCount':2,'positiveCandidatesVerified':True}
 check('genuine-positive-skips-all',lambda:require(common.decision(parsed([base,positive]))=='positive-skip-all'))
 check('insufficient-three-windows-skips-all',lambda:require(common.decision(parsed([{**base,'attempt':28}]))=='insufficient-windows-skip-all'))
 invalid=[('missing-result',lambda x:x.pop('result')),('wrong-status',lambda x:x.update(httpStatus=503)),('boolean-status',lambda x:x.update(httpStatus=True)),('float-attempt',lambda x:x.update(attempt=1.0)),('zero-attempt',lambda x:x.update(attempt=0)),('negative-count',lambda x:x.update(candidateCount=-1)),('nan-elapsed',lambda x:x.update(elapsedMilliseconds=float('nan'))),('not-public-gateway',lambda x:x.update(actualPublicGateway=False)),('fabricated-write',lambda x:x.update(fabricatedProjectionWrite=True)),('bootstrap-credential',lambda x:x.update(nonBootstrapCredential=False)),('authority-supplied',lambda x:x.update(runtimeAuthoritySupplied=True))]
 for name,mutate in invalid:
  def test(mutate=mutate):
   row=dict(base);mutate(row);require(parsed([row])==[])
  check('ignore-'+name,test)
 with tempfile.TemporaryDirectory(prefix='story65-receiver-source-guard-')as tmp:
  root=Path(tmp)/'root';root.mkdir();ops=Path(tmp)/'ops';ops.mkdir();data={}
  for i in range(55):
   f=root/f'source{i}.cs';f.write_text('synthetic-source');data[f.name]=common.sha(f)
  source=ops/'source-controls.json';source.write_text(json.dumps({'sourceSha256':data}))
  with patch.object(common,'ROOT',root),patch.object(common,'OPS',ops):
   check('source55-valid-shaped-hashes',lambda:require(common.sources()['source55Match']))
   for name,change in [('hash-shape',lambda d:d.update(source0='bad')),('relative-escape',lambda d:d.update({'../escape':d.pop('source0.cs')}))]:
    def test(change=change):
     changed=dict(data);change(changed);source.write_text(json.dumps({'sourceSha256':changed}))
     try:common.sources()
     except(common.Guard,FileNotFoundError):return
     raise AssertionError('MalformedSourceAccepted')
    check('reject-source-'+name,test)
   source.write_text(json.dumps({'sourceSha256':data}))
   outside=Path(tmp)/'outside';outside.write_text('synthetic-source');(root/'source0.cs').unlink();(root/'source0.cs').symlink_to(outside)
   def symlink():
    try:common.sources()
    except common.Guard:return
    raise AssertionError('OutsideSymlinkAccepted')
   check('reject-resolved-source-escape',symlink)
 # Simulated fresh-session evidence must be checked before opening candidate configuration.
 def session_test(matching):
  with tempfile.TemporaryDirectory(prefix='supported-project-reads-private-smoke-')as tmp:
   folder=Path(tmp);folder.chmod(0o700);cfg=folder/'fixture.json';cfg.write_text('{}');receipt=folder/'apphost-session.json';receipt.write_text('{}');owned={'appHostPid':61001,'appHostStartTicks':'200','cliPid':61002,'cliStartTicks':'201','_configuration':str(cfg),'_isolation':'synthetic','configurationSha256':'b'*64};events=[]
   def read(path):return owned if Path(path)==receipt else{'aspireExecutablePath':'/synthetic/aspire','aspireExecutableSha256':'a'*64}
   def hashed(path):
    if Path(path)==cfg:events.append('configuration-read');return'b'*64
    return'a'*64
   def environment(pid,expected,absent=()):
    if'SUPPORTED_PROJECT_READ_EVIDENCE'in expected:
     events.append('evidence-check')
     if not matching:raise common.Guard('owned-known-environment-mismatch')
    return True
   def link(path):return ownership.APPHOST if str(path)=='/proc/61001/exe'else Path('/synthetic/aspire')
   with patch.object(Path,'glob',return_value=[receipt]),patch.object(ownership,'read_json',side_effect=read),patch.object(ownership,'same',return_value=True),patch.object(ownership,'sha',side_effect=hashed),patch.object(ownership,'known_env',side_effect=environment),patch.object(Path,'readlink',link):
    found=ownership.session(time.time()-1,61000,'100',Path('/synthetic/evidence'))
   if matching:require(found is not None and events.index('evidence-check')<events.index('configuration-read'))
   else:require(found is None and'configuration-read'not in events)
 check('matching-evidence-before-private-config',lambda:session_test(True));check('foreign-fresh-evidence-never-reads-config',lambda:session_test(False))
 def sequence(fail_parser_proc=False):
  with tempfile.TemporaryDirectory(prefix='story65-receiver-coordinator-guard-')as tmp:
   root=Path(tmp);events=[]
   for name in['projects','tenants','folders']:
    folder=root/name;folder.mkdir(mode=0o700);(folder/'execution.nettrace').write_text('synthetic-only');(folder/'collection-receipt.json').write_text('{}')
   def popen(command,**kwargs):return Child(command,events)
   calls=0
   def proc(pid):
    nonlocal calls
    calls+=1
    if fail_parser_proc and calls==4:raise OSError('SyntheticProcReadFailure')
    return{'startTicks':'100'}
   with patch.object(coordinate.subprocess,'Popen',side_effect=popen),patch.object(coordinate,'process',side_effect=proc),patch.object(coordinate,'members',return_value=[]):
    result=coordinate.run(Path('/synthetic/tool'),{r:Path('/synthetic/identity')for r in['projects','tenants','folders']},root,lambda:{'fixed':True})
   first_parse=next((i for i,x in enumerate(events)if x[0]=='spawn'and x[1]=='analyze'),None)
   require(first_parse is not None and sum(x[0]=='wait'for x in events[:first_parse])>=3)
   require(result['allOwnedDiagnosticGroupsAbsent']and all(d['allNonAllowlistedPrivateFilesAbsent']for d in result['privateRemoval'].values()))
   require(all(any(e[0]=='wait'and e[1]==x['pid']for e in events)for x in result['parserProcesses']))
 check('collect-all-before-sequential-parsers-and-purge',sequence);check('parser-tracked-before-fallible-proc-read',lambda:sequence(True))
 def child_finally():
  events=[];child=Child(['synthetic'],events)
  def signal(fd,sig):require(fd==91);child.returncode=-15
  with patch.object(run_one.subprocess,'Popen',return_value=child),patch.object(run_one.os,'pidfd_open',return_value=91),patch.object(run_one.os,'close'),patch.object(run_one,'process',side_effect=OSError('SyntheticProcReadFailure')),patch('signal.pidfd_send_signal',side_effect=signal):
   result=run_one.run_owned(['synthetic'],1,1,1)
  require(result['ownedChildExited']and result['onlyDirectlyOwnedDiagnosticChildSignaled']and result['supervisorErrorType']=='OSError')
 check('all-postspawn-errors-reap-exact-pidfd-child',child_finally)
 with tempfile.TemporaryDirectory(prefix='story65-receiver-purge-guard-')as tmp:
  root=Path(tmp);out=root/'owned';out.mkdir(mode=0o700);outside=root/'outside';outside.write_text('synthetic-sentinel');(out/'execution.nettrace').write_text('synthetic-only');(out/'derivative.tmp').write_text('synthetic-only');(out/'linked').symlink_to(outside);(out/'cpu-summary.json').write_text('{}')
  def purge():
   result=run_one.remove_private_derivatives(out);require(result['allNonAllowlistedPrivateFilesAbsent']and outside.read_text()=='synthetic-sentinel'and set(x.name for x in out.iterdir())=={'cpu-summary.json'})
  check('purge-derivatives-no-symlink-target-access',purge)
 value={'offlineGuardQualificationOnly':True,'passed':all(x['passed']for x in results),'checks':len(results),'results':results,'actualApplicationProcesses':0,'actualCollectorParserProcesses':0,'actualAttaches':0,'actualSignals':0,'fixtureValuesPersisted':False};common.write(output,value);print(json.dumps({'offlineGuardSmoke':True,'checks':len(results),'passed':value['passed']}));return 0 if value['passed']else 1

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();raise SystemExit(main(a.output))
