from pathlib import Path
import json,hashlib,collections,os,stat,xml.etree.ElementTree as ET
Q=Path('/tmp/story65-receiver-phase-qualification-retry-20261107')
P=Path('/tmp/story65-receiver-phase-prep-20261107')
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def load(p):return json.loads(Path(p).read_bytes())
s=load(Q/'qualification-summary.json');assert s['passed'] is True and s['processes']==62 and s['focusedPairs']==64 and s['nativePassesPerVariant']=={'ordinary':47,'private':47} and s['sourceAndRuntimeInputsUnchanged'] is True
before=load(Q/'inputs-before.json');after=load(Q/'inputs-after.json');assert before==after
for p,h in before.items():assert sha(p)==h,p
reference=load('/tmp/story65-receiver-phase-ops-prep-20261107/native-reference-case-set.json')
expected={r['filter'].rsplit('.',1)[-1]:collections.Counter({(e['parsedXmlNameSha256'],e['type'],e['method']):e['multiplicity']for e in r['expectedDisplayMultiset']})for r in reference['selections']}
records=[];native=collections.Counter();paired={};raw_rows=0;groups=set();canaries=0;additional_roles=0;excluded=0;closed_count=0
semantic={'Case','Passed','MiddlewareEntries','AcquireCalls','NextCalls','ReleaseCallbacks','ErrorType','Faults','TokenMatched','LifetimeMatched','CallerRestored','ExpectedStageMask','DoubleDisposeOrder','ReleaseBoundaryRestored','ExactContextReachedNext','FirstDependencyProbeObserved','EntrySuppressionObserved'}
zero={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
fields=zero|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
stages=['Whole','ComponentFile','ComponentPolicy','AclFile','SettingsEnumerationSortDictionary','AuthenticationSerialization','ActorBoundary','ResultTail']
for row in s['receipts']:
 p=Path(row['receipt']);assert sha(p)==row['sha256'] and row['passed']is True
 r=load(p);assert r['passed']is True and r['processGroupAbsent']is True and r['remainingGroup']==[] and r['exitCode']==0 and r['signalsSent']is False and r['directoryReleased']is True
 assert r['rawBusinessValuesPersisted']is False and r['featureAcceptance']is False
 groups.add(r['groupId']);assert not Path('/proc',str(r['pid'])).exists()
 b=load(p.parent/'business.json');assert sha(p.parent/'business.json')==r['businessSha256']and b['Passed']is True
 if r['mode']=='native':
  x=p.parent/'native.xml';assert sha(x)==r['nativeXmlSha256'];tree=ET.parse(x);tests=tree.getroot().findall('.//test');assert not tree.getroot().findall('.//errors/error') and all(t.attrib['result']=='Pass'for t in tests)
  actual=collections.Counter((hashlib.sha256(t.attrib['name'].encode()).hexdigest(),t.attrib['type'],t.attrib['method'])for t in tests)
  assert actual==expected[r['group']];native[r['variant']]+=len(tests)
 elif r['mode'].startswith('canary-'):
  canaries+=1;assert b['CallerPreserved']is True and b['CanaryValuesPersisted']is False
 elif r['mode']=='excluded':
  excluded+=1;assert b['HelperInspected']is False and b['Admitted']is None and b['LivedTicks']>=12*b['Frequency']
 elif r['role']!='folders':additional_roles+=len(b['Cases'])
 else:
  for case in b['Cases']:
   c=case['Result'];assert c['Passed']is True
   paired.setdefault((r['mode'],c['Case']),{})[r['variant']]={k:c[k]for k in semantic}
 for name,a in r['artifacts'].items():
  if a['contentsArchived']:
   f=p.parent/name;assert sha(f)==a['sha256']and f.stat().st_size==a['bytes']
 if r.get('closed'):
  c=r['closed'];closed_count+=1;assert c['admitted']==c['disposedOrUnwound']and all(c[k]==0 for k in ['activeAtDrainEnd','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease']) and c['countersFinal']==1
  raw=p.parent/(r['role']+'.jsonl');captures=collections.defaultdict(dict);n=0
  for line in raw.read_bytes().splitlines():
   v=json.loads(line);assert set(v)==fields and all(type(v[k])is int and v[k]>=0 for k in fields-{'role','stage'})and all(v[k]==0 for k in zero)
   assert v['role']==r['role']and v['runOrdinal']==c['runOrdinal']and v['frequency']==c['frequency']and v['stage']in stages
   capture=captures[v['captureOrdinal']];assert v['stage']not in capture;capture[v['stage']]=v['elapsedTicks'];n+=1
  assert n==c['writtenRows']and raw.stat().st_size==c['writtenBytes'];raw_rows+=n
  if r['mode']=='pre-capped':assert c['capRowsDropped']>0 and c['ioFailures']==0
  elif r['mode']=='failing-sink':assert c['ioFailures']>0 and c['writtenRows']==0
  else:
   assert all(c[k]==0 for k in ['capRowsDropped','ioFailures','queuedRowsDropped','closedRowsDropped'])
   assert set(captures)==set(range(1,c['admitted']+1))
   assert all('Whole'in z and all(v<=z['Whole']for v in z.values())for z in captures.values())
   if r['mode']=='active':
    for case in b['Cases']:
     cse=case['Result'];actual=captures[case['CaptureOrdinal']];assert set(actual)=={stages[i]for i in range(8)if cse['ExpectedStageMask']&(1<<i)}
     if case['SyntheticDurationBracketOnly']:assert cse['MinimumLastDisposeTicks']<=actual[stages[5]]<=cse['MaximumLastDisposeTicks']<cse['FirstDisposeHoldTicks']
 records.append({'receipt':str(p),'sha256':sha(p)})
assert native=={'ordinary':47,'private':47}and len(paired)==64 and all(set(v)=={'ordinary','private'}and v['ordinary']==v['private']for v in paired.values())
assert (canaries,additional_roles,excluded)==(3,8,1)
for p in Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:f=(p/'stat').read_text().rsplit(')',1)[1].split();assert int(f[2])not in groups
 except (FileNotFoundError,ProcessLookupError):pass
private=Path('/tmp/story65-capture-counter-private-20261107');st=private.stat();assert(st.st_dev,st.st_ino,st.st_uid,stat.S_IMODE(st.st_mode))==(2096,8752986,1000,0o700)and list(private.iterdir())==[]
out={'passed':True,'qualified':True,'featureAcceptance':False,'summarySha256':sha(Q/'qualification-summary.json'),'rootAuditScriptSha256':sha(__file__),'processes':62,'nativePassesPerVariant':dict(native),'focusedPairedCells':len(paired),'firstBusinessCanaries':canaries,'additionalRolePositiveCases':additional_roles,'excludedCases':excluded,'closedCounterProcesses':closed_count,'rawMetadataRows':raw_rows,'sourceRuntimeInputsRehashed':len(before),'allInputsUnchanged':True,'all62ProcessGroupsAbsent':True,'privateDirectoryIdentityAndEmpty':True,'rawBusinessOrTraceValuesPersisted':False,'records':records,'limits':['Diagnostic94 native executions do not expand ordinary1048.','Whole includes capture bookkeeping; stage7 overlaps3/4/5.','Stage5 is last release interval; no summed release or route/outcome inference.','Excluded entry proves artifact absence, not absence of all invisible work.','Capacity/sink modes prove business parity with expected metadata loss.']}
p=Q/'parent-root-audit.json';assert not p.exists();p.write_text(json.dumps(out,indent=2)+'\n');p.chmod(0o600);print(json.dumps({k:v for k,v in out.items()if k not in ['records','limits']}));print('parentAuditSha256',sha(p))
