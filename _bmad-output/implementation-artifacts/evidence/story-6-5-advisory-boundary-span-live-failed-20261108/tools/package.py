from pathlib import Path
import gzip,hashlib,json,shutil,xml.etree.ElementTree as ET
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-advisory-boundary-span-live-prep-20261108')
LIVE=Path('/tmp/story65-advisory-boundary-span-diagnostic-20261108')
OUT=Path('/tmp/story65-advisory-boundary-span-live-evidence-final-20261108')
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
provenance=[]
def write(name,value):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value,indent=2)+'\n')
def copy(source,name):
 source=Path(source);assert source.is_file()and not source.is_symlink()
 target=OUT/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target);assert sha(source)==sha(target)
 provenance.append({'sourcePath':str(source.resolve()),'sourceSha256':sha(source),'sourceBytes':source.stat().st_size,'destination':name,'destinationSha256':sha(target),'destinationBytes':target.stat().st_size,'transformation':'byte-exact-copy'})

assert not OUT.exists();OUT.mkdir(mode=0o700)
final=Path('/tmp/story65-advisory-boundary-span-final-audit-prep-20261108')
assert sha(final/'independent-final-audit.json')=='ce4a09e4fb9e45ecde09a49ae2f0d95187a541a93f38fa3a8e8afc11ad1fdbea'
assert sha(final/'audit.py')=='8106018717c6094a9b605e4382c346a8edeb653043d503d9012c67070da989e9'
audit=json.loads((final/'independent-final-audit.json').read_text());assert audit['passed']is True
copy(__file__,'tools/package.py')
# Frozen operative source/plans; omit the large historical dependency-binary census.
for p in sorted(PREP.iterdir()):
 if p.is_file()and(p.suffix in ['.py','.targets','.diff']or p.name in ['operational-pins.json','commands.json','source-qualification-binding.json','native-reference-case-set.json','mapping-pins.json','expected-managed-profile.json','private-directory-proof.json','operational-plan.json','adaptation-plan.json','writer-attribution-plan.json','boundary-stage-map.json']):copy(p,'operation/'+p.name)
for p in (PREP/'root-release').iterdir():
 if p.is_file()and p.suffix=='.json':copy(p,'operation/root-release/'+p.name)
for n in ['latest-private-attempt.json','latest-native-attempt.json','latest-operational-result.json']:copy(PREP/n,'operation/'+n)
private=Path(json.loads((PREP/'latest-private-attempt.json').read_text())['attemptDirectory'])
native=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory'])
ordinary=PREP/'operational-ordinary-teyq1vsm'
for phase,directory in [('private',private),('ordinary',ordinary)]:
 for n in ['commands.json','receipt.json','attempt-closed.json','before-census.json','after-census.json','mapping-receipt.json','evaluated-eight-property-profile.json','actual-advisory-compiler-inputs.json','actual-projects-producer-compiled-proof.json','story65-advisory-compiler-inputs.txt','story65-advisory-writable-paths.txt','story65-advisory-precore-flags.txt','story65-advisory-postcore-flags.txt','affected-paths-union.json','restoration-baseline-source.json']:
  if(directory/n).exists():copy(directory/n,phase+'/'+n)
for n in ['receipt.json','native-identity.json','native-139.xml']:copy(native/n,'native139/'+n)
for n in ['supervisor.json','early-runtime-identities.json','independent-cleanup-source-audit.json','cleanup-observation-history.json','aspire-nobuild-substitution.json','supplemental-cleanup-qualification.json','runtime-private-projects-proof.json','managed-command.json','execution-entry-counts.json','closed-recorder-validation.json','pre-observation-closed-source-facts.json']:
 copy(LIVE/n,'managed-metadata/'+n)
