"""Prepared read-only audit; run only after Root releases the closed boundary-span results."""
from pathlib import Path
import argparse,collections,datetime,hashlib,json,os,stat
P=Path('/tmp/story65-advisory-boundary-span-prep-20261108');R=Path('/tmp/story65-advisory-boundary-span-qualification-20261108');B=Path('/tmp/story65-advisory-boundary-span-build-20261108');O=Path(__file__).parent;ROOT=Path('/home/administrator/projects/hexalith/projects')
SOURCE_PREP='37dbcd22e1f4acc261abb78c34ffceb8add4904240ca8b28c8a3c7b452b31d93'
RUNTIME_PREP='b7662b6597a06c630d8937bd092fc527fb33fa809acfbd00ad5f0a249f202ad2'
parser=argparse.ArgumentParser();parser.add_argument('--root-closed-results-release',action='store_true');parser.add_argument('--pins-sha256',required=True);parser.add_argument('--summary-sha256',required=True)
args=parser.parse_args();assert args.root_closed_results_release
PREP=args.pins_sha256;SUMMARY=args.summary_sha256
assert all(len(value)==64 and all(ch in '0123456789abcdef'for ch in value)for value in [PREP,SUMMARY])
assert PREP==RUNTIME_PREP
bindings={}
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
assert pins['compileReleased'] and pins['executionReleased'] and pins['sourceQualificationReady']is False
assert bindings[str(B/'disabled-preparation-receipt.json')]==SOURCE_PREP
allpins={}
compiled_count=len(pins['compiledInputsSha256']);assert compiled_count==335
for key,base,count in [('filesSha256',P,50),('inputsSha256',ROOT,381),('compiledInputsSha256',P,compiled_count)]:
 assert len(pins[key])==count
 for n,h in pins[key].items():
  p=Path(n);p=p if p.is_absolute()else base/p
  assert p.is_file()and not p.is_symlink()and all(not v.is_symlink()for v in p.parents)
  assert sha(p)==h;assert str(p)not in allpins or allpins[str(p)]==h;allpins[str(p)]=h
assert len(allpins)==50+381+compiled_count and pins['rawReferenceCount']==148
assert disabled['filesSha256']==pins['filesSha256'] and disabled['inputsSha256']==pins['inputsSha256'] and not disabled['compiledInputsSha256']
for key,count in [('sourceSha256',65),('app147Sha256',147)]:
 assert len(pins[key])==count
 for n,h in pins[key].items():assert sha(Path(n)if Path(n).is_absolute()else ROOT/n)==h
assert pins['oracleSha256']==sha(pins['oraclePath'])
assert build['passed'] and len(build['commands'])==2 and bindings[str(B/'receipt.json')]==allpins[str(B/'receipt.json')]
for c in build['commands']:assert c['exitCode']==0 and c['ownedChildExited']and c['guardReason']is None and not c['onlyDirectlyOwnedDiagnosticChildSignaled']
assert facts['runtimeVersion']=='10.0.14' and facts['processorCount']==14 and facts['processorOverride']is None
assert nat(facts['factsProcessId'])and facts['factsProcessId']>0 and facts['factsProcessExitCode']==0 and facts['factsProcessGroupAbsentIncludingZombies']is True
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
assert sha(P/'ProjectAdvisoryQueryExecutor.private.cs')=='d36afcb5709e36eb0b8039133947b1fad93df56de0147e1b624842f0e77542fe'
assert sha(P/'AdvisoryTerminalCapture.cs')=='591a2363582336bebd27e1e50515aea58e937147c56f98a19a2bb91b8d2799a0'
adapter=load(P/'source-proof.json');s=(P/'AdvisoryTerminalCapture.cs').read_text()
for edit in reversed(adapter['adapterEdits']):assert s.count(edit['after'])==1;s=s.replace(edit['after'],edit['before'],1)
assert s.encode()==(P/'AdvisoryTerminalCapture.original.cs').read_bytes()and sha(P/'AdvisoryTerminalCapture.original.cs')=='9a210774ec8078ce8f27fe7d5513055d13e8d4bf56f582ffbd1688e365961a16'
counts=['yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'];fields={'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency','allocationMode','allocatedBytes',*counts};maximum=[11,11,7,2,1,1]
stages=['Whole','ComponentFile','ComponentPolicy','AclFile','SettingsEnumerationSortDictionary','AuthenticationSerialization','ActorBoundary','ResultTail'];selected={1,2,3,5,6,7}
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
# Independently derive selected marker spans from the original closed branch catalog.
# These are source-path assertions, not a claim missing/dropped/zero rows prove nonexecution.
masks={name:set(selected)for name in expected}
for name in ['deny-initial','invalid-payload','invalid-envelope','raw-33','clock-exception-identity']:masks[name]={1}
for name in ['folder-unavailable','file-only','no-signal','deny-folder']:masks[name]={1,2,5,6}
for wave in range(1,8):
 for fault in ['throw','dependency-cancel']:
  masks[f'wave-{wave}-{fault}']={1:{1},2:{1,2,5,6},3:{1,2,7}}.get(wave,set(selected))
 masks[f'wave-{wave}-caller-cancel']={1:{1},2:{1,2},3:{1,2,7},4:{1,2,3,7},5:{1,2,3,5,7}}.get(wave,set(selected))
