from pathlib import Path
import json,hashlib,re,xml.etree.ElementTree as ET,statistics,datetime
OUT=Path('/tmp/story65-pool-selected-evidence-20261107');NATIVE=Path('/tmp/story65-pool-selected-native-20261107');BENCH=Path('/tmp/story65-pool-capacity-probe-20261107');OWNER=Path('/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore');RECEIPT=Path('/tmp/story65-owner-tests/image-pool-final-command-receipt.json');DIFF=Path('/tmp/story65-pool-selected-current-owner-20261107.diff')
def sha(data):return hashlib.sha256(data).hexdigest()
provenance=[]
def copy(source,relative,expected=None):
 source=Path(source);data=source.read_bytes();actual=sha(data)
 if expected is not None:assert actual==expected,(source,expected,actual)
 p=OUT/relative;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data);provenance.append({'sourcePath':str(source),'retainedPath':str(relative),'bytes':len(data),'sha256':actual});return actual
receipt=json.loads(RECEIPT.read_text());summary=json.loads((BENCH/'final-benchmark-summary.json').read_text());native=json.loads((NATIVE/'native-commands.json').read_text());binary=json.loads((NATIVE/'binary-source-audit.json').read_text());matrix=json.loads((NATIVE/'matrix-audit.json').read_text());guards=json.loads((NATIVE/'parent-guards.json').read_text())
copy(RECEIPT,Path('owner/image-pool-final-command-receipt.json'),'247be13b8296513bd93bb1e717b3cf3df80bcca290f160ec9b9261fb93d7f353')
copy(DIFF,Path('source/current-owner.diff'),'e5f4c5a1f8f2c67e213542754d4653148db4c4ca39f82949c78f71ad241d7e23')
for f in sorted(NATIVE.iterdir()):
 if f.is_file():copy(f,Path('restored-native')/f.name)
for f in sorted(BENCH.iterdir()):
 if f.is_file() and f.suffix in ['.json','.log','.cs','.csproj','.txt','.py']:copy(f,Path('benchmark')/f.name)
assert sha((OUT/'benchmark/final-benchmark-summary.json').read_bytes())==receipt['benchmarks']['summarySha256']=='1df977874272d2c7709e9db06b32fd3b5826948d4c2603119e1f7aa5602e0649'
copy(receipt['build']['log'],Path('owner')/Path(receipt['build']['log']).name,receipt['build']['logSha256'])
for invocation in receipt['nativeInvocations']:
 assert invocation['exitCode']==0
 for key in ['xml','log']:copy(invocation[key],Path('owner')/Path(invocation[key]).name,invocation[key+'Sha256'])
for key in ['preFixtureMoveVerification','earlierCommandReceipt']:
 source=Path(receipt[key]);copy(source,Path('historical-reference')/source.name)
priorReceipt=json.loads(Path(receipt['earlierCommandReceipt']).read_text());priorRegression=next(x['xml']['path'] if isinstance(x['xml'],dict) else x['xml'] for x in priorReceipt['nativeInvocations'] if (x['xml']['path'] if isinstance(x['xml'],dict) else x['xml']).endswith('profile-fact-regression.xml'));copy(priorRegression,Path('historical-reference/profile-fact-regression.xml'))
assert receipt['sourceSha256']==binary['ownerSourceHashes'] and len(receipt['sourceSha256'])==37
sources=[]
for name,expected in receipt['sourceSha256'].items():
 value=copy(OWNER/name,Path('source/owner')/name,expected);sources.append({'source':name,'retainedPath':str(Path('source/owner')/name),'sha256':value})
for value in [receipt['ownerHead'],*native['revisions'].values(),matrix['canonicalRevision'],guards['baseline']['head']]:assert re.fullmatch('[0-9a-f]{40}',value)
assert binary['checkedAssemblyCopies']==len(binary['files'])==147 and binary['matchingSdkCopies']==8 and all(r['privateDiagnosticMarkersAbsent'] for r in binary['files'])
for command in native['commands']:
 assert command['exitCode']==0
 if 'log' in command:assert sha((NATIVE/command['log']).read_bytes())==command['logSha256']
 if 'xml' in command:assert sha((NATIVE/command['xml']).read_bytes())==command['xmlSha256']
def parse(path):
 root=ET.parse(path).getroot();tests=root.findall('.//test');assert tests and all(t.get('result')=='Pass' for t in tests) and not root.findall('.//error');keys={(t.get('type'),t.get('method'),t.get('name')) for t in tests};assert len(keys)==len(tests)
 return {'path':str(path.relative_to(OUT)),'sha256':sha(path.read_bytes()),'passed':len(tests),'failed':0,'errors':0,'skipped':0,'notRun':0,'assemblyTotals':[dict(a.attrib) for a in root.findall('.//assembly')]},keys
facts=[];projectKeys=set()
for path in sorted((OUT/'restored-native').glob('*.xml')):
 fact,keys=parse(path);facts.append(fact);assert not(projectKeys&keys);projectKeys|=keys
