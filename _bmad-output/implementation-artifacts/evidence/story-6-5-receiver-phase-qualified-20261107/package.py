from pathlib import Path
import collections,datetime,gzip,hashlib,json,os,stat,xml.etree.ElementTree as ET
OUT=Path(__file__).parent
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-receiver-phase-prep-20261107')
Q2=Path('/tmp/story65-receiver-phase-qualification-retry-20261107')
Q1=Path('/tmp/story65-receiver-phase-qualification-20261107')
HISTORY=Path('/tmp/story65-receiver-phase-history-20261107/298912-attempt1')
PRODUCER=Path('/tmp/story65-receiver-phase-producer-prep-20261107')
AGGREGATE_SHA='8fddf022ddad74d69ee414dd1723ab9ccd9876d4885dd43a06aa482cf22ba2a6'
def h(data):return hashlib.sha256(data).hexdigest()
def read(p):
 p=Path(p);assert p.is_file() and not p.is_symlink();return p.read_bytes()
def sha(p):return h(read(p))
def load(p):return json.loads(read(p))
def save(name,data):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);assert not p.exists();p.write_text(json.dumps(data,indent=2)+'\n')
q=load(Q2/'qualified-receipt.json');assert sha(Q2/'qualified-receipt.json')==AGGREGATE_SHA
assert q['qualified']is True and q['featureAcceptance']is False and len(q['sourceSha256'])==56
assert (q['passingFocusedSemanticPairs'],q['passingFirstBusinessCanaries'],q['controlledProcesses'],q['diagnosticNativeExecutions'])==(64,3,62,94)
assert q['passingNativeCasesPerVariant']=={'ordinary':47,'private':47} and q['ordinaryNative1048UnchangedNotInflated']is True
assert q['strictValidatedRows']==167438 and q['currentInputPathsRehashed']==1286
for p,digest in q['evidenceSha256'].items():assert sha(p)==digest
for rel,digest in q['sourceSha256'].items():assert sha(ROOT/rel)==digest
before=load(Q2/'inputs-before.json');after=load(Q2/'inputs-after.json');assert before==after and len(before)==1286
for p,digest in before.items():assert sha(p)==digest
records=q['receipts'];assert len(records)==62
rows=closed=0;stages=collections.Counter();native=collections.Counter();process_keys=set()
zero={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
fields=zero|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
for item in records:
 r=load(item['path']);assert sha(item['path'])==item['sha256']
 assert r['passed']is True and r['exitCode']==0 and r['processGroupAbsent']is True and r['remainingGroup']==[] and r['signalsSent']is False and r['directoryReleased']is True
 assert r['pid'] not in process_keys;process_keys.add(r['pid'])
 work=Path(item['path']).parent
 if r['mode']=='native':
  ts=ET.fromstring(read(work/'native.xml')).findall('.//test');assert all(t.attrib['result']=='Pass'for t in ts);native[r['variant']]+=len(ts)
 if r['closed'] is not None:
  closed+=1;c=r['closed'];assert c['countersFinal']==1 and c['activeAtDrainEnd']==c['queuedAtDrainEnd']==c['incompleteHardDrain']==0
  rs=0
  for line in read(work/(r['role']+'.jsonl')).splitlines():
   x=json.loads(line);assert set(x)==fields and all(type(x[k])is int and x[k]>=0 for k in fields-{'role','stage'})and all(x[k]==0 for k in zero)
   rs+=1;stages[x['stage']]+=1
  assert rs==c['writtenRows'];rows+=rs
assert native=={'ordinary':47,'private':47} and rows==167438 and closed==22 and dict(stages)==q['stageRowCounts']
provenance=[]
def copy(source,destination,compress=False):
 source=Path(source);destination=Path(destination);assert not destination.is_absolute() and '..'not in destination.parts
 original=read(source);stored=gzip.compress(original,compresslevel=9,mtime=0) if compress else original
 target=OUT/destination;target.parent.mkdir(parents=True,exist_ok=True)
 if target.exists():assert read(target)==stored;return
 target.write_bytes(stored)
 provenance.append({'originalPath':str(source),'originalSha256':h(original),'originalBytes':len(original),'packetPath':str(destination),'packetSha256':h(stored),'packetBytes':len(stored),'encoding':'gzip-mtime0'if compress else'exact-copy'})
prep=load(PREP/'preparation-receipt.json')
for relative,digest in prep['filesSha256'].items():
 source=PREP/relative;assert sha(source)==digest;copy(source,Path('source-preparation')/relative)
copy(PREP/'preparation-receipt.json','source-preparation/preparation-receipt.json')
# One exact current control-source set. Original paths/hashes remain explicit.
for relative,digest in prep['sourceSha256'].items():copy(ROOT/relative,Path('production-source-controls')/relative)
# Every closed scratch result, command, input map and XML; no runtime binary copies.
allowed={'.json','.xml','.log','.py','.md'}
for origin,bucket in [(Q2,'qualification-q2'),(Q1,'failed-qualification-q1')]:
 for p in sorted(origin.rglob('*')):
  if not p.is_file() or 'runtime'in p.relative_to(origin).parts:continue
  relative=p.relative_to(origin)
  if p.suffix=='.jsonl':copy(p,Path(bucket)/(str(relative)+'.gz'),True)
  elif p.suffix in allowed:copy(p,Path(bucket)/relative)
# Preserve only changed old source files plus the exact old full manifest; unchanged source is above.
for name in ['preparation-receipt.json','runtime-manifest.json','driver/owned-source-index.json','entry/projects/Entry.csproj','entry/tenants/Entry.csproj','entry/folders/Entry.csproj','entry/excluded/Entry.csproj']:
 copy(HISTORY/name,Path('failed-generation-298912')/name)
for origin,bucket in [(Path('/tmp/story65-receiver-phase-fixture-build-20261107'),'initial-six-project-build'),(Path('/tmp/story65-receiver-phase-entry-build-retry-20261107'),'entry-only-build-retry'),(Path('/tmp/story65-receiver-phase-qualified-prep-20261107'),'aggregate-audit')]:
 for p in sorted(origin.rglob('*')):
  if p.is_file()and p.suffix in allowed and '__pycache__'not in p.parts:copy(p,Path(bucket)/p.relative_to(origin))
copy(Path('/tmp/story65-receiver-phase-runtime-manifest-retry-20261107.json'),'runtime-manifest-retry.json')
# Isolated producer source/config/commands/proofs and actual compile metadata; binary paths/hash only.
for p in sorted(PRODUCER.rglob('*')):
 if not p.is_file()or any(x in p.relative_to(PRODUCER).parts for x in ['isolated-sdk','isolated-oracle','__pycache__']):continue
 if p.suffix in {'.json','.py','.props','.targets','.log','.md'}:copy(p,Path('isolated-sdk-producer')/p.relative_to(PRODUCER))
for name in ['native-reference-case-set.json','native47-source-derivation.json','entry-metadata-contract.json']:
 p=Path('/tmp/story65-receiver-phase-ops-prep-20261107')/name
 if p.exists():copy(p,Path('source-role-native-reference')/name)
for path in [Path('/tmp/story65-receiver-phase-parent-root-audit-20261107.py'),Path('/tmp/story65-receiver-phase-parent-audit-20261107.py')]:
 if path.exists():copy(path,Path('parent-audit-scripts')/path.name)
save('independent-quick-verification.json',{'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':True,'aggregateSha256':AGGREGATE_SHA,'aggregateEvidenceHashesRechecked':len(q['evidenceSha256']),'currentInputPathsRehashed':len(before),'sourceControlsRehashed':56,'actualProcessReceipts':62,'allReceiptsClosedZeroNoSignalsAndGroupsAbsent':True,'focusedSemanticPairsFromQualifiedAggregate':64,'firstBusinessCanariesFromQualifiedAggregate':3,'nativeXmlPassesIndependentlyCounted':dict(native),'diagnosticNativeExecutions':94,'ordinaryNative1048UnchangedNotInflated':True,'strictCounterRowsIndependentlyRecounted':rows,'closedCounterReceipts':closed,'stageRowCounts':dict(stages),'allCountAllocationColumnsZero':True,'featureAcceptance':False,'independentCheckIsFileOnlyNoNewProcOrPrivateDirOperations':True})
save('original-evidence-path-digests.json',q['evidenceSha256'])
save('copy-provenance.json',provenance)
# No raw control, binary, object outputs, credentials or runtime payloads are packaged.
for p in OUT.rglob('*'):
 if p.is_file():assert p.suffix not in ['.dll','.pdb','.exe','.nettrace','.etl','.etlx'] and p.name!='window.control'
print(json.dumps({'aggregateAccepted':True,'evidenceHashes':len(q['evidenceSha256']),'copiedFiles':len(provenance),'rows':rows,'closed':closed,'ordinaryNative1048NotInflated':True}))
