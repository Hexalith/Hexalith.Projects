#!/usr/bin/env python3
"""Audit a CLOSED domain receiver qualifier; never starts builds, children, hosts or counter windows."""
from pathlib import Path
import argparse, collections, datetime, hashlib, json, os, re, stat
import xml.etree.ElementTree as ET

ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-domain-receiver-phase-prep-20261108')
Q2=Path('/tmp/story65-domain-receiver-phase-qualification-native-output-retry-20261108')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
PREP_SHA='91282cf8bd429d6f301f3f0edbe5c2c0224eff81c7b7490941a6c5bf356973af'
# Exact new runtime-manifest path/SHA is supplied by Root after its independent postbuild review.
PRODUCER=Path('/tmp/story65-domain-receiver-phase-producer-prep-20261108/producer-attempts/attempt-i0bt41o7')
PRODUCER_SHA='39ba5bdd1ba6a1760d2e12368ab59852c8769ae204d1bef79776df722834b644'
PRODUCER_AUDIT_SHA='6e369fd5f63e8e0fac3ef92a761e3fc3b53c5f40f6b87c7f7bd1b45c0505e5b5'
CORE={'DomainRouteCatalogFilter': '121ae30a04c76268f6fee5fa725730188579d7ec741ad3e293df27b1e5bb9a06', 'DomainRouteCatalogGuard': '50ba9c6b9200c6463aded2bb13f64af2ce5b775b94ce6520aded74e3d186f953', 'DomainRouteCatalogResult': '304eba32f8ec5ef2bc8832425a8996a2ebd721b2d58ec9b6bf777d39420f8ca7'}
HELPER='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
BOOTSTRAP='ff22ad6362bf4f7dea2ffaa782066dc719f1fcbf6e9205e50ee3df9df30df495'
REFERENCE=PREP/'native-reference-case-set.json'
REFERENCE_SHA='c692fb41ac9e9cf01841b28c7d56e597ea504cc587d6dedce6d8fead9e9d176a'
STAGES=['Whole','ComponentFile','ComponentPolicy','AclFile','SettingsEnumerationSortDictionary','AuthenticationSerialization','ActorBoundary','ResultTail']
ZERO={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
ROW=ZERO|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
CLOSED={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
MODES=['disabled','active','pre-capped','failing-sink']
GROUPS={'returns': ['custom-no-lease-nonresult', 'custom-owned-nonresult', 'concrete-delayed-result', 'terminal-refusal-owned-result'], 'faults': ['concrete-result-fault', 'concrete-result-cancel', 'concrete-release-override', 'result-release-cancel-override'], 'guard-next': ['filter-next-fault', 'filter-next-cancel', 'guard-acquire-fault', 'guard-validation-fault'], 'nested': ['nested-two-results-success', 'nested-inner-result-fault'], 'double-success': ['filter-double-dispose-retry-success'], 'double-fault': ['filter-double-dispose-second-fault']}
# Acquires, next calls, durable release callbacks, injected faults, exact stage mask, error type.
CASES={'custom-no-lease-nonresult': (1, 0, 1, 0, 0, 147, None), 'custom-owned-nonresult': (1, 0, 1, 1, 0, 179, None), 'concrete-delayed-result': (1, 1, 1, 1, 0, 255, None), 'concrete-result-fault': (1, 1, 1, 1, 1, 255, 'System.InvalidOperationException'), 'concrete-result-cancel': (1, 1, 1, 1, 1, 255, 'System.OperationCanceledException'), 'concrete-release-override': (1, 1, 1, 1, 2, 255, 'System.IO.IOException'), 'filter-next-fault': (1, 1, 1, 1, 1, 191, 'System.InvalidOperationException'), 'filter-next-cancel': (1, 1, 1, 1, 1, 191, 'System.OperationCanceledException'), 'guard-acquire-fault': (1, 1, 0, 0, 1, 135, None), 'guard-validation-fault': (1, 1, 0, 1, 0, 175, None), 'terminal-refusal-owned-result': (1, 1, 0, 1, 0, 255, None), 'filter-double-dispose-retry-success': (1, 0, 1, 2, 1, 179, 'Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogUnavailableException'), 'filter-double-dispose-second-fault': (1, 0, 1, 2, 2, 179, 'System.IO.IOException'), 'nested-two-results-success': (2, 2, 2, 2, 0, 255, None), 'nested-inner-result-fault': (2, 2, 2, 2, 1, 255, 'System.InvalidOperationException'), 'result-release-cancel-override': (1, 1, 1, 1, 2, 255, 'System.OperationCanceledException')}
SEMANTIC={'Case','Passed','FilterEntries','AuthorityAcquireCalls','NextCalls','ReleaseCallbacks','ErrorType','Faults','TokensMatched','LifetimeAndHandoffMatched','CallerRestored','ExpectedStageMask','DoubleDisposeOrder','ReleaseBoundaryRestored','ExactContextReachedNext','FirstDependencyProbeObserved','EntrySuppressionObserved'}
RESULT=SEMANTIC|{'MinimumLastDisposeTicks','MaximumLastDisposeTicks','FirstDisposeHoldTicks'}
ENTRIES={'projects':'Hexalith.Projects.Server','tenants':'Hexalith.Tenants','folders':'Hexalith.Folders.Server','excluded':'Story65.DomainReceiverPhase.Excluded'}
NATIVE={'DevelopmentDomainRouteCatalogTests': 14, 'DomainSidecarCatalogLeaseTests': 12, 'CatalogReceiverScopeTests': 41, 'CatalogReceiverStateBoundaryTests': 16, 'CatalogReceiverVersionTests': 7, 'TechnicalHttpDomainPeerTests': 12, 'HeldMetadataDispatchTests.RealDomainFilterAndResultComposeWithSdkMetadataClient': 1}

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
 for k in ['FilterEntries','AuthorityAcquireCalls','NextCalls','ReleaseCallbacks','Faults','ExpectedStageMask','MinimumLastDisposeTicks','MaximumLastDisposeTicks','FirstDisposeHoldTicks']:integers(r,[k])
 for k in SEMANTIC-{'Case','ErrorType','FilterEntries','AuthorityAcquireCalls','NextCalls','ReleaseCallbacks','Faults','ExpectedStageMask'}:need(type(r[k])is bool,'result-boolean-types')
 need(r['FilterEntries']==CASES[r['Case']][0],'actual-filter-entry-count')
 need((r['FilterEntries'],r['AuthorityAcquireCalls'],r['NextCalls'],r['ReleaseCallbacks'],r['Faults'],r['ExpectedStageMask'],r['ErrorType'])==CASES[r['Case']],'exact-case-counts-faults-mask-error')
 for k in ['Passed','TokensMatched','LifetimeAndHandoffMatched','CallerRestored','DoubleDisposeOrder','ReleaseBoundaryRestored','ExactContextReachedNext','EntrySuppressionObserved']:need(r[k]is True,'case-invariant')
 need(r['FirstDependencyProbeObserved']is True,'real-first-dependency-observed')
 if r['Case'].startswith('filter-double-dispose-'):
  need(0<r['MinimumLastDisposeTicks']<=r['MaximumLastDisposeTicks']<r['FirstDisposeHoldTicks'],'last-release-not-first-or-total-bracket')


def main():
 parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--runtime-manifest',required=True);parser.add_argument('--runtime-manifest-sha256',required=True);parser.add_argument('--parent-audit',required=True);parser.add_argument('--parent-audit-sha256',required=True);parser.add_argument('--output',required=True)
 args=parser.parse_args();need(args.execute,'explicit-audit-execute-required')
 MANIFEST=Path(args.runtime_manifest);MANIFEST_SHA=args.runtime_manifest_sha256
 need(MANIFEST.is_absolute()and MANIFEST.is_relative_to('/tmp')and re.fullmatch('[0-9a-f]{64}',MANIFEST_SHA)is not None,'root-reviewed-runtime-manifest-pin')
 out=Path(args.output);need(out.is_absolute()and out.is_relative_to('/tmp')and not out.exists(),'fresh-temp-output')
 evidence={}
 def bind(path,expected=None):
  p=Path(path);h=sha(p);need(expected is None or h==expected,'pinned-evidence-hash');evidence[str(p)]=h;return load(p)
 # No /proc or private-directory inspection until the complete closed summary has passed.
 summary=bind(Q2/'qualification-summary.json')
 need(summary['passed']is True and summary['processes']==68 and summary['focusedPairs']==64,'closed-complete-q2-required')
 need(summary['nativePassesPerVariant']=={'ordinary':103,'private':103}and summary['firstBusinessCanaries']==3 and summary['additionalRolePositiveCases']==8 and summary['excludedCases']==1,'summary-counts')
 need(summary['sourceAndRuntimeInputsUnchanged']is True and summary['noNativeCountInflation']is True and summary['rawRequestOrTraceValuesPersisted']is False,'summary-limits')
 preparation=bind(PREP/'preparation-receipt.json',PREP_SHA);manifest=bind(MANIFEST,MANIFEST_SHA);reference=bind(REFERENCE,REFERENCE_SHA)
 need(manifest['sourcePreparationSha256']==PREP_SHA and len(preparation['sourceSha256'])==59 and len(preparation['app147Sha256'])==147,'preparation-runtime-source-counts')
 need(all(sha(PREP/(name+'.private.cs'))==h for name,h in CORE.items())and sha(PREP/'CaptureCounters.cs')==HELPER and sha(PREP/'ReceiverPhaseBootstrap.cs')==BOOTSTRAP,'exact-three-core-helper-bootstrap')
 before=bind(Q2/'inputs-before.json');after=bind(Q2/'inputs-after.json');need(before==after,'all-input-maps-equal')
 expected={str(PREP/'preparation-receipt.json'):PREP_SHA}
 for field,base in [('filesSha256',PREP),('currentExternalInputsSha256',ROOT),('sourceSha256',ROOT),('app147Sha256',ROOT)]:
  for name,h in preparation[field].items():
   p=Path(name);p=p if p.is_absolute()else base/p
   if str(p)in expected:need(expected[str(p)]==h,'consistent-canonical-input')
   expected[str(p)]=h
 expected[str(MANIFEST)]=MANIFEST_SHA
 for rows in [manifest['commonFiles'],manifest['resources']]+[x['files']for x in manifest['entries'].values()]+list(manifest['domainService'].values()):
  for row in rows.values():
   p=row['path'];h=row['sha256']
   if p in expected:need(expected[p]==h,'consistent-runtime-input')
   expected[p]=h
 need(before==expected,'complete-expected-input-union')
 for path,h in expected.items():need(sha(path)==h,'current-source-runtime-input-unchanged')
 # All copied runtime folders have identical non-DomainService bytes, including the ordinary SDK pair.
 for role,entry in manifest['entries'].items():
  variants={}
  for variant,sdk in manifest['domainService'].items():
   directory=Q2/'runtime'/role/variant;binding=bind(directory/'runtime-binding.json')
   need(binding['variant']==variant and binding['role']==role and binding['entryAssemblyName']==ENTRIES[role]and binding['domainServiceSha256']==sdk['dll']['sha256']and binding['sdkSha256']==manifest['commonFiles']['Hexalith.EventStore.ServiceDefaults.dll']['sha256'],'runtime-binding-role-and-both-carriers')
   rows=dict(manifest['commonFiles']);rows.update(entry['files']);rows.update(manifest['resources']);rows.update({'Hexalith.EventStore.DomainService.dll':sdk['dll'],'Hexalith.EventStore.DomainService.pdb':sdk['pdb']})
   want={n:r['sha256']for n,r in rows.items()};need(binding['filesSha256']==want,'exact-flat-closure')
   for name,h in want.items():need(sha(directory/name)==h,'copied-runtime-current-hash')
   variants[variant]=want
  need({n for n in variants['ordinary']if variants['ordinary'][n]!=variants['private'][n]}=={'Hexalith.EventStore.DomainService.dll','Hexalith.EventStore.DomainService.pdb'},'only-domain-pair-differs')
 need(manifest['commonFiles']['Hexalith.EventStore.ServiceDefaults.dll']['sha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'and manifest['commonFiles']['Hexalith.EventStore.ServiceDefaults.pdb']['sha256']=='ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6','same-ordinary-sdk-pair')
 producer=bind(PRODUCER/'receipt.json',PRODUCER_SHA);pa=bind(PRODUCER/'parent-producer-audit.json',PRODUCER_AUDIT_SHA)
 need(producer['completed']is True and producer['exitCode']==0 and producer['rootModulesUnchanged']is True and producer['coreInputsUnchanged']is True and pa['passed']is True and producer['sourceCorePrivateSha256']==CORE and len(bind(PRODUCER/'observed-compiler-inputs.json')['observedInputs'])==636,'isolated-producer-audit')
 for row in bind(PRODUCER/'observed-compiler-inputs.json')['observedInputs']:need(sha(row['path'])==row['sha256'],'actual-producer-compiler-input-current-hash')
 need(producer['candidateDllSha256']==manifest['domainService']['private']['dll']['sha256'] and producer['candidatePdbSha256']==manifest['domainService']['private']['pdb']['sha256'],'producer-runtime-domain-binding')
 parent=bind(args.parent_audit,args.parent_audit_sha256);need(parent.get('passed')is True,'root-parent-audit-pass-required')
 records=[];seen=set();by_key={};native=collections.Counter();stage_totals=collections.Counter();raw_rows=0;cap_processes=io_processes=0
 expected_order=[('folders','private',m,'concrete-delayed-result')for m in ['canary-positive-control','canary-current','canary-suppressed']]+[(r,'private','active','returns')for r in ['projects','tenants']]+[('excluded','private','excluded','concrete-delayed-result')]+[('folders',v,'native',g)for v in ['ordinary','private']for g in NATIVE]+[('folders',v,m,g)for m in MODES for g in GROUPS for v in ['ordinary','private']]
 need(len(summary['receipts'])==len(expected_order)==68,'all-68-receipts')
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
  need(facts['ActualDefaultContextOnly']is True and facts['HistoricalSdkOrDomainFallback']is False and facts['SameOrdinarySdkBothVariants']is True and facts['DomainServiceSha256']==manifest['domainService'][variant]['dll']['sha256']and facts['SdkSha256']==manifest['commonFiles']['Hexalith.EventStore.ServiceDefaults.dll']['sha256'],'default-context-no-sdk-or-domain-fallback')
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
   assemblies=tree.findall('assembly');need(len(assemblies)==1 and all(int(assemblies[0].get(k,'0'))==0 for k in ['errors','failed','skipped','not-run']),'native-zero-errors-failures-skips-notrun')
   native[variant]+=len(tests)
  elif mode in MODES:
   need(business['Mode']==mode and business['Group']==group and business['AllSetupBeforeReady']is True and business['RawBusinessValuesPersisted']is False and business['ActualCounterCountsAndAllocationsZero']is True,'focused-closed-business')
   need(len(business['Cases'])==len(GROUPS[group]),'focused-case-count')
   case_ordinals=set()
   for item,name in zip(business['Cases'],GROUPS[group]):
    need(set(item)=={'Result','CaptureOrdinals','AdmittedDelta','ActualStageRowsRequired','HandoffCounterObservationRequired','HandoffChecks','SyntheticDurationBracketOnly'}and item['Result']['Case']==name,'focused-case-order-schema');result(item['Result'])
    integers(item,['AdmittedDelta','HandoffChecks']);need(type(item['ActualStageRowsRequired'])is bool and type(item['SyntheticDurationBracketOnly'])is bool,'typed-stage-and-bracket-flags');ordinals=item['CaptureOrdinals'];need(type(ordinals)is list and all(type(n)is int and n>0 for n in ordinals),'typed-own-capture-ordinals')
    need(item['AdmittedDelta']==(item['Result']['FilterEntries']if recording else 0)and item['ActualStageRowsRequired']==(recording and mode=='active'),'all-eligible-filter-entries-actually-admitted')
    need((not recording and ordinals==[])or(recording and len(ordinals)==item['Result']['FilterEntries']and ordinals==list(range(ordinals[0],ordinals[0]+len(ordinals)))),'consecutive-separate-nested-captures')
    need(not(case_ordinals&set(ordinals)),'separate-local-ordinals-for-each-case');case_ordinals.update(ordinals)
    deferred=item['Result']['ExpectedStageMask']==255;required=recording and deferred
    need(type(item['HandoffCounterObservationRequired'])is bool and item['HandoffCounterObservationRequired']==required and item['HandoffChecks']==int(required),'actual-live-captures-at-deferred-handoff')
    need(item['SyntheticDurationBracketOnly']==name.startswith('filter-double-dispose-'),'double-dispose-bracket-classification')
   by_key[(mode,group,variant)]=business
  elif mode=='excluded':
   result(business['Result']);need(business['HelperInspected']is False and business['Admitted']is None and business['LivedTicks']>=12*business['Frequency']and business['SourceGateAndNoArtifactsOnly']is True and business['InvisibleWorkerAbsenceClaimed']is False and business['CanaryValuesPersisted']is False,'genuine-excluded-observed-only')
  else:
   need(business['Mode']==mode and business['CallerPreserved']is True and business['CanaryValuesPersisted']is False,'first-business-canary-pass')
   positive=mode=='canary-positive-control';need(business['UnsafeDetectorPositive']==positive and business['HelperInspected']==(not positive),'canary-positive-detector-and-first-business')
   need((positive and business['CaptureOrdinal']==0 and business['ExpectedStages']==0)or(not positive and business['ProbeChecks']==14 and business['CaptureOrdinal']>0 and business['ExpectedStages']==255),'canary-progress-actual-capture')
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
     mask=item['Result']['ExpectedStageMask']
     for ordinal in item['CaptureOrdinals']:
      observed=by_capture[ordinal];need(set(observed)=={STAGES[i]for i in range(8)if mask&(1<<i)},'exact-actual-filter-result-stage-mask')
      if item['SyntheticDurationBracketOnly']:
       a=item['Result'];need(a['MinimumLastDisposeTicks']<=observed[STAGES[5]]<=a['MaximumLastDisposeTicks']<a['FirstDisposeHoldTicks'],'actual-last-release-interval')
   if mode in ['canary-current','canary-suppressed']:need(set(by_capture[business['CaptureOrdinal']])=={STAGES[i]for i in range(8)if business['ExpectedStages']&(1<<i)},'actual-canary-native-stage-mask')
   stage_totals.update(counts);raw_rows+=sum(counts.values())
  else:
   need(closed is None and r['stageCounts']=={},'nonrecording-no-helper-state')
   need(set(artifacts)==({'window.control'}if mode=='excluded'else set()),'nonrecording-no-counter-artifacts')
  if 'window.control'in artifacts:need(artifacts['window.control']['contentsArchived']is False and not(work/'window.control').exists(),'numeric-control-not-archived')
  records.append({'path':str(work/'controller-receipt.json'),'sha256':ref['sha256'],'mode':mode,'variant':variant,'role':role,'group':group})
 need(native=={'ordinary':103,'private':103}and cap_processes==6 and io_processes==6,'native-cap-sink-totals')
 pair_count=0
 for mode in MODES:
  for group in GROUPS:
   a=by_key[(mode,group,'ordinary')]['Cases'];b=by_key[(mode,group,'private')]['Cases']
   for x,y in zip(a,b):need({k:x['Result'][k]for k in SEMANTIC}=={k:y['Result'][k]for k in SEMANTIC},'ordinary-private-semantic-parity');pair_count+=1
 need(pair_count==64,'all-64-semantic-pairs')
 for variant in ['ordinary','private']:
  need(sum(item['Result']['FilterEntries']for (mode,group,v),business in by_key.items()if v==variant for item in business['Cases'])==72,'all72-focused-filter-entries-per-variant')
 s=PRIVATE.lstat();need(stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)and(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and not any(PRIVATE.iterdir()),'same-pinned-empty-private-directory')
 # Bind the exact completed fixture-only build generation and its command/artifact receipts.
 build=bind('/tmp/story65-domain-receiver-phase-fixture-build-20261108/receipt.json','a5971947cc0959099bf1cd07c235a5ee1ce6ec2a4b9bcf8bef1f29ef57fecd56')
 need(build['all12CommandsClosedZero']is True and build['allSixDebugBuildsZeroWarningsAndErrors']is True and build['all1570CurrentInputsUnchanged']is True,'actual-fixture-build-inputs-closed')
 audit={'qualified':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'coreSha256':CORE,'helperSha256':HELPER,'bootstrapSha256':BOOTSTRAP,'sourceSha256':preparation['sourceSha256'],'sourceCount':59,'sourcePreparationSha256':PREP_SHA,'runtimeManifestSha256':MANIFEST_SHA,'producerReceiptSha256':PRODUCER_SHA,'parentProducerAuditSha256':PRODUCER_AUDIT_SHA,'parentAudit':{'path':args.parent_audit,'sha256':args.parent_audit_sha256},'passingFocusedSemanticPairs':64,'passingFirstBusinessCanaries':3,'controlledProcesses':68,'all68ActualPidsAndGroupsAbsent':True,'passingNativeCasesPerVariant':dict(native),'diagnosticNativeExecutions':206,'additionalRolePositiveCases':8,'excludedEntryCases':1,'strictValidatedRows':raw_rows,'stageRowCounts':dict(stage_totals),'allCountAndAllocationColumnsZero':True,'allCountersFinal':True,'activeExactPerCaseAdmissionAndStageMasks':True,'actualDoubleDisposeLastIntervalBracketPassed':True,'actualPreCappedProcesses':cap_processes,'actualFailingSinkProcesses':io_processes,'currentInputPathsRehashed':len(expected),'sourceAndRuntimeInputsBeforeAfterIdentical':True,'fixedPrivateDirectorySameIdentityAndEmpty':True,'onlyDomainServiceDllPdbDifferBetweenVariantClosures':True,'noDefaultSdkOrDomainFallback':True,'matchedDebugRuntimeChecksExecutedByPinnedDriver':True,'runtime':'10.0.14','naturalProcessorCount':14,'ordinaryNative1048UnchangedNotInflated':True,'applicationHostsStartedByThisAudit':0,'counterWindowsStartedByThisAudit':0,'nativeAppLiveFeatureAcceptancePending':True,'sourceCorrectionsPreservedOutsideRuntimeInputs':True,'actualNestedTwoFilterCapturesAndDeferredHandoffVerified':True,'focusedFilterEntriesPerVariant':72,'sameOrdinarySdkBothVariants':True,'unknownCrossmoduleOldVsCurrentProvenanceLimitsRetained':True,'allDependencyGenerationsCurrentProductionCoherentClaimed':False,'stage5Meaning':'last original disposal interval per capture; result await-using region includes only disposal/unwind glue; not total, callback duration or count','wholeMeaning':'owned filter-to-result lifetime including delayed handoff; excludes entry eligibility and initial helper-lock wait, includes post-timestamp bookkeeping; unowned/refusal cases close locally','measurementLimits':['diagnostic scratch parity only; no host deadline or feature acceptance claim','overlapping stages are not disjoint and residual is unattributed','stage absence is not a request kind, outcome or cancellation signal','no old-probe, downstream, trace or request ordinal joins','capped and failing-sink modes prove semantic/admission parity and explicit loss, not complete per-case rows','excluded process proves observed artifacts absent with source gate, not invisible helper-worker absence','only three genuine eligible synthetic entries and one genuine excluded entry were executed','original and private native103 are diagnostic executions; ordinary native1048 is unchanged','two Folders-style nested filters preserve both acquisitions/results/captures; overlapping stages are not summed','static installation witness and typed scratch pipeline are not fresh live endpoint-factory inspection'],'receipts':records,'evidenceSha256':evidence,'auditScriptSha256':sha(Path(__file__))}
 out.parent.mkdir(parents=True,exist_ok=True);fd=os.open(out,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600)
 with os.fdopen(fd,'w')as stream:json.dump(audit,stream,indent=2);stream.write('\n')
 print(json.dumps({'qualified':True,'focusedPairs':64,'diagnosticNativeExecutions':206,'processes':68,'strictRows':raw_rows,'receiptSha256':sha(out)}))

if __name__=='__main__':
 try:main()
 except AuditFailure as error:print(json.dumps({'qualified':False,'closedFailure':str(error)}));raise SystemExit(1)