assert len(projectKeys)==626
focusedFacts,focused=parse(OUT/'owner/image-pool-final-focused.xml');ownerFacts,owner=parse(OUT/'owner/image-pool-final-regression.xml');priorFacts,prior=parse(OUT/'historical-reference/profile-fact-regression.xml')
assert len(focused)==71 and len(owner)==340 and len(prior)==333 and focused<=owner and prior<=owner and len(owner-prior)==7
assert not owner&projectKeys
ui=OUT/'restored-native/ui-tests.xml';assert sha(ui.read_bytes())==matrix['xmlSha256'];covering={(c['type'],c['method'],c['name']) for row in matrix['rows'] for c in row['coveringTests']};uiKeys={(t.get('type'),t.get('method'),t.get('name')) for t in ET.parse(ui).getroot().findall('.//test')};assert len(matrix['rows'])==5 and len(covering)==12 and covering<=uiKeys and all(c['result']=='Pass' for row in matrix['rows'] for c in row['coveringTests'])
assert guards['frozenSha256']==guards['baseline']['frozen_sha256'] and guards['sprintSha256']==guards['baseline']['sprint_sha256'] and guards['indexSha256']==guards['baseline']['index_sha256']
assert len(summary['rows'])==12
for row in summary['rows']:
 p=OUT/'benchmark'/Path(row['file']).name;assert sha(p.read_bytes())==row['sha256']
assert summary['selectedSmallestUsefulCapacity']==receipt['benchmarks']['chosenSlots']==2
(OUT/'native-verification.json').write_text(json.dumps({'checkedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'rootProjects':facts,'ownerFocused':focusedFacts,'ownerRegression':ownerFacts,'uniqueNativePassingCases':966,'ownerPassingCases':340,'projectsPassingCases':626,'focused71SubsetOf340':True,'focusedNotAddedAgain':True,'prior333NamesRetained':True,'addedCaseNames':sorted(k[2] for k in owner-prior),'recordedAssemblyCopies':147,'recordedSdkMatches':8,'allRecordedDiagnosticMarkersAbsent':True,'sourceHashesMatched':37,'matrixRows':5,'matrixCoveringCases':12,'matrixCoveredPassNamesVerified':True,'frozenSprintIndexRecordedGuardsMatchBaseline':True,'featureAcceptance':False,'limitations':['147-copy audit is recorded ordinary pre-live proof; this packaging task does not inspect hosts or rehash active binaries.','Selected native commands/classes are exact; no assertion that every repository test ran.','The Projects.Server command explicit GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites exclusion is preserved; it is not claimed executed.']},indent=2)+'\n')
(OUT/'source-and-revisions.json').write_text(json.dumps({'revisions':native['revisions'],'ownerHead':receipt['ownerHead'],'sourceDiff':{'path':'source/current-owner.diff','sha256':sha(DIFF.read_bytes()),'bytes':DIFF.stat().st_size},'ownerSources':sources,'all37SourcesMatchReceiptAndRecordedOrdinaryAudit':True,'sourceMappingEnvironment':receipt['sourceMappingEnvironment'],'mappingSource':receipt['mappingSource'],'mappingSha256':receipt['mappingSha256'],'sourceFreezeRecorded':receipt['sourceFreeze']},indent=2)+'\n')
(OUT/'benchmark-and-bounds.json').write_text(json.dumps({'source':'benchmark/final-benchmark-summary.json','sourceSha256':sha((BENCH/'final-benchmark-summary.json').read_bytes()),'selectedSharedSlots':2,'observationsPerInventory':summary['observationsPerInventory'],'imagesPerObservation':summary['imageCount'],'freshBytesPerCompleteInventory':summary['freshBytesPerInventory'],'measuredRows':[{k:v for k,v in r.items() if k not in ['firstCold','aligned','trackedWarmRents']} for r in summary['rows']],'selectedBounds':receipt['bounds'],'summaryLimits':summary['limits'],'exclusions':['This external source-derived12-image FullName-sorted inventory is count-corroborated; actual managed trace did not retain image names/order.','Per-inventory measurements include two fresh observations, original file reads, logical slices, PE/MVID, owned scope entry/disposal, actual memo and exact-byte equality. They are not whole catalog/owner/actor/network/browser latency.','Primary timing uses unwrapped ArrayPool; rent-tracking/aligned phases are separate and their overhead is not substituted into primary results.','All cold/warm/confirmation outputs are retained; no latency guarantee or full-host memory bound is claimed.','Measured benchmark allocation attribution is not retroactive attribution of the earlier instrumented managed6.16GiB process delta.','Selected pool raw payload is below64MiB plus bounded metadata/object overhead; both ordinary pools stay below32MiB each, allthree below128MiB raw payload. The independent image memo64MiB charged bound is distinct.','Initial external inventory-census failure is retained without conversion to performance success.','Live/browser acceptance remains pending and is not established by native or benchmark passes.']},indent=2)+'\n')
(OUT/'acceptance-status.json').write_text(json.dumps({'packetPhase':'native-and-benchmark-ready-live-pending','uniqueNativePassingCases':966,'ownerPassingCases':340,'projectsPassingCases':626,'selectedSharedSlots':2,'featureAcceptance':False,'liveAcceptance':'pending','liveEvidencePath':'/tmp/story65-pool-selected-live-20261107','liveResultIncluded':False,'browserAcceptanceIncluded':False,'activeLiveCleanupIncluded':False,'noDiagnosticOverlayInRecordedOrdinaryAudit':True,'packagingActions':'Temporary reads/copies/hash/XML/source consistency only; no build, host action, repository edit or active runtime output read.','historicalFailures':'Earlier239-entry packet was copied/verified by parent into separate root evidence. This packet does not overwrite it or relabel failures.'},indent=2)+'\n')
(OUT/'artifact-provenance.json').write_text(json.dumps({'artifacts':provenance,'currentPhase':'native/bench/source only; managed live result pending'},indent=2)+'\n')
print(json.dumps({'inputArtifacts':len(provenance),'nativePasses':966,'ownerSources':37,'benchmarkRuns':12,'liveAcceptance':'pending'},indent=2))