masks.update({'shared-deadline':{1,2,7},'index-uncooperative':{1,2,7},'filter-uncooperative':{1,2},'final-anchor-uncooperative':set(selected),'caller-uncooperative-child':{1,2,3,7}})
old_protocol_values=[[10,3,6,2,0,0],[10,4,2,1,0,0],[6,0,0,0,0,1],[0,0,0,0,0,0]]
old_protocol_masks=[{7},{3},{6},set()]
focused=[
 ('six-selected-markers',[10,0,1,0,0,0],set(selected)),
 ('envelope-gap-excluded',[10,0,1,0,0,0],{1,2}),
 ('failure-ends-before-fallback',[10,3,6,2,0,0],{5,7}),
 ('close-dispose-idempotent',[5,0,0,0,0,1],{5}),
 ('repeat-disables-future-spans',[10,0,1,0,0,0],{2}),
 ('invalid-enums-ignored',[3,0,0,0,0,0],{2,7}),
 ('unselected-markers-end-span',[10,0,0,0,0,0],{3}),
 ('index-selected-final-scope-gap',[7,0,0,0,0,0],{3,5,6,7}),
 ('final-marker-cancellation-no-callback-claim',[10,6,6,1,0,1],{6}),
 ('late-events-cannot-change-spans',[6,0,0,0,0,0],{6}),
]
protocol_values=old_protocol_values+[value for _,value,_ in focused]
protocol_masks=old_protocol_masks+[mask for _,_,mask in focused]
def check_ticks(ticks,mask):
 assert isinstance(ticks,list)and len(ticks)==8 and all(nat(v)for v in ticks)
 assert ticks[0]>0 and ticks[4]>0 and {i for i in selected if ticks[i]>0}==mask
 assert sum(ticks[1:])<=ticks[0]
def check_rows_snapshot(group,ticks):
 assert set(group)=={stages[i]for i,t in enumerate(ticks)if t>0}
 assert all(group[stages[i]]['elapsedTicks']==t for i,t in enumerate(ticks)if t>0)