for p in sorted(LIVE.glob('cleanup-observation-[0-9]*.json')):copy(p,'managed-metadata/'+p.name)
copy(LIVE/'managed/source-manifest.json','managed-metadata/source-manifest.json')
run=json.loads((LIVE/'managed/run.json').read_text())
allowed=['qualification','acceptedEquivalence','productionReadiness','result','matrixCompleted','qualificationGroup','sourceDiffSha256','sourceManifestSha256','sourceMappingHash','sourceMappingEnvironment','compilationProfile','runtimeModes','testProcessCleanup','ownedSidecarsAbsent','preexistingResourcesPreserved','privateMaterialRemoved']
write('managed-metadata/run-closed-projection.json',{'originalSha256':sha(LIVE/'managed/run.json'),'originalBytes':(LIVE/'managed/run.json').stat().st_size,'projection':{k:run[k]for k in allowed if k in run},'rawFailureCommandsPrivateConfigurationAndRuntimeLogsExcluded':True})
keys=['multiple','attempt','elapsedMilliseconds','httpStatus','result','incomplete','candidateCount','positiveCandidatesVerified','actualPublicGateway','fabricatedProjectionWrite','nonBootstrapCredential','runtimeAuthoritySupplied']
p=LIVE/'managed/browser-advisory-seed-readiness.jsonl';rows=[json.loads(line)for line in p.read_bytes().split(b'\n')[:-1]]
write('managed-metadata/readiness-closed-projection.json',{'originalSha256':sha(p),'originalBytes':p.stat().st_size,'elapsedIncludesSubsequentEvidenceReads':True,'noJoinToCaptureOrdinals':True,'rows':[{k:d.get(k)for k in keys}for d in rows]})
p=LIVE/'managed/browser-seed.xml';tests=list(ET.parse(p).getroot().iter('test'))
write('managed-metadata/browser-seed-result-projection.json',{'originalSha256':sha(p),'cases':[{'type':t.get('type'),'method':t.get('method'),'result':t.get('result'),'exceptionType':t.find('failure').get('exception-type')if t.find('failure')is not None else None}for t in tests],'messagesStacksAndStdoutExcluded':True,'browserAcceptanceAssertions':0})
# Preserve fresh managed seven values as a DISTINCT generation from prepared build/native.
prepared=json.loads((private/'mapping-receipt.json').read_text())['emittedEnvironment']
managed=run['sourceMappingEnvironment'];assert len(prepared)==len(managed)==7
write('managed-metadata/mapping-generation-boundaries.json',{'preparedBuildNativeEnvironment':prepared,'freshManagedEnvironment':managed,'freshManagedMappingHash':run['sourceMappingHash'],'preparedMappingHash':prepared['HEXALITH_SOURCE_MAPPING_HASH'],'sameSevenNames':set(prepared)==set(managed),'valuesEqualByName':{k:prepared[k]==managed[k]for k in prepared},'allSevenCrossRunIdentityClaimed':False,'wrapperDidNotOverrideOriginalManagedMappings':True})
for name,folder in [('private-native','/tmp/story65-advisory-boundary-span-native-audit-20261108'),('final','/tmp/story65-advisory-boundary-span-final-audit-prep-20261108')]:
 d=Path(folder)
 for p in d.iterdir():
  if p.is_file()and p.suffix in ['.py','.json']:copy(p,'independent-audits/'+name+'/'+p.name)
