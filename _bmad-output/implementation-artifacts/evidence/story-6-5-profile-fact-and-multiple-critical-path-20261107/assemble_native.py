from pathlib import Path
import json,hashlib,shutil,re,statistics,xml.etree.ElementTree as ET,datetime
OUT=Path('/tmp/story65-profile-fact-evidence-20261107');NATIVE=Path('/tmp/story65-profile-fact-restored-native-20261107');OWNER=Path('/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore');RECEIPT=Path('/tmp/story65-owner-tests/profile-fact-command-receipt.json');DIFF=Path('/tmp/story65-profile-fact-current-owner-20261107.diff')
def sha(data):return hashlib.sha256(data).hexdigest()
provenance=[]
def copy(source,relative,expected=None):
 source=Path(source);data=source.read_bytes();actual=sha(data)
 if expected is not None:assert actual==expected,(source,expected,actual)
 target=OUT/relative;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);provenance.append({'sourcePath':str(source),'retainedPath':str(relative),'bytes':len(data),'sha256':actual});return actual
receipt=json.loads(RECEIPT.read_text());commands=json.loads((NATIVE/'native-commands.json').read_text());binary=json.loads((NATIVE/'binary-source-audit.json').read_text());matrix=json.loads((NATIVE/'matrix-audit.json').read_text());guards=json.loads((NATIVE/'parent-guards.json').read_text())
for source in sorted(NATIVE.iterdir()):
 if source.is_file():copy(source,Path('restored-native')/source.name)
receiptSha=copy(RECEIPT,Path('owner/profile-fact-command-receipt.json'))
copy(receipt['build']['log']['path'],Path('owner')/Path(receipt['build']['log']['path']).name,receipt['build']['log']['sha256'])
for invocation in receipt['nativeInvocations']:
 for key in ['xml','log']:
  item=invocation[key];copy(item['path'],Path('owner')/Path(item['path']).name,item['sha256'])
diffSha=copy(DIFF,Path('source/current-owner.diff'))
assert receipt['sourceSha256']==binary['ownerSourceHashes'];sourceRows=[]
for relative,expected in receipt['sourceSha256'].items():
 value=copy(OWNER/relative,Path('source/owner')/relative,expected);sourceRows.append({'source':relative,'sha256':value,'retainedPath':str(Path('source/owner')/relative)})
assert len(sourceRows)==33
for revision in [receipt['ownerHead'],*commands['revisions'].values(),matrix['canonicalRevision'],guards['baseline']['head']]:assert re.fullmatch('[0-9a-f]{40}',revision)
assert guards['frozenSha256']==guards['baseline']['frozen_sha256'] and guards['sprintSha256']==guards['baseline']['sprint_sha256'] and guards['indexSha256']==guards['baseline']['index_sha256']
assert len(binary['files'])==binary['checkedAssemblyCopies']==147 and all(r['privateDiagnosticMarkersAbsent'] for r in binary['files']);assert binary['matchingSdkCopies']==8
# Check recorded restored command artifacts without reading any live runtime output.
for command in commands['commands']:
 assert command['exitCode']==0
 assert sha((NATIVE/command['log']).read_bytes())==command['logSha256']
 if 'xml' in command:assert sha((NATIVE/command['xml']).read_bytes())==command['xmlSha256']
def xml_facts(path):
 root=ET.parse(path).getroot();tests=root.findall('.//test');assemblies=root.findall('.//assembly');bad=[r for r in tests if r.get('result')!='Pass'];assert not bad
 assert not root.findall('.//error')
 keys={(r.get('type'),r.get('method'),r.get('name')) for r in tests};assert len(keys)==len(tests)
 return {'path':str(path.relative_to(OUT)),'sha256':sha(path.read_bytes()),'total':len(tests),'passed':len(tests),'failed':0,'skipped':0,'errors':0,'assemblyTotals':[dict(a.attrib) for a in assemblies]},keys
facts=[];allKeys=set()
for path in sorted((OUT/'restored-native').glob('*.xml')):
 fact,keys=xml_facts(path);facts.append(fact);assert not allKeys.intersection(keys);allKeys|=keys
assert len(allKeys)==959
focusedFacts,focused=xml_facts(OUT/'owner/profile-fact-focused.xml');regressionFacts,regression=xml_facts(OUT/'owner/profile-fact-regression.xml');restoredOwnerFacts,restoredOwner=xml_facts(OUT/'restored-native/owner-tests.xml')
assert len(focused)==49 and len(regression)==333 and focused<=regression and regression==restoredOwner
ui=OUT/'restored-native/ui-tests.xml';assert sha(ui.read_bytes())==matrix['xmlSha256'];uiKeys={(r.get('name')) for r in ET.parse(ui).getroot().findall('.//test')};assert matrix['allMatrixRowsCoveredAndPassed'] and len(matrix['rows'])==5 and matrix['executedCoveringCases']==12
covering={(case['type'],case['method'],case['name']) for row in matrix['rows'] for case in row['coveringTests']};uiNative={(r.get('type'),r.get('method'),r.get('name')) for r in ET.parse(ui).getroot().findall('.//test')};assert len(covering)==12 and covering<=uiNative and all(case['result']=='Pass' for row in matrix['rows'] for case in row['coveringTests'])
# Retain only metadata receipts from earlier external probes, not their runtime/configuration material.
probeRefs=[]
for item in receipt['externalDiagnosticProbes']:
 source=Path(item['path']);value=copy(source,Path('external-probe-receipts')/source.parent.name/source.name,item['sha256']);probeRefs.append({'source':str(source),'sha256':value,'scope':'Earlier external metadata-only diagnostic; not managed live acceptance.'})
