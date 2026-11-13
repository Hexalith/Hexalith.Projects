#!/usr/bin/env python3
"""Audit a CLOSED receiver Q2; never starts builds, children, hosts or counter windows."""
from pathlib import Path
import argparse, collections, datetime, hashlib, json, os, re, stat
import xml.etree.ElementTree as ET

ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-receiver-phase-prep-20261107')
Q2=Path('/tmp/story65-receiver-phase-qualification-retry-20261107')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
PREP_SHA='83bb2d0938e32482f1a459df945e7441904e84bcd64c605d9eb223e76f2fc623'
MANIFEST=Path('/tmp/story65-receiver-phase-runtime-manifest-retry-20261107.json')
MANIFEST_SHA='2e127e4f3ecab5617cfcca8d847a32447cf2f3f5e65b89be1584a020c4e5ba4c'
PRODUCER=Path('/tmp/story65-receiver-phase-producer-prep-20261107/producer-attempts/attempt-bv7kbrua')
PRODUCER_SHA='d7d77a65c2ff3a8d3d767eae1eecfabb847bc597504106008e1e7f4b15f0c4fd'
PRODUCER_AUDIT_SHA='e0a954336da6fd64877a722d55dcb48d45482637e0a4b7a2a2851afe3e412ef7'
CORE='f426ca5a31a53b26d92ed218151d1d2aa1eab4dc64b6a2f0cd35da142c0eaed1'
HELPER='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
BOOTSTRAP='ff22ad6362bf4f7dea2ffaa782066dc719f1fcbf6e9205e50ee3df9df30df495'
REFERENCE=Path('/tmp/story65-receiver-phase-ops-prep-20261107/native-reference-case-set.json')
REFERENCE_SHA='188290c760bcd13dbad01dcf133845cda5d82b42a46889ac6c6c2aa7e43f018b'
STAGES=['Whole','ComponentFile','ComponentPolicy','AclFile','SettingsEnumerationSortDictionary','AuthenticationSerialization','ActorBoundary','ResultTail']
ZERO={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
ROW=ZERO|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
CLOSED={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
MODES=['disabled','active','pre-capped','failing-sink']
GROUPS={'ingress':['bypass','invalid-channel','native-config-discovery','native-allows-refusal'],'refusals':['receiver-not-ready','native-acquire-refusal','native-snapshot-refusal','technical-auth-refusal'],'execution':['native-success','native-next-fault','native-next-cancellation','technical-success'],'denial':['technical-grant-denial','technical-next-fault-release-precedence'],'double-success':['technical-double-dispose-retry-success'],'double-fault':['technical-double-dispose-second-fault']}
# Acquires, next calls, durable release callbacks, injected faults, exact stage mask, error type.
CASES={'bypass':(0,1,0,0,1,None),'invalid-channel':(0,0,0,0,1,None),'native-config-discovery':(0,1,0,0,3,None),'native-allows-refusal':(0,0,0,0,3,None),'receiver-not-ready':(0,0,0,0,3,None),'native-acquire-refusal':(1,0,0,1,143,None),'native-snapshot-refusal':(1,0,1,0,175,None),'technical-auth-refusal':(0,0,0,0,71,None),'native-success':(1,1,1,0,191,None),'native-next-fault':(1,1,0,1,191,'System.InvalidOperationException'),'native-next-cancellation':(1,1,0,1,191,'System.OperationCanceledException'),'technical-success':(1,1,1,0,127,None),'technical-grant-denial':(1,0,1,0,111,None),'technical-next-fault-release-precedence':(1,1,1,2,127,'System.IO.IOException'),'technical-double-dispose-retry-success':(1,0,2,1,111,None),'technical-double-dispose-second-fault':(1,0,2,2,111,'System.InvalidOperationException')}
SEMANTIC={'Case','Passed','MiddlewareEntries','AcquireCalls','NextCalls','ReleaseCallbacks','ErrorType','Faults','TokenMatched','LifetimeMatched','CallerRestored','ExpectedStageMask','DoubleDisposeOrder','ReleaseBoundaryRestored','ExactContextReachedNext','FirstDependencyProbeObserved','EntrySuppressionObserved'}
RESULT=SEMANTIC|{'MinimumLastDisposeTicks','MaximumLastDisposeTicks','FirstDisposeHoldTicks'}
ENTRIES={'projects':'Hexalith.Projects.Server','tenants':'Hexalith.Tenants','folders':'Hexalith.Folders.Server','excluded':'Story65.ReceiverPhase.Excluded'}
NATIVE={'TechnicalHttpInstalledMetadataTests':8,'TechnicalHttpNativeRpcTests':12,'ReceiverImagePrecheckTests':20,'NativeDispatchDiagnosticsTests':7}

class AuditFailure(Exception):pass

def need(value,code):
 if not value:raise AuditFailure(code)

def raw(path,maximum=16*1024*1024):
 fd=os.open(path,os.O_RDONLY|os.O_NOFOLLOW|os.O_NONBLOCK|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);need(stat.S_ISREG(s.st_mode)and s.st_size<=maximum,'regular-bounded-file')
  with os.fdopen(fd,'rb',closefd=False)as stream:data=stream.read(maximum+1)
  need(len(data)<=maximum,'bounded-file-read');return data
 finally:os.close(fd)

def sha(path):
 fd=os.open(path,os.O_RDONLY|os.O_NOFOLLOW|os.O_NONBLOCK|os.O_CLOEXEC)
 try:
  need(stat.S_ISREG(os.fstat(fd).st_mode),'regular-hash-input')
  with os.fdopen(fd,'rb',closefd=False)as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
 finally:os.close(fd)

def digest(data):return hashlib.sha256(data).hexdigest()

def decode(data):
 def pairs(values):
  d={}
  for k,v in values:need(k not in d,'duplicate-json-key');d[k]=v
  return d
 return json.loads(data,object_pairs_hook=pairs)

def load(path):return decode(raw(path))

def integers(d,keys):need(all(type(d[k])is int and d[k]>=0 for k in keys),'closed-integer-types')

def proc(pid):
 try:
  v=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()
  return {'pid':pid,'group':int(v[2]),'startTicks':v[19]}
 except(FileNotFoundError,ProcessLookupError):return None

def group_absent(group):
 for p in Path('/proc').iterdir():
  if p.name.isdecimal():
   x=proc(int(p.name))
   if x and x['group']==group:return False
 return True

def result(r):
 need(set(r)==RESULT and r['Case']in CASES,'result-schema-case')
 for k in ['MiddlewareEntries','AcquireCalls','NextCalls','ReleaseCallbacks','Faults','ExpectedStageMask','MinimumLastDisposeTicks','MaximumLastDisposeTicks','FirstDisposeHoldTicks']:integers(r,[k])
 for k in SEMANTIC-{'Case','ErrorType','MiddlewareEntries','AcquireCalls','NextCalls','ReleaseCallbacks','Faults','ExpectedStageMask'}:need(type(r[k])is bool,'result-boolean-types')
 need(r['MiddlewareEntries']==1,'one-actual-entry')
 need((r['AcquireCalls'],r['NextCalls'],r['ReleaseCallbacks'],r['Faults'],r['ExpectedStageMask'],r['ErrorType'])==CASES[r['Case']],'exact-case-counts-faults-mask-error')
 for k in ['Passed','TokenMatched','LifetimeMatched','CallerRestored','DoubleDisposeOrder','ReleaseBoundaryRestored','ExactContextReachedNext','EntrySuppressionObserved']:need(r[k]is True,'case-invariant')
 need(r['FirstDependencyProbeObserved']==bool(r['AcquireCalls']or r['NextCalls']),'dependency-observed-when-called')
 if r['Case'].startswith('technical-double-dispose-'):
  need(0<r['MinimumLastDisposeTicks']<=r['MaximumLastDisposeTicks']<r['FirstDisposeHoldTicks'],'last-release-not-first-or-total-bracket')


def main():
 parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--parent-audit',required=True);parser.add_argument('--parent-audit-sha256',required=True);parser.add_argument('--output',required=True)
 args=parser.parse_args();need(args.execute,'explicit-audit-execute-required')
 out=Path(args.output);need(out.is_absolute()and out.is_relative_to('/tmp')and not out.exists(),'fresh-temp-output')
 evidence={}
 def bind(path,expected=None):
  p=Path(path);h=sha(p);need(expected is None or h==expected,'pinned-evidence-hash');evidence[str(p)]=h;return load(p)
 # No /proc or private-directory inspection until the complete closed summary has passed.
 summary=bind(Q2/'qualification-summary.json')
 need(summary['passed']is True and summary['processes']==62 and summary['focusedPairs']==64,'closed-complete-q2-required')
 need(summary['nativePassesPerVariant']=={'ordinary':47,'private':47}and summary['firstBusinessCanaries']==3 and summary['additionalRolePositiveCases']==8 and summary['excludedCases']==1,'summary-counts')
 need(summary['sourceAndRuntimeInputsUnchanged']is True and summary['noNativeCountInflation']is True and summary['rawRequestOrTraceValuesPersisted']is False,'summary-limits')
 preparation=bind(PREP/'preparation-receipt.json',PREP_SHA);manifest=bind(MANIFEST,MANIFEST_SHA);reference=bind(REFERENCE,REFERENCE_SHA)
 need(manifest['sourcePreparationSha256']==PREP_SHA and len(preparation['sourceSha256'])==56 and len(preparation['app147Sha256'])==147,'preparation-runtime-source-counts')
 need(sha(PREP/'TechnicalHttpRouteCatalogReceiver.private.cs')==CORE and sha(PREP/'CaptureCounters.cs')==HELPER and sha(PREP/'ReceiverPhaseBootstrap.cs')==BOOTSTRAP,'exact-core-helper-bootstrap')
 before=bind(Q2/'inputs-before.json');after=bind(Q2/'inputs-after.json');need(before==after,'all-input-maps-equal')
 expected={str(PREP/'preparation-receipt.json'):PREP_SHA}
 for field,base in [('filesSha256',PREP),('currentExternalInputsSha256',ROOT),('sourceSha256',ROOT),('app147Sha256',ROOT)]:
  for name,h in preparation[field].items():
   p=Path(name);p=p if p.is_absolute()else base/p
   if str(p)in expected:need(expected[str(p)]==h,'consistent-canonical-input')
   expected[str(p)]=h
 expected[str(MANIFEST)]=MANIFEST_SHA
 for rows in [manifest['commonFiles'],manifest['resources']]+[x['files']for x in manifest['entries'].values()]+list(manifest['sdk'].values()):
  for row in rows.values():
   p=row['path'];h=row['sha256']
   if p in expected:need(expected[p]==h,'consistent-runtime-input')
   expected[p]=h
 need(before==expected,'complete-expected-input-union')
 for path,h in expected.items():need(sha(path)==h,'current-source-runtime-input-unchanged')
 # All copied runtime folders have the exact same non-SDK bytes and only SDK DLL/PDB differ.
 for role,entry in manifest['entries'].items():
  variants={}
  for variant,sdk in manifest['sdk'].items():
   directory=Q2/'runtime'/role/variant;binding=bind(directory/'runtime-binding.json')
   need(binding['variant']==variant and binding['role']==role and binding['entryAssemblyName']==ENTRIES[role],'runtime-binding-role')
   rows=dict(manifest['commonFiles']);rows.update(entry['files']);rows.update(manifest['resources']);rows.update({'Hexalith.EventStore.ServiceDefaults.dll':sdk['dll'],'Hexalith.EventStore.ServiceDefaults.pdb':sdk['pdb']})
   want={n:r['sha256']for n,r in rows.items()};need(binding['filesSha256']==want,'exact-flat-closure')
   for name,h in want.items():need(sha(directory/name)==h,'copied-runtime-current-hash')
   variants[variant]=want
  need({n for n in variants['ordinary']if variants['ordinary'][n]!=variants['private'][n]}=={'Hexalith.EventStore.ServiceDefaults.dll','Hexalith.EventStore.ServiceDefaults.pdb'},'only-sdk-pair-differs')
 producer=bind(PRODUCER/'receipt.json',PRODUCER_SHA);pa=bind(PRODUCER/'parent-producer-audit.json',PRODUCER_AUDIT_SHA)
 need(producer['completed']is True and producer['exitCode']==0 and producer['rootModulesUnchanged']is True and producer['sourceCoreInputsUnchanged']is True and pa['passed']is True and pa['actualCompilerInputsRehashed']==502,'isolated-producer-audit')
 need(producer['candidateDllSha256']==manifest['sdk']['private']['dll']['sha256'] and producer['candidatePdbSha256']==manifest['sdk']['private']['pdb']['sha256'],'producer-runtime-sdk-binding')
 parent=bind(args.parent_audit,args.parent_audit_sha256);need(parent.get('passed')is True,'root-parent-audit-pass-required')
 records=[];seen=set();by_key={};native=collections.Counter();stage_totals=collections.Counter();raw_rows=0;cap_processes=io_processes=0
 expected_order=[('folders','private',m,'native-success')for m in ['canary-positive-control','canary-current','canary-suppressed']]+[(r,'private','active','execution')for r in ['projects','tenants']]+[('excluded','private','excluded','native-success')]+[('folders',v,'native',g)for v in ['ordinary','private']for g in NATIVE]+[('folders',v,m,g)for m in MODES for g in GROUPS for v in ['ordinary','private']]
 need(len(summary['receipts'])==len(expected_order)==62,'all-62-receipts')
 for index,(ref,key)in enumerate(zip(summary['receipts'],expected_order),22002):
  work=Path(ref['receipt']).parent;need(work.parent==Q2/'processes' and ref['passed']is True,'owned-process-path-pass')
  r=bind(ref['receipt'],ref['sha256']);role,variant,mode,group=key
  need((r['role'],r['variant'],r['mode'],r['group'])==key and r['started']is True and r['passed']is True and r['exitCode']==0,'exact-process-order-exit')
  need(type(r['pid'])is int and r['pid']>0 and r['groupId']==r['pid'] and re.fullmatch(r'[0-9]+',r['startTicks'])is not None,'actual-process-identity')
  need(r['pid']not in seen,'fresh-distinct-process');seen.add(r['pid'])
  need(r['processGroupAbsent']is True and r['remainingGroup']==[] and r['signalsSent']is False and r['directoryReleased']is True,'closed-process-cleanup')
  need(proc(r['pid'])is None and group_absent(r['groupId']),'actual-pid-group-absence')
  need(r['featureAcceptance']is False and r['rawBusinessValuesPersisted']is False,'process-privacy-limits')
  command=r['command'];directory=Q2/'runtime'/role/variant
  need(command[:2]==['/home/administrator/.dotnet/dotnet',str(directory/(ENTRIES[role]+'.dll'))]and command[2:4]==[mode,group],'real-entry-command')
  need(command[4]==str(directory/'runtime-binding.json')and command[5]==sha(directory/'runtime-binding.json')and command[6]==str(work/'business.json'),'bound-child-arguments')
  facts=bind(work/'runtime-facts.json');need(facts['Variant']==variant and facts['Role']==role and facts['Entry']==ENTRIES[role]and facts['Runtime']=='10.0.14'and facts['Processors']==14,'actual-runtime-entry-cpu')
  need(facts['ActualDefaultContextOnly']is True and facts['HistoricalSdkFallback']is False and facts['SdkSha256']==manifest['sdk'][variant]['dll']['sha256'],'default-context-no-sdk-fallback')
  need(facts['TestDllSha256']==manifest['commonFiles']['Hexalith.EventStore.Server.Tests.dll']['sha256']and facts['CompanionSha256']==manifest['commonFiles']['Hexalith.EventStore.Server.LiveSidecar.Tests.dll']['sha256'],'same-tests-companion')
  business=bind(work/'business.json',r['businessSha256']);need(business['Passed']is True,'business-pass')
  recording=variant=='private'and mode not in ['disabled','canary-positive-control','excluded','native']
  if mode=='native':
   need(len(command)==8 and command[7]==str(work/'native.xml'),'native-xml-argument')
   xml=raw(work/'native.xml');need(b'<!DOCTYPE'not in xml and b'<!ENTITY'not in xml,'native-xml-no-entities');need(digest(xml)==r['nativeXmlSha256'],'native-xml-hash');evidence[str(work/'native.xml')]=digest(xml)
   tree=ET.fromstring(xml);tests=tree.findall('.//test');need(len(tests)==NATIVE[group] and all(t.attrib.get('result')=='Pass'for t in tests)and not tree.findall('.//errors/error'),'native-all-pass-zero-error')
   actual=collections.Counter((digest(t.attrib['name'].encode()),t.attrib['type'],t.attrib['method'])for t in tests)
   selection=next(x for x in reference['selections']if x['filter']=='Hexalith.EventStore.Server.Tests.Routing.'+group)
   wanted=collections.Counter({(x['parsedXmlNameSha256'],x['type'],x['method']):x['multiplicity']for x in selection['expectedDisplayMultiset']})
   need(actual==wanted and r['nativeCases']==NATIVE[group]and business['ExpectedCases']==NATIVE[group]and business['NativeAssertionsUnchanged']is True,'exact-native-name-multiset')
   native[variant]+=len(tests)
  elif mode in MODES:
   need(business['Mode']==mode and business['Group']==group and business['AllSetupBeforeReady']is True and business['RawBusinessValuesPersisted']is False and business['ActualCounterCountsAndAllocationsZero']is True,'focused-closed-business')
   need(len(business['Cases'])==len(GROUPS[group]),'focused-case-count')
   for item,name in zip(business['Cases'],GROUPS[group]):
    need(set(item)=={'Result','CaptureOrdinal','AdmittedDelta','ActualStageRowsRequired','SyntheticDurationBracketOnly'}and item['Result']['Case']==name,'focused-case-order-schema');result(item['Result'])
    integers(item,['CaptureOrdinal','AdmittedDelta']);need(item['AdmittedDelta']==int(recording)and item['ActualStageRowsRequired']==(recording and mode=='active'),'all-eligible-cases-actually-admitted')
    need(item['SyntheticDurationBracketOnly']==name.startswith('technical-double-dispose-'),'double-dispose-bracket-classification')
   by_key[(mode,group,variant)]=business
  elif mode=='excluded':
   result(business['Result']);need(business['HelperInspected']is False and business['Admitted']is None and business['LivedTicks']>=12*business['Frequency']and business['SourceGateAndNoArtifactsOnly']is True and business['InvisibleWorkerAbsenceClaimed']is False and business['CanaryValuesPersisted']is False,'genuine-excluded-observed-only')
  else:
   need(business['Mode']==mode and business['CallerPreserved']is True and business['CanaryValuesPersisted']is False,'first-business-canary-pass')
   positive=mode=='canary-positive-control';need(business['UnsafeDetectorPositive']==positive and business['HelperInspected']==(not positive),'canary-positive-detector-and-first-business')
   need((positive and business['CaptureOrdinal']==0 and business['ExpectedStages']==0)or(not positive and business['ProbeChecks']>=4 and business['CaptureOrdinal']>0 and business['ExpectedStages']==191),'canary-progress-actual-capture')
  artifacts=r['artifacts'];closed=r['closed'];by_capture=collections.defaultdict(dict);counts=collections.Counter()
  output_role='folders'if role=='excluded'else role
  if recording:
   need(set(artifacts)=={'window.control',output_role+'.jsonl',output_role+'.closed.json'}and type(closed)is dict and set(closed)==CLOSED,'strict-recording-artifacts')
   integers(closed,CLOSED-{'role'});need(closed['role']==output_role and closed['diagnosticOnly']==1 and closed['featureAcceptance']==0 and closed['runOrdinal']==index,'closed-role-ordinal')
   need(closed['admitted']==closed['disposedOrUnwound']and closed['activeAtDrainEnd']==0 and closed['countersFinal']==1 and all(closed[k]==0 for k in ['incompleteHardDrain','queuedAtDrainEnd','lateDisposedOrDropCountersMayIncrease']),'final-closed-counters')
   need((closed['maximumRows'],closed['maximumBytes'],closed['queueCapacity'])==(65536,8388608,4096)and closed['frequency']>0 and closed['windowEndTicks']-closed['windowStartTicks']==8*closed['frequency'],'unchanged-recorder-bounds-window')
   for name in [output_role+'.jsonl',output_role+'.closed.json']:
    a=artifacts[name];data=raw(work/name,8388608);need(digest(data)==a['sha256']and len(data)==a['bytes']and a['contentsArchived']is True,'closed-artifact-hash-bytes');evidence[str(work/name)]=digest(data)
   need(load(work/(output_role+'.closed.json'))==closed,'closed-archive-equals-receipt')
   data=raw(work/(output_role+'.jsonl'),8388608)
   for line in data.splitlines():
    row=decode(line);need(set(row)==ROW and row['role']==output_role and row['stage']in STAGES,'strict-eight-stage-row-schema');integers(row,ROW-{'role','stage'});need(all(row[k]==0 for k in ZERO),'all-count-allocation-columns-zero')
    need(row['runOrdinal']==index and row['frequency']==closed['frequency']and row['captureOrdinal']>0 and row['stage']not in by_capture[row['captureOrdinal']],'unique-local-capture-stage')
    by_capture[row['captureOrdinal']][row['stage']]=row['elapsedTicks'];counts[row['stage']]+=1
   need(dict(counts)==r['stageCounts']and sum(counts.values())==closed['writtenRows']and len(data)==closed['writtenBytes'],'raw-counts-bytes-match-final')
   if mode=='pre-capped':need(closed['capRowsDropped']>0 and closed['ioFailures']==0,'actual-pre-cap-drop');cap_processes+=1
   elif mode=='failing-sink':need(closed['ioFailures']>0 and closed['writtenRows']==0,'actual-failing-sink');io_processes+=1
   else:
    need(all(closed[k]==0 for k in ['capRowsDropped','ioFailures','queuedRowsDropped','closedRowsDropped']),'lossless-active-canary')
    need(set(by_capture)==set(range(1,closed['admitted']+1)),'all-admitted-active-captures-present')
    for stages in by_capture.values():need('Whole'in stages and all(n<=stages['Whole']for n in stages.values()),'nested-stages-bounded-by-whole')
   if mode=='active':
    for item in business['Cases']:
     observed=by_capture[item['CaptureOrdinal']];mask=item['Result']['ExpectedStageMask'];need(set(observed)=={STAGES[i]for i in range(8)if mask&(1<<i)},'exact-actual-middleware-stage-mask')
     if item['SyntheticDurationBracketOnly']:
      a=item['Result'];need(a['MinimumLastDisposeTicks']<=observed[STAGES[5]]<=a['MaximumLastDisposeTicks']<a['FirstDisposeHoldTicks'],'actual-last-release-interval')
   if mode in ['canary-current','canary-suppressed']:need(set(by_capture[business['CaptureOrdinal']])=={STAGES[i]for i in range(8)if business['ExpectedStages']&(1<<i)},'actual-canary-native-stage-mask')
   stage_totals.update(counts);raw_rows+=sum(counts.values())
  else:
   need(closed is None and r['stageCounts']=={},'nonrecording-no-helper-state')
   need(set(artifacts)==({'window.control'}if mode=='excluded'else set()),'nonrecording-no-counter-artifacts')
  if 'window.control'in artifacts:need(artifacts['window.control']['contentsArchived']is False and not(work/'window.control').exists(),'numeric-control-not-archived')
  records.append({'path':str(work/'controller-receipt.json'),'sha256':ref['sha256'],'mode':mode,'variant':variant,'role':role,'group':group})
 need(native=={'ordinary':47,'private':47}and cap_processes==6 and io_processes==6,'native-cap-sink-totals')
 pair_count=0
 for mode in MODES:
  for group in GROUPS:
   a=by_key[(mode,group,'ordinary')]['Cases'];b=by_key[(mode,group,'private')]['Cases']
   for x,y in zip(a,b):need({k:x['Result'][k]for k in SEMANTIC}=={k:y['Result'][k]for k in SEMANTIC},'ordinary-private-semantic-parity');pair_count+=1
 need(pair_count==64,'all-64-semantic-pairs')
 s=PRIVATE.lstat();need(stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)and(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and not any(PRIVATE.iterdir()),'same-pinned-empty-private-directory')
 # Preserve both the initial failed source/runtime lineage and the accepted correction/build receipts.
 for p,h in [(Path(preparation['priorQualificationAttempt']['preservedPreparationPath']),preparation['priorQualificationAttempt']['preservedPreparationSha256']),(Path(preparation['priorQualificationAttempt']['preservedRuntimeManifestPath']),preparation['priorQualificationAttempt']['preservedRuntimeManifestSha256']),(Path('/tmp/story65-receiver-phase-entry-build-retry-20261107/receipt.json'),'3bcc478c1c47b88b31d18bb1cba8f4a8abab3038412ea5ed90095cd89bdc15de')]:bind(p,h)
 audit={'qualified':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'coreSha256':CORE,'helperSha256':HELPER,'bootstrapSha256':BOOTSTRAP,'sourceSha256':preparation['sourceSha256'],'sourceCount':56,'sourcePreparationSha256':PREP_SHA,'runtimeManifestSha256':MANIFEST_SHA,'producerReceiptSha256':PRODUCER_SHA,'parentProducerAuditSha256':PRODUCER_AUDIT_SHA,'parentAudit':{'path':args.parent_audit,'sha256':args.parent_audit_sha256},'passingFocusedSemanticPairs':64,'passingFirstBusinessCanaries':3,'controlledProcesses':62,'all62ActualPidsAndGroupsAbsent':True,'passingNativeCasesPerVariant':dict(native),'diagnosticNativeExecutions':94,'additionalRolePositiveCases':8,'excludedEntryCases':1,'strictValidatedRows':raw_rows,'stageRowCounts':dict(stage_totals),'allCountAndAllocationColumnsZero':True,'allCountersFinal':True,'activeExactPerCaseAdmissionAndStageMasks':True,'actualDoubleDisposeLastIntervalBracketPassed':True,'actualPreCappedProcesses':cap_processes,'actualFailingSinkProcesses':io_processes,'currentInputPathsRehashed':len(expected),'sourceAndRuntimeInputsBeforeAfterIdentical':True,'fixedPrivateDirectorySameIdentityAndEmpty':True,'onlySdkDllPdbDifferBetweenVariantClosures':True,'noDefaultSdkFallback':True,'matchedDebugRuntimeChecksExecutedByPinnedDriver':True,'runtime':'10.0.14','naturalProcessorCount':14,'ordinaryNative1048UnchangedNotInflated':True,'applicationHostsStartedByThisAudit':0,'counterWindowsStartedByThisAudit':0,'nativeAppLiveFeatureAcceptancePending':True,'priorAttemptFailureAndCorrectionPreserved':True,'unknownCrossmoduleOldVsCurrentProvenanceLimitsRetained':True,'allDependencyGenerationsCurrentProductionCoherentClaimed':False,'stage5Meaning':'last completed or unwound original DisposeAsync interval per capture; not total, callback duration or count','wholeMeaning':'excludes entry-name eligibility and initial admission-lock wait; includes post-timestamp capture construction/admission bookkeeping; stops before queue writes','measurementLimits':['diagnostic scratch parity only; no host deadline or feature acceptance claim','overlapping stages are not disjoint and residual is unattributed','stage absence is not a request kind, outcome or cancellation signal','no old-probe, downstream, trace or request ordinal joins','capped and failing-sink modes prove semantic/admission parity and explicit loss, not complete per-case rows','excluded process proves observed artifacts absent with source gate, not invisible helper-worker absence','only three genuine eligible synthetic entries and one genuine excluded entry were executed','original and private native47 are diagnostic executions; ordinary native1048 is unchanged'],'receipts':records,'evidenceSha256':evidence,'auditScriptSha256':sha(Path(__file__))}
 out.parent.mkdir(parents=True,exist_ok=True);fd=os.open(out,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600)
 with os.fdopen(fd,'w')as stream:json.dump(audit,stream,indent=2);stream.write('\n')
 print(json.dumps({'qualified':True,'focusedPairs':64,'diagnosticNativeExecutions':94,'processes':62,'strictRows':raw_rows,'receiptSha256':sha(out)}))

if __name__=='__main__':
 try:main()
 except AuditFailure as error:print(json.dumps({'qualified':False,'closedFailure':str(error)}));raise SystemExit(1)
