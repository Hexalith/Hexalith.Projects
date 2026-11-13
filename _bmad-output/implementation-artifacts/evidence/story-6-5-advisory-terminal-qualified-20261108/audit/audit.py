"""Read-only independent reconciliation of closed synthetic terminal qualification."""
from pathlib import Path
import collections,datetime,hashlib,json,os,stat
P=Path('/tmp/story65-advisory-terminal-prep-20261108');R=Path('/tmp/story65-advisory-terminal-qualification-20261108');B=Path('/tmp/story65-advisory-terminal-build-20261108');O=Path(__file__).parent;ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP='138fdb2bf6ec823c4144ab8ce89ad2001478708eedbc238385854ea616824d5e';SUMMARY='c006e0d32b115c2b883d2f6dda3cb3b3d18e33e5e4ad38f5fb0a87d8731b7964';bindings={}
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def unique(pairs):
 d={}
 for k,v in pairs:assert k not in d;d[k]=v
 return d
def load(p):
 p=Path(p);bindings[str(p)]=sha(p);return json.loads(p.read_bytes(),object_pairs_hook=unique)
def nat(v):return type(v)is int and v>=0
assert sha(P/'preparation-receipt.json')==PREP and sha(R/'qualification-summary.json')==SUMMARY
pins=load(P/'preparation-receipt.json');summary=load(R/'qualification-summary.json');build=load(B/'receipt.json');facts=load(B/'runtime-facts.json');disabled=load(B/'disabled-preparation-receipt.json')
assert pins['compileReleased'] and pins['executionReleased'] and pins['sourceQualificationReady']is False and pins['rootDisabledPreparationSha256']==bindings[str(B/'disabled-preparation-receipt.json')]=='c1aee3d2c763336e5d0966556c3f0334a116b60a0d13514beb16969e7c353031'
allpins={}
for key,base,count in [('filesSha256',P,44),('inputsSha256',ROOT,378),('compiledInputsSha256',P,311)]:
 assert len(pins[key])==count
 for n,h in pins[key].items():
  p=Path(n);p=p if p.is_absolute()else base/p
  assert p.is_file()and not p.is_symlink()and all(not v.is_symlink()for v in p.parents)
  assert sha(p)==h;assert str(p)not in allpins or allpins[str(p)]==h;allpins[str(p)]=h
assert len(allpins)==733 and pins['rawReferenceCount']==148
assert disabled['filesSha256']==pins['filesSha256'] and disabled['inputsSha256']==pins['inputsSha256'] and not disabled['compiledInputsSha256']
for key,count in [('sourceSha256',65),('app147Sha256',147)]:
 assert len(pins[key])==count
 for n,h in pins[key].items():assert sha(Path(n)if Path(n).is_absolute()else ROOT/n)==h
assert pins['oracleSha256']==sha(pins['oraclePath'])
assert build['passed'] and len(build['commands'])==2 and bindings[str(B/'receipt.json')]==pins['rootCompileReceiptSha256']
for c in build['commands']:assert c['exitCode']==0 and c['ownedChildExited']and c['guardReason']is None and not c['onlyDirectlyOwnedDiagnosticChildSignaled']
assert facts['runtimeVersion']=='10.0.14' and facts['processorCount']==14 and facts['processorOverride']is None
for key,name in [('oracle','Hexalith.Projects.Server'),('scratch','Hexalith.Projects.Server.Tests')]:
 f=facts[key];assert f['assembly']==name and f['configuration']=='Debug'and f['jitOptimizerDisabled']and allpins[f['location']]==sha(f['location'])
# Exact application substitution, then the three differential-only edits; no code is imported or executed.
normal=load(P/'normalization.json');s=(P/'ProjectAdvisoryQueryExecutor.private.cs').read_bytes()
for e in reversed(normal['edits']):
 a=e['new'].encode();b=e['old'].encode();assert s.count(a)==1;s=s.replace(a,b,1)