mode_totals=collections.Counter();terminals=0;row_total=0;stage_totals=collections.Counter();stress=[];ids=[];zero_drop_capture_groups=0;failing_io=0;all_attempted_rows=0
for ordinal,r in enumerate(records,1001):
 m,g,c=r['mode'],r['group'],r['case'];d=R/m/(c or g);rec=load(d/'controller-receipt.json');assert rec==r
 b=load(d/'business-parity.json');assert b['Cases']==b['Passed']and b['ErrorType']=='none'
 assert r['exitCode']==0 and r['ownedGroupMembersAfter']==[]and r['errorType']=='none'and r['runtimeProcessorOverride']is None and not r['rawDependencyTracePersisted']and r['inputPaths']==len(allpins)
 ids.append((r['pid'],str(r['startTicks'])))
 recording=m not in ['disabled','canary-positive-control'];cl=r['closed'];business_captures=0;span_rows=0;snapshots=[]
 if m in ['disabled','active','capped','failing-sink']:
  mode_totals[m]+=b['Cases'];assert not b['Failures']and not b['TraceValuesPersisted']and not b['RawTokenValuesPersisted']and b['AllocationMode']=='None'and b['TerminalCountEncodingOnly']and b['ConversationsAdmitted']==0
  assert b['FaultCases']==b['FaultsTriggered']and b['PostCapParityCases']==int(m=='capped')
  assert b['Cases']==(49 if m=='disabled'else 44 if g=='fast'else 1)
  business_captures=b['Captures'];want=0 if m=='disabled'else 38 if g=='fast'else 1
  assert b['AdmittedCases']==business_captures==want and b['NotAdmittedCases']==b['Cases']-want
  ts=b['Terminals'];assert len(ts)==want
  names=[t['Case']for t in ts];assert len(names)==len(set(names))
  if want:assert names==(list(expected)[:38]if g=='fast'else[c])
  for t in ts:
   assert set(t)=={'Case','Values','SpanTicks'}and t['Values']==expected[t['Case']]
   check_ticks(t['SpanTicks'],masks[t['Case']]);snapshots.append(t['SpanTicks'])
   span_rows+=len(masks[t['Case']])
  terminals+=len(ts)
 elif m in canaries:
  assert b['Cases']==1 and all(b[k]for k in ['CallerCanaryUnchanged','CallerActivityUnchanged','CallerFlowUnchanged'])and not b['ActualCanaryOrActivityValuePersisted']
  assert b['PositiveControl']==b['DrainCanaryObserved']==(m=='canary-positive-control')
  assert b['AlreadySuppressedBranch']==(m=='canary-suppressed')and b['FakeAwaitChecks']==(0 if m=='canary-positive-control' else 22)
  assert b['WorkerProgressAndClosedReceipt']==recording;business_captures=int(recording);span_rows=6*business_captures
 else:
  assert b['Cases']==b['Captures']==14 and b['OriginalTerminalProtocolCases']==4 and b['BoundarySpanCases']==10 and b['SyntheticOnly']and not b['ApplicationValuesPersisted']
  assert b['TerminalValues']==protocol_values and len(b['SpanTicks'])==14 and len(b['BoundaryCases'])==10
  for ticks,mask in zip(b['SpanTicks'],protocol_masks):check_ticks(ticks,mask)
  for index,(name,value,mask)in enumerate(focused):
   test=b['BoundaryCases'][index];assert set(test)=={'Case','Values','SpanTicks','ExpectedStages'}
   assert test['Case']==name and test['Values']==value and test['SpanTicks']==b['SpanTicks'][index+4]
   assert set(test['ExpectedStages'])==mask and len(test['ExpectedStages'])==len(mask)
  business_captures=14;span_rows=sum(map(len,protocol_masks));snapshots=b['SpanTicks']
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
  row=json.loads(line,object_pairs_hook=unique);assert set(row)==fields and row['role']=='projects'and row['stage']in stages
  assert all(nat(row[k])for k in fields-{'role','stage'})and row['runOrdinal']==ordinal and row['captureOrdinal']>0 and row['frequency']==cl['frequency']and row['allocationMode']==row['allocatedBytes']==0
  values=[row[k]for k in counts];assert all(v<=hi for v,hi in zip(values,maximum))
  if row['stage']!='SettingsEnumerationSortDictionary':assert values==[0]*6
  else:assert (values[1]==0)==(values[3]==0)
  entry=groups.setdefault(row['captureOrdinal'],{});assert row['stage']not in entry;entry[row['stage']]=row;n+=1;stage_totals[row['stage']]+=1
 assert n==r['rows']==cl['writtenRows']and len(raw)==cl['writtenBytes'];row_total+=n
 # Flood captures emit only Whole. Real captures add terminal4 plus actual selected spans.
 expected_emitted=cl['admitted']+business_captures+span_rows;all_attempted_rows+=expected_emitted
 assert expected_emitted==n+cl['queuedRowsDropped']+cl['capRowsDropped']+cl['closedRowsDropped']+cl['ioFailures']
 if m=='capped':
  assert cl['capRowsDropped']>0 and cl['ioFailures']==0 and cl['admitted']>65538
  stress.append({'group':g,'case':c,'admitted':cl['admitted'],'writtenRows':n,'queueDropped':cl['queuedRowsDropped'],'capDropped':cl['capRowsDropped'],'ioFailures':0})
 elif m=='failing-sink':assert cl['ioFailures']==expected_emitted and n==0;failing_io+=cl['ioFailures']
 else:
  assert cl['admitted']==business_captures and cl['queuedRowsDropped']==cl['capRowsDropped']==cl['ioFailures']==0 and set(groups)==set(range(1,business_captures+1))
  for x in groups.values():
   assert {'Whole','SettingsEnumerationSortDictionary'}<=set(x)and len(x)<=8
   assert sum(row['elapsedTicks']for stage,row in x.items()if stage!='Whole')<=x['Whole']['elapsedTicks']
   if m in ['canary-current','canary-suppressed']:assert set(x)==set(stages)
  values=[[groups[i]['SettingsEnumerationSortDictionary'][k]for k in counts]for i in sorted(groups)]
  if 'Terminals'in b:assert values==[x['Values']for x in b['Terminals']]
  elif 'TerminalValues'in b:assert values==b['TerminalValues']
  if snapshots:
   assert len(snapshots)==len(groups)
   for capture,ticks in zip(sorted(groups),snapshots):check_rows_snapshot(groups[capture],ticks)
  zero_drop_capture_groups+=len(groups)