history=Path('/tmp/story65-advisory-boundary-span-final-audit-history-20261108/e4426136')
for n in ['audit.py','PLAN.json','adaptation.diff']:copy(history/n,'audit-history/unexecuted-e4426136/'+n)
copy(final/'root-review-corrections.diff','audit-history/root-review-corrections.diff')
write('audit-history/classification.json',{'preservedTemplateSha256':sha(history/'audit.py'),'templateExecuted':False,'sourceCorrectionScope':'three literal protocol-count and printed-count metadata corrections only','businessNativeAndApplicationCodeChanged':False,'acceptedFinalSourceSha256':sha(final/'audit.py'),'acceptedFinalReportSha256':sha(final/'independent-final-audit.json'),'finalReadsImmutableArchiveAndSeparatesMappingGenerations':True})
for n in ['archive-before-removal.json','archive-cleanup.json','projects.closed.json']:copy(LIVE/'counter'/n,'counter/'+n)
raw=(LIVE/'counter/projects.jsonl').read_bytes();compressed=gzip.compress(raw,mtime=0);assert gzip.decompress(compressed)==raw
(OUT/'counter/projects.jsonl.gz').write_bytes(compressed)
provenance.append({'sourcePath':str((LIVE/'counter/projects.jsonl').resolve()),'sourceSha256':hashlib.sha256(raw).hexdigest(),'sourceBytes':len(raw),'destination':'counter/projects.jsonl.gz','destinationSha256':hashlib.sha256(compressed).hexdigest(),'destinationBytes':len(compressed),'transformation':'gzip-mtime0','decompressedSha256':hashlib.sha256(gzip.decompress(compressed)).hexdigest(),'decompressedBytes':len(gzip.decompress(compressed)),'rows':len(raw.splitlines())})
write('counter/compression.json',{'originalSha256':hashlib.sha256(raw).hexdigest(),'originalBytes':len(raw),'rows':len(raw.splitlines()),'compressedSha256':hashlib.sha256(compressed).hexdigest(),'decompressedBytesExactlyMatch':True,'controlContentsCopied':False})
copy(Path('/tmp/story65-advisory-boundary-span-archive-prep-20261108/archive_closed.py'),'archive-source/archive_closed.py')
published=ROOT/'_bmad-output/implementation-artifacts/evidence/story-6-5-advisory-boundary-span-qualified-20261108'
write('qualified-scratch-reference.json',{'durablePath':str(published.relative_to(ROOT)),'artifacts':163,'manifestSha256':sha(published/'manifest.json'),'sha256sumsSha256':sha(published/'SHA256SUMS'),'businessComparisons':196,'freshFlowCanaries':3,'stateProtocolCases':14,'controlledProcesses':23,'frozenInputs':766,'numericRows':167522,'notRepeatedHere':True,'notAdditionalNativeCases':True})
oldManifestPath=Path('/tmp/story65-advisory-terminal-diagnostic-20261108/managed/source-manifest.json');oldManifest=json.loads(oldManifestPath.read_text());currentManifest=json.loads((LIVE/'managed/source-manifest.json').read_text())
added=sorted(currentManifest.keys()-oldManifest.keys());assert len(oldManifest)==17361 and len(currentManifest)==17363 and len(added)==2 and not oldManifest.keys()-currentManifest.keys()and all(oldManifest[k]==currentManifest[k]for k in oldManifest)
assert all(name.startswith('_bmad-output/implementation-artifacts/evidence/story-6-5-advisory-terminal-live-failed-20261108/operation/')and name.endswith('.targets')for name in added)
write('managed-metadata/full-manifest-scope.json',{'source65Unchanged':True,'priorLiveManifestCount':len(oldManifest),'currentLiveManifestCount':len(currentManifest),'priorManifestPath':str(oldManifestPath),'priorManifestSha256':sha(oldManifestPath),'currentManifestSha256':sha(LIVE/'managed/source-manifest.json'),'onlyAddedPaths':added,'allSharedManifestHashesEqual':True,'additionalPathsArePublishedHistoricalEvidenceTargetsNotProductionChanges':True})
records=audit['records'];write('counter/eight-stage-summary.json',{'sourceIndependentAuditSha256':sha(final/'independent-final-audit.json'),'captures':records['captures'],'rows':records['rows'],'wholeMilliseconds':records['wholeMilliseconds'],'terminalEncodingMilliseconds':records['terminalEncodingMilliseconds'],'terminalVectorHistogram':records['terminalVectorHistogram'],'retainedMarkerSpanSummary':records['retainedMarkerSpanSummary'],'perCapturePairedSpans':records['perCapturePairedSpans'],'selectedSpansAreMarkerIntervalsNotAwaitRemoteCallbackDurations':True,'stage7IsIndex':True,'missingSpanIsUnobservedNotZeroOrCallbackAbsence':True,'noReadinessRequestServicePriorProbeOrProfileJoins':True})
copy(Path(__file__).with_name('README.md'),'README.md')
write('provenance.json',{'byteExactCopiesAndGzipTransformation':provenance,'projectionsCarryTheirOwnOriginalSha256AndBytes':True,'archiveContainsOnlyClosedNumericMetadata':True})
artifacts={str(p.relative_to(OUT)):sha(p)for p in sorted(OUT.rglob('*'))if p.is_file()}
write('manifest.json',{'diagnosticOnly':True,'featureAcceptance':False,'artifacts':len(artifacts),'filesSha256':artifacts,'finalIndependentAuditSha256':sha(final/'independent-final-audit.json'),'nativeIndependentAuditSha256':sha(Path('/tmp/story65-advisory-boundary-span-native-audit-20261108/independent-private-native-audit.json')),'scratchReferenceOnly':True})
files=sorted(p for p in OUT.rglob('*')if p.is_file());(OUT/'SHA256SUMS').write_text(''.join(f'{sha(p)}  {p.relative_to(OUT)}\n'for p in files))
assert all(sha(OUT/name)==h for name,h in artifacts.items())
print(json.dumps({'directory':str(OUT),'artifactsExcludingSums':len(files),'manifestSha256':sha(OUT/'manifest.json'),'sha256sumsSha256':sha(OUT/'SHA256SUMS'),'counterRows':len(raw.splitlines()),'sealed':True}))