assert s==(P/'ProjectAdvisoryQueryExecutor.original.cs').read_bytes()and sha(P/'ProjectAdvisoryQueryExecutor.original.cs')=='ac34b0c5c86c8a85ddf8591f73ddb02d32dc3a63db47a206b42688f2c696df8d'
s=(P/'ProjectAdvisoryQueryExecutor.private.cs').read_bytes();s=s.replace(b'public sealed partial class ProjectAdvisoryQueryExecutor(',b'public sealed partial class InstrumentedProjectAdvisoryQueryExecutor(',1)
s=s.replace(b'    /// <summary>The maximum distinct Project details examined per query.</summary>',b'    internal Hexalith.PrivateCaptureCounter.AdvisoryTerminalCapture? TerminalForProof { get; private set; }\r\n\r\n    /// <summary>The maximum distinct Project details examined per query.</summary>',1)
s=s.replace(b'        using var terminal = conversation ? null : Hexalith.PrivateCaptureCounter.AdvisoryTerminalCapture.Begin();',b'        using var terminal = conversation ? null : Hexalith.PrivateCaptureCounter.AdvisoryTerminalCapture.Begin();\r\n        TerminalForProof = terminal;',1)
assert s==(P/'InstrumentedProjectAdvisoryQueryExecutor.cs').read_bytes()
assert sha(P/'CaptureCounters.cs')=='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'and sha(P/'AdvisoryWaveBootstrap.cs')=='8f5836d1ff4dabb4c5fe819ae762dfff0748b5d792f984ae1adf45af28f45375'
counts=['yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'];fields={'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency','allocationMode','allocatedBytes',*counts};maximum=[11,11,7,2,1,1]
closed_fields={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
slow=['shared-deadline','index-uncooperative','filter-uncooperative','final-anchor-uncooperative','caller-uncooperative-child'];canaries=['canary-positive-control','canary-current','canary-suppressed']
plan=[('disabled','all',None)]+[(m,'canary',None)for m in canaries]
for m in ['active','capped','failing-sink']:plan +=[(m,'fast',None)]+[(m,'slow',c)for c in slow]
plan +=[('state-protocol','protocol',None)]
records=summary['records'];assert len(records)==summary['controlledProcesses']==23 and summary['allClosedZero']and not summary['featureAcceptance']
assert [(r['mode'],r['group'],r['case'])for r in records]==plan
# Closed source-derived case oracle, independent of the RAM snapshots or recorded rows.
expected={'one-candidate':[10,0,1,0,0,0],'two-candidates':[10,0,1,0,0,0],'folder-unavailable':[10,0,2,0,0,0],'clock-exception-identity':[11,0,0,0,0,0]}
for wave in range(1,8):
 for fault in ['throw','dependency-cancel','caller-cancel']:
  value=[10,0,2,0,0,0];kind=1 if fault=='dependency-cancel'else 2
  if wave==1:value=[1,1,5,kind,0,0]
  elif wave in [3,7]:value=[10,wave,6,kind,0,0]
  if fault=='caller-cancel':value=[wave,wave,7,1,1,1]
  expected[f'wave-{wave}-{fault}']=value
for name in ['deny-initial','invalid-payload','invalid-envelope','raw-33','file-only','no-signal','deny-folder','revoke-scope','revoke-project','revoke-input','anchor-change','lifecycle-change','parallel-four']:
 expected[name]={'deny-initial':[1,0,3,0,0,0],'invalid-payload':[11,0,4,0,0,0],'invalid-envelope':[11,0,4,0,0,0],'raw-33':[11,0,4,0,0,0],'file-only':[10,0,2,0,0,0],'revoke-scope':[7,0,3,0,0,0]}.get(name,[10,0,1,0,0,0])
for name,bound in [('shared-deadline',3),('index-uncooperative',3),('filter-uncooperative',2),('final-anchor-uncooperative',6)]:expected[name]=[10,bound,6,1,0,1]
expected['caller-uncooperative-child']=[4,4,7,1,1,1]
assert len(expected)==43
excluded=['wrong-type','null-query','pre-canceled','conversation-disabled','conversation-no-signal','conversation-final-anchor'];assert len(expected)+len(excluded)==49
mode_totals=collections.Counter();terminals=0;row_total=0;stage_totals=collections.Counter();stress=[];ids=[];zero_drop_capture_groups=0
for ordinal,r in enumerate(records,1001):
 m,g,c=r['mode'],r['group'],r['case'];d=R/m/(c or g);rec=load(d/'controller-receipt.json');assert rec==r
 b=load(d/'business-parity.json');assert b['Cases']==b['Passed']and b['ErrorType']=='none'
 assert r['exitCode']==0 and r['ownedGroupMembersAfter']==[]and r['errorType']=='none'and r['runtimeProcessorOverride']is None and not r['rawDependencyTracePersisted']and r['inputPaths']==733
 ids.append((r['pid'],str(r['startTicks'])))
 recording=m not in ['disabled','canary-positive-control'];cl=r['closed'];business_captures=0
 if m in ['disabled','active','capped','failing-sink']:
  mode_totals[m]+=b['Cases'];assert not b['Failures']and not b['TraceValuesPersisted']and not b['RawTokenValuesPersisted']and b['AllocationMode']=='None'and b['TerminalCountEncodingOnly']and b['ConversationsAdmitted']==0
  assert b['FaultCases']==b['FaultsTriggered']and b['PostCapParityCases']==int(m=='capped')
  assert b['Cases']==(49 if m=='disabled'else 44 if g=='fast'else 1)
  business_captures=b['Captures'];want=0 if m=='disabled'else 38 if g=='fast'else 1
  assert b['AdmittedCases']==business_captures==want and b['NotAdmittedCases']==b['Cases']-want
  ts=b['Terminals'];assert len(ts)==want
  names=[t['Case']for t in ts];assert len(names)==len(set(names))
  if want:assert names==(list(expected)[:38]if g=='fast'else[c])
  for t in ts:assert t['Values']==expected[t['Case']]
  terminals+=len(ts)
 elif m in canaries:
  assert b['Cases']==1 and all(b[k]for k in ['CallerCanaryUnchanged','CallerActivityUnchanged','CallerFlowUnchanged'])and not b['ActualCanaryOrActivityValuePersisted']
  assert b['PositiveControl']==b['DrainCanaryObserved']==(m=='canary-positive-control')
  assert b['AlreadySuppressedBranch']==(m=='canary-suppressed')and b['FakeAwaitChecks']==(0 if m=='canary-positive-control' else 22)
  assert b['WorkerProgressAndClosedReceipt']==recording;business_captures=int(recording)
 else:
  assert b['Cases']==b['Captures']==4 and b['SyntheticOnly']and not b['ApplicationValuesPersisted']
  assert b['TerminalValues']==[[10,3,6,2,0,0],[10,4,2,1,0,0],[6,0,0,0,0,1],[0,0,0,0,0,0]];business_captures=4
 assert {f.name for f in d.iterdir()}==({'business-parity.json','controller-receipt.json','projects.jsonl','projects.closed.json'}if recording else{'business-parity.json','controller-receipt.json'})
 if not recording:assert cl is None and r['rows']==0 and r['artifacts']=={};continue
 assert set(cl)==closed_fields and load(d/'projects.closed.json')==cl
 assert all(nat(v)for k,v in cl.items()if k!='role')and cl['role']=='projects'and cl['runOrdinal']==ordinal and cl['diagnosticOnly']==1 and cl['featureAcceptance']==0
 assert cl['admitted']==cl['disposedOrUnwound'] and cl['activeAtDrainEnd']==cl['queuedAtDrainEnd']==cl['incompleteHardDrain']==cl['lateDisposedOrDropCountersMayIncrease']==0 and cl['countersFinal']==1
 assert cl['maximumRows']==65536 and cl['maximumBytes']==8388608 and cl['queueCapacity']==4096 and cl['frequency']==1000000000 and cl['windowEndTicks']-cl['windowStartTicks']==8*cl['frequency']
 assert cl['writtenRows']<=cl['maximumRows']and cl['writtenBytes']<=cl['maximumBytes']and cl['closedRowsDropped']==0
 assert set(r['artifacts'])=={'projects.closed.json','projects.jsonl','window.control'}
 for name,a in r['artifacts'].items():
  if name=='window.control':data=f"{ordinal}\n{cl['frequency']}\n{cl['windowStartTicks']}\n{cl['windowEndTicks']}\n".encode()
  else:data=(d/name).read_bytes()
  assert len(data)==a['bytes']and hashlib.sha256(data).hexdigest()==a['sha256']
 raw=(d/'projects.jsonl').read_bytes();bindings[str(d/'projects.jsonl')]=sha(d/'projects.jsonl');groups={};n=0
 for line in raw.splitlines():
  row=json.loads(line,object_pairs_hook=unique);assert set(row)==fields and row['role']=='projects'and row['stage']in ['Whole','SettingsEnumerationSortDictionary']
  assert all(nat(row[k])for k in fields-{'role','stage'})and row['runOrdinal']==ordinal and row['captureOrdinal']>0 and row['frequency']==cl['frequency']and row['allocationMode']==row['allocatedBytes']==0
  values=[row[k]for k in counts];assert all(v<=hi for v,hi in zip(values,maximum))
  if row['stage']=='Whole':assert values==[0]*6
  else:assert (values[1]==0)==(values[3]==0)
  entry=groups.setdefault(row['captureOrdinal'],{});assert row['stage']not in entry;entry[row['stage']]=row;n+=1;stage_totals[row['stage']]+=1
 assert n==r['rows']==cl['writtenRows']and len(raw)==cl['writtenBytes'];row_total+=n
 # Flood captures emit only Whole; each real terminal capture emits Whole plus Stage4.
 expected_emitted=cl['admitted']+business_captures
 assert expected_emitted==n+cl['queuedRowsDropped']+cl['capRowsDropped']+cl['closedRowsDropped']+cl['ioFailures']
 if m=='capped':
  assert cl['capRowsDropped']>0 and cl['ioFailures']==0 and cl['admitted']>65538
  stress.append({'group':g,'case':c,'admitted':cl['admitted'],'writtenRows':n,'queueDropped':cl['queuedRowsDropped'],'capDropped':cl['capRowsDropped'],'ioFailures':0})
 elif m=='failing-sink':assert cl['ioFailures']==2*business_captures and n==0
 else:
  assert cl['admitted']==business_captures and cl['queuedRowsDropped']==cl['capRowsDropped']==cl['ioFailures']==0 and set(groups)==set(range(1,business_captures+1))
  for x in groups.values():assert set(x)=={'Whole','SettingsEnumerationSortDictionary'}and x['SettingsEnumerationSortDictionary']['elapsedTicks']<=x['Whole']['elapsedTicks']
  values=[[groups[i]['SettingsEnumerationSortDictionary'][k]for k in counts]for i in sorted(groups)]
  if 'Terminals'in b:assert values==[x['Values']for x in b['Terminals']]
  elif 'TerminalValues'in b:assert values==b['TerminalValues']
  zero_drop_capture_groups+=len(groups)
assert dict(mode_totals)=={'disabled':49,'active':49,'capped':49,'failing-sink':49}and terminals==129 and row_total==167315
# Read-only kernel identity scan includes every state, including zombies, and both PGID/SID.
groups_to_check={pid for pid,_ in ids}|{v['childGroup']for v in build['commands']};allids=ids+[(v['childPid'],str(v['childStartTicks']))for v in build['commands']];observed={};permission_errors=0
for p in Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:
  f=(p/'stat').read_text().rsplit(')',1)[1].split();observed[int(p.name)]=(f[19],int(f[2]),int(f[3]),f[0])
 except(FileNotFoundError,ProcessLookupError):pass
 except PermissionError:permission_errors+=1
assert permission_errors==0 and all(observed.get(pid,(None,))[0]!=tick for pid,tick in allids)and all(v[1]not in groups_to_check and v[2]not in groups_to_check for v in observed.values())
private=Path('/tmp/story65-capture-counter-private-20261107');fd=os.open(private,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
try:
 st=os.fstat(fd);assert stat.S_ISDIR(st.st_mode)and(st.st_dev,st.st_ino,st.st_uid,stat.S_IMODE(st.st_mode))==(2096,8752986,1000,0o700)and os.listdir(fd)==[]
finally:os.close(fd)
report={'passed':True,'syntheticQualificationOnly':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'auditSourceSha256':sha(__file__),'bindingsSha256':bindings,'inputs':{'prepared':44,'external':378,'compiled':311,'distinctCurrentPins':733,'source65Current':True,'app147Current':True,'cachedRawReferences':148,'compiledOracleSha256':pins['oracleSha256'],'helperSha256':sha(P/'CaptureCounters.cs'),'bootstrapSha256':sha(P/'AdvisoryWaveBootstrap.cs'),'coreReverseRestoresOriginalBytes':True,'differentialOnlyRenamePropertyAssignmentExact':True},'runtime':{'version':'10.0.14','processorCount':14,'processorOverride':None,'oracleAndScratchDebugJitDisabled':True,'scratchRestoreAndBuildExitZero':True},'coverage':{'controlledProcesses':23,'businessCasesPerMode':dict(mode_totals),'businessComparisons':196,'freshFlowCanaries':3,'stateProtocolCases':4,'closedRamBusinessTerminalSnapshots':129,'strictScalarRows':row_total,'rowFieldCount':14,'stageRowCounts':dict(stage_totals),'closedReceiptFieldCount':23,'zeroDropPairedCaptureGroups':zero_drop_capture_groups,'nativeCasesAdded':0},'terminalProtocol':{'ownEnumBounds':maximum,'expectedSourceBranchValuesIndependentlyChecked':True,'activePersistedRowsEqualRamSnapshots':True,'noAllocationMeasurementOrBusinessCountSlots':True,'responseFlagIsOnlyLocalOmissionObservationAfterSerialization':True,'firstFailureRetainedAcrossFallbackResponse':True,'stateTests':['repeated-close/dispose','parallel-marker-first-failure','late-work-cannot-change-closed-state','invalid-enums-ignore'],'wholeAndEncodingIntervalInterpretation':'Whole includes admitted capture bookkeeping and original method lifetime; Stage4 measures terminal encoding only, not an await wave.','capsMayDropEitherMemberOfAPair':True},'stress':{'cappedRuns':stress,'failingSinkRuns':6,'failingSinkWrittenRows':0,'failingSinkIoFailures':86,'activeBusinessPairs':43,'zeroDropFinalReceiptsAllFinalNoActiveQueuedOrLateCounters':True,'closedRowsDropped':0,'allRowEmissionAndDropAndIoAccountingReconciled':True},'canaries':{'positiveControlObservedOwnSyntheticLeak':True,'freshCurrentAndPreSuppressedDrainDidNotObserveCallerValue':True,'callerContextActivityAndFlowRestored':True,'awaitChecksEach':22,'callerValuesOrActivityIdentifiersPersisted':False},'cleanup':{'all23SyntheticPidPgidSidFreshlyAbsentIncludingZombies':True,'scratchBuildChildrenAndGroupAlsoAbsent':True,'procPermissionErrors':0,'fixedPrivateDirectory':{'device':2096,'inode':8752986,'uid':1000,'mode':'0700','empty':True},'originalPrivateArtifactCustody':'Controller exact nofollow descriptor identity, hash/bytes copy then unlink; control hashes independently reconstructed from closed numeric fields. No control file was read or modified by this audit.'},'limits':['Scratch differential class retains a harness-only terminal property/assignment; live private class has neither. Only reviewed rename/property/assignment differences are admitted.','Business output/error/reference and token/dependency trace comparisons are RAM-only assertions; this audit verifies their closed successful receipts and pinned source, not persisted raw traces.','Capped and failing-sink cases prove business parity plus RAM terminal state under sink failure, not successful persistence of every business terminal row.','Normal and presuppressed first-business canaries cover the synthetic fixture and exact helper/bootstrap; no actual host/workload or inherited request identity claim.','Terminal enum codes are control-flow observations and postserialization local omission state; they do not identify serialized payload outcome, reason for omission, exact dependency cause or deadline causality.','No actual application substitution/live run/native suite was performed by this scratch qualification or audit.']}
(O/'independent-qualification-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'report':str(O/'independent-qualification-audit.json'),'sha256':sha(O/'independent-qualification-audit.json'),'inputs':len(allpins),'rows':row_total,'business':sum(mode_totals.values()),'snapshots':terminals,'stageCounts':dict(stage_totals),'pairedGroups':zero_drop_capture_groups}))
