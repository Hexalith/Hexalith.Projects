from pathlib import Path
import argparse,datetime,json,math,os,secrets,subprocess,sys,time
from ops_common import *

parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
EVIDENCE=Path('/tmp/story65-delegated-client-diagnostic-20261107')
command=[sys.executable,str(PREP/'launch_managed.py'),'--operational-pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED','--browser','--timeout','900','--evidence',str(EVIDENCE/'managed')]
plan={'preparationOnly':not args.execute,'command':command,'cwd':str(ROOT),'noNewRuntimeEnvironmentOrConfigurationKeys':True,'naturalCpuOverrideAbsent':True,'noRuntimeSourceHook':True,'fixedPrivateControlPath':str(PRIVATE/'window.control'),'eightSecondAdmissionOnlyAfterFailedMultiple':True,'noCutoffOrTargetSignals':True,'diagnosticOnly':True,'featureAcceptance':False}
def valid_multi(row):
 return isinstance(row,dict)and row.get('multiple')is True and type(row.get('attempt'))is int and 1<=row['attempt']<=30 and type(row.get('httpStatus'))is int and row['httpStatus']==200 and type(row.get('incomplete'))is bool and type(row.get('candidateCount'))is int and row['candidateCount']>=0 and type(row.get('positiveCandidatesVerified'))is bool and type(row.get('elapsedMilliseconds'))in[int,float]and math.isfinite(row['elapsedMilliseconds'])and row['elapsedMilliseconds']>=0 and row.get('result')in[None,'Empty','SingleCandidate','MultipleCandidates']
def positive(row):
 return valid_multi(row)and row['result']=='MultipleCandidates'and row['incomplete']is False and row['candidateCount']>=2 and row['positiveCandidatesVerified']is True
def failed(row):
 return valid_multi(row)and row['attempt']<=27 and row['result']in[None,'Empty']and row['incomplete']is True and row['candidateCount']==0 and row['positiveCandidatesVerified']is False