earlier=receipt['earlierCommandReceipt'];copy(earlier['path'],Path('historical-reference')/Path(earlier['path']).name,earlier['sha256'])
bench=[]
for b in receipt['benchmarks']:
 raw=b['rawObservation'];measurements=raw.get('measurements',[]);numeric={}
 for row in measurements:
  for key,value in row.items():
   if key=='sample' or not isinstance(value,(int,float)) or isinstance(value,bool):continue
   numeric.setdefault(key,[]).append(value)
 bench.append({'nativeLane':b['nativeLane'],'case':b['case'],'inputFacts':{k:v for k,v in raw.items() if k!='measurements'},'sampleCount':len(measurements),'sampleStats':{k:{'min':min(v),'median':statistics.median(v),'max':max(v)} for k,v in numeric.items()},'recordedSampleMedians':b['sampleMedians'],'firstColdAvailability':b['firstColdAvailability']})
(OUT/'benchmarks-and-bounds.json').write_text(json.dumps({'sourceReceiptSha256':receiptSha,'benchmarks':bench,'profileFactBounds':receipt['profileFactBounds'],'imageMemoBounds':receipt['imageMemoBounds'],'exclusions':['These local measurements are not complete owner query/authority/network/actor or browser timings.','First scoped samples include capture/retention when supplied; they are not cold-process or cold-host measurements. Some cases have no separately recorded first-cold sample.','Focused and regression samples are distinct invocations; sample medians and ranges are retained separately, not pooled into a claimed latency bound.','The profile-fact benchmark uses a219012-byte synthetic structured catalog with1000 schema properties; actual production workload equivalence is not asserted.','Profile scalar storage is bounded separately at3 document slots×128 characters; retained-bytes counters count canonical bytes only, not total process memory.','No live performance, p95, concurrency throughput or full-host memory ceiling is established by these cases.']},indent=2)+'\n')
(OUT/'source-and-revisions.json').write_text(json.dumps({'revisions':commands['revisions'],'ownerReceiptHead':receipt['ownerHead'],'currentOwnerDiff':{'path':'source/current-owner.diff','sha256':diffSha,'bytes':DIFF.stat().st_size},'ownerSources':sourceRows,'all33CurrentHashesMatchOwnerReceiptAndRestoredAudit':True,'sourceMappingEnvironment':commands['sourceMappingEnvironment'],'sourceMappingReuse':commands['sourceMappingReuse'],'ownerMappingEnvironmentLimitation':receipt['mappingEnvironment']},indent=2)+'\n')
(OUT/'native-verification.json').write_text(json.dumps({'checkedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'restoredNative':facts,'uniquePassingCases':959,'ownerPassingCases':333,'projectsPassingCases':626,'ownerFocusedCases':49,'focusedSubsetOfRegression':True,'regressionMatchesRestoredOwnerCases':True,'focusedCasesNotAddedAgain':True,'recordedOrdinaryBinaryCopies':147,'allRecordedPrivateMarkersAbsent':True,'matchingRecordedSdkCopies':8,'ownerSourceHashes':33,'matrixRows':5,'matrixCoveringCases':12,'matrixXmlHashMatches':True,'parentFrozenSprintIndexGuardsMatchRecordedBaseline':True,'exclusions':['The Projects.Server command explicitly excludes SupportedProjectReadTests.GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites; this packet does not claim it ran.','These are selected native classes/methods listed in exact commands, not an assertion that every repository test ran.','147-copy/8-SDK restoration assertions are recorded parent audit evidence; this packaging task does not inspect active hosts or rehash live binaries.']},indent=2)+'\n')
(OUT/'acceptance-status.json').write_text(json.dumps({'packetPhase':'native-ready-live-pending','nativeChecksPassed':True,'featureAcceptance':False,'liveAcceptance':'pending','liveEvidencePath':'/tmp/story65-profile-fact-live-20261107','liveResultIncluded':False,'browserAcceptanceIncluded':False,'cleanupForActiveLiveRunIncluded':False,'noPrivateOverlaysInRecordedOrdinaryRestoration':True,'packagingActions':'Temporary file reads/copies/hash/XML validation only; no repository edits/build/host action and no active live runtime output read.','historicalFailures':'All historical evidence remains immutable. Earlier receipt retained separately and never relabeled as current acceptance.'},indent=2)+'\n')
(OUT/'artifact-provenance.json').write_text(json.dumps({'artifacts':provenance,'externalProbeReceipts':probeRefs,'currentPhase':'native packet only; live result pending'},indent=2)+'\n')
print(json.dumps({'packet':str(OUT),'retainedInputFiles':len(provenance),'uniqueNativePasses':len(allKeys),'sourceHashes':len(sourceRows),'acceptance':'pending'},indent=2))