assert dict(mode_totals)=={'disabled':49,'active':49,'capped':49,'failing-sink':49}and terminals==129 and row_total>0
# Read-only kernel identity scan includes every state, including zombies, and both PGID/SID.
groups_to_check={pid for pid,_ in ids}|{v['childGroup']for v in build['commands']}|{facts['factsProcessId']};allids=ids+[(v['childPid'],str(v['childStartTicks']))for v in build['commands']];observed={};permission_errors=0
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
report={
 'passed':True,'syntheticQualificationOnly':True,'featureAcceptance':False,
 'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'auditSourceSha256':sha(__file__),'sourcePreparationSha256':SOURCE_PREP,
 'runtimePreparationSha256':PREP,'qualificationSummarySha256':SUMMARY,'bindingsSha256':bindings,
 'inputs':{'prepared':50,'external':381,'compiled':compiled_count,'distinctCurrentPins':len(allpins),
  'allFrozenInputsCurrent':True,'source65Current':True,'app147Current':True,'cachedRawReferences':148,
  'compiledOracleSha256':pins['oracleSha256'],'helperSha256':sha(P/'CaptureCounters.cs'),
  'bootstrapSha256':sha(P/'AdvisoryWaveBootstrap.cs'),'adapterSha256':sha(P/'AdvisoryTerminalCapture.cs'),
  'coreReverseRestoresOriginalBytes':True,'adapterReverseRestoresAcceptedTerminalBytes':True,
  'differentialOnlyRenamePropertyAssignmentExact':True,
  'currentCensusesNotHistoricalAll733GenerationEquality':True,'mutableSpecOutsideSourceControls':True},
 'runtime':{'version':'10.0.14','processorCount':14,'processorOverride':None,
  'oracleAndScratchDebugJitDisabled':True,'scratchRestoreAndBuildExitZero':True},
 'coverage':{'controlledProcesses':23,'businessCasesPerMode':dict(mode_totals),'businessComparisons':196,
  'freshFlowCanaries':3,'retainedTerminalProtocolCases':4,'newBoundarySpanCases':10,'stateProtocolCases':14,
  'closedRamBusinessTerminalSnapshots':terminals,'strictScalarRows':row_total,'rowFieldCount':14,
  'stageRowCounts':dict(stage_totals),'closedReceiptFieldCount':23,
  'zeroDropCaptureGroups':zero_drop_capture_groups,'allAttemptedRowsReconciled':all_attempted_rows,
  'nativeCasesAdded':0,'applicationBuildReleased':False,'liveRunReleased':False},
 'terminalProtocol':{'ownEnumBounds':maximum,'expectedSourceBranchValuesIndependentlyChecked':True,
  'activePersistedRowsEqualRamSnapshots':True,'noAllocationMeasurementOrBusinessCountSlots':True,
  'responseFlagIsOnlyLocalOmissionObservationAfterSerialization':True,'firstFailureRetainedAcrossFallbackResponse':True,
  'originalStateTests':['repeated-close/dispose','parallel-marker-first-failure','late-work-cannot-change-closed-state','invalid-enums-ignore'],
  'newFocusedCaseNames':[name for name,_,_ in focused],
  'capsMayDropEitherMemberOrAnySpanOfACapture':True},
 'boundarySpans':{'sourceMapping':{'ComponentFile':'InitialScope','ComponentPolicy':'InitialInputs',
  'AclFile':'Discovery','AuthenticationSerialization':'FinalInputs','ActorBoundary':'FinalCandidates','ResultTail':'Index'},
  'stoppedAtEveryValidMarkerFailureOrClose':True,'sourceDerived49CaseMasksChecked':True,
  'original4AndNew10ProtocolMasksChecked':True,'zeroDropRamToRawTicksExact':True,
  'stage4StillTerminalEncodingOnly':True,'countColumnsZeroOutsideStage4':True,
  'selectedIntervalsPlusEncodingNotGreaterThanWhole':True,'oneRowPerCaptureStage':True,
  'maximumRowsPerAdmittedCapture':8,'syntheticHeldGapAndFailureAssertionsSourceBoundAndPassed':True,
  'auditReconstructsExactHoldSchedules':False,'missingZeroOrDroppedSpanDoesNotProveSkippedWork':True,
  'notAwaitOnlyOrCallbackActivityOrQuiescence':True},
 'stress':{'cappedRuns':stress,'failingSinkRuns':6,'failingSinkWrittenRows':0,'failingSinkIoFailures':failing_io,
  'activeBusinessCaptures':43,'zeroDropFinalReceiptsAllFinalNoActiveQueuedOrLateCounters':True,
  'closedRowsDropped':0,'allRowEmissionAndDropAndIoAccountingReconciled':True},
 'canaries':{'positiveControlObservedOwnSyntheticLeak':True,
  'freshCurrentAndPreSuppressedDrainDidNotObserveCallerValue':True,'callerContextActivityAndFlowRestored':True,
  'awaitChecksEach':22,'callerValuesOrActivityIdentifiersPersisted':False},
 'cleanup':{'all23SyntheticPidPgidSidFreshlyAbsentIncludingZombies':True,'scratchBuildChildrenAndGroupAlsoAbsent':True,
  'factsProcessGroupAndSessionFreshlyAbsentIncludingZombies':True,'factsProcessStartTicksRecorded':False,
  'procPermissionErrors':0,'fixedPrivateDirectory':{'device':2096,'inode':8752986,'uid':1000,'mode':'0700','empty':True},
  'originalPrivateArtifactCustody':'Controller exact nofollow descriptor identity, hash/bytes copy then unlink; control hashes independently reconstructed from closed numeric fields. This audit neither reads nor modifies numeric control.'},
 'limits':[
  'Scratch differential class retains its reviewed harness-only terminal property/assignment; live private class has neither.',
  'Business output/error/reference and token/dependency comparisons remain RAM-only assertions. This audit reconciles pinned sources and successful closed receipts, not raw traces.',
  'Spans run from recorder timestamps at existing markers to the next valid marker, first catch notification, or close. They include intervening work/bookkeeping and are not exact awaits or proof callbacks started or quiesced.',
  'EnvelopeValidation, FinalScope, Response and other unselected intervals are gaps. Whole minus selected spans is unattributed, not an exact omitted activity.',
  'Capped/failing sinks prove parity plus closed RAM state under loss, not persistence of every terminal or span. Missing/zero/dropped rows are unobserved.',
  'Pure synthetic held-gap timing checks are bound to source and passed status; exact schedules are not persisted or independently reconstructed.',
  'First-business canaries cover this synthetic fixture/helper/bootstrap; no actual host/workload or inherited request identity claim.',
  'Terminal fields do not identify serialized outcome, omission reason, dependency cause, deadline cause or child quiescence.',
  'Source65/current app147 and current compiled closure are checked. No equality to all historical733 input generations or historical native outputs is claimed.',
  'No application substitution/live run/native suite is performed by this scratch qualification or audit.']}
with (O/'independent-qualification-audit.json').open('x')as output:json.dump(report,output,indent=2);output.write('\n')
print(json.dumps({'passed':True,'report':str(O/'independent-qualification-audit.json'),'sha256':sha(O/'independent-qualification-audit.json'),
 'inputs':len(allpins),'rows':row_total,'business':sum(mode_totals.values()),'snapshots':terminals,
 'stageCounts':dict(stage_totals),'zeroDropCaptureGroups':zero_drop_capture_groups,'failingSinkIoFailures':failing_io}))