if not args.execute:print(json.dumps(plan,indent=2));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True)
native_work=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory']);assert native_work.parent==PREP
native_gate=json.loads((native_work/'receipt.json').read_text());assert native_gate['passed']is True and native_gate['caseCount']==24 and native_gate['nativeProcessAndDescendantsAbsent']is True and native_gate['counterDirectoryEmpty']is True
assert sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll'))==native_gate['privateDomainServiceBefore']['producerSha256']and sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.pdb'))==native_gate['privateDomainServiceBefore']['producerPdbSha256']
ordinary_sdk_proof();ordinary_server_proof();EVIDENCE.mkdir(mode=0o700);env=clean_environment()
assert 'CustomBeforeMicrosoftCommonTargets'not in env and not any('CAPTURE_COUNTER'in key for key in env)
with(EVIDENCE/'launcher-console.log').open('wb')as stream:
 process=subprocess.Popen(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 started=Path('/proc',str(process.pid),'stat').read_text().rsplit(')',1)[1].split()[19]
 save(EVIDENCE/'managed-command.json',{'command':command,'cwd':str(ROOT),'launcherPid':process.pid,'launcherStartTicks':started,'diagnosticOnly':True,'featureAcceptance':False,'noCutoffOrSignals':True})
 receipt={**plan,'preparationOnly':False,'launcherPid':process.pid,'launcherStartTicks':started,'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'controlPublished':False}
 save(EVIDENCE/'supervisor.json',receipt);print(json.dumps({'phase':'managed-started','launcherPid':process.pid,'evidence':str(EVIDENCE)}),flush=True)
 try:
  while process.poll()is None:
   rows=[];path=EVIDENCE/'managed/browser-advisory-seed-readiness.jsonl'
   if path.exists():
    # An unfinished final line is never interpreted as a row or positive.
    for line in path.read_bytes().split(b'\n')[:-1]:
     try:rows.append(json.loads(line))
     except ValueError:continue
   multi=[row for row in rows if valid_multi(row)]
   if not receipt['controlPublished']and multi:
    has_positive=any(positive(row)for row in multi)
    if has_positive:receipt['coverageSkip']='genuine-multiple-positive-observed';save(EVIDENCE/'supervisor.json',receipt)
    elif any(failed(row)for row in multi)and max(row['attempt']for row in multi)<=27 and not receipt.get('coverageSkip')and(EVIDENCE/'runtime-private-domain-proof.json').exists():
     try:
      sources();private_directory(require_empty=True)
      proof=json.loads((EVIDENCE/'runtime-private-domain-proof.json').read_text());assert proof['allSixOrdinarySdkCopiesMatch']and proof['projectsPrivateDomainAssemblyVerified']
      assert sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll'))==proof['privateProducerSha256']and marker(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll'))
      assert sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.pdb'))==proof['privateProducerPdbSha256']
      for value in proof['copies']:
       assert Path('/proc',str(value['pid']),'stat').read_text().rsplit(')',1)[1].split()[19]==value['startTicks']
       project=ROOT/json.loads((PREP/'affected-inventory.json').read_text())['projects']['eventstore'if value['role']=='eventstore'else value['role']]
       sdk=project.parent/'bin/Debug/net10.0'/(SDK+'.dll');assert sha(sdk)==ORDINARY_SDK_SHA and not marker(sdk)
      ordinary_sdk_proof();ordinary_server_proof()
      maps=Path('/proc',str(proof['projectsPid']),'maps').read_text();private_domain=ROOT/'src/Hexalith.Projects.Server/bin/Debug/net10.0'/(ASSEMBLY+'.dll')
      assert str(private_domain)==proof['projectsDomainCopyPath']and sha(private_domain)==proof['projectsDomainCopySha256']==proof['privateProducerSha256']and marker(private_domain)
      assert sha(private_domain.with_suffix('.pdb'))==proof['projectsDomainPdbSha256']==proof['privateProducerPdbSha256']
      assert any(line.split(maxsplit=5)[-1]==str(private_domain)for line in maps.splitlines()if len(line.split(maxsplit=5))==6)
      start=time.monotonic_ns();end=start+8_000_000_000;control=PRIVATE/'window.control';temporary=PRIVATE/'window.control.tmp'
      # Generated opaque ordinal has no inherited request/trace/identity linkage.
      run=secrets.randbelow(2**62)+1
      fd=os.open(temporary,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
      with os.fdopen(fd,'w')as output:output.write(f'{run}\n1000000000\n{start}\n{end}\n')
      temporary.replace(control)
      receipt.update(controlPublished=True,admissionStartTicks=start,admissionEndTicks=end,frequency=1000000000,runOrdinal=run,failedMultipleBeforeAdmission=len(multi),identityReadyBeforeAdmission=True)
      save(EVIDENCE/'supervisor.json',receipt);print(json.dumps({'phase':'private-eight-second-admission','failedMultiple':len(multi)}),flush=True)
     except Exception as failure:
      receipt['coverageSkip']='private-admission-guard-refused';receipt['diagnosticFailureType']=type(failure).__name__;save(EVIDENCE/'supervisor.json',receipt)
    elif max(row['attempt']for row in multi)>27:receipt['coverageSkip']='fewer-than-three-attempt-windows-remain';save(EVIDENCE/'supervisor.json',receipt)
   time.sleep(.1)
 except Exception as failure:
  receipt['coverageSkip']='private-controller-failed';receipt['diagnosticFailureType']=type(failure).__name__
  # Metadata controller faults never abandon or signal the managed child.
  process.wait()
 receipt.update(launcherExitCode=process.wait(),finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),phase='managed-complete',noTopologyCutoffOrSignals=True)
 save(EVIDENCE/'supervisor.json',receipt);print(json.dumps({'phase':'managed-complete','launcherExitCode':receipt['launcherExitCode']}),flush=True)
