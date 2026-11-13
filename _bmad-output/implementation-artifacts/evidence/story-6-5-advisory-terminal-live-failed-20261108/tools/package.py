from pathlib import Path
import gzip,hashlib,json,shutil,xml.etree.ElementTree as ET
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-advisory-terminal-live-prep-20261108')
LIVE=Path('/tmp/story65-advisory-terminal-diagnostic-20261108')
OUT=Path('/tmp/story65-advisory-terminal-live-evidence-final-20261108')
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
provenance=[]
def write(name,value):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value,indent=2)+'\n')
def copy(source,name):
 source=Path(source);assert source.is_file()and not source.is_symlink()
 target=OUT/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target);assert sha(source)==sha(target)
 provenance.append({'sourcePath':str(source.resolve()),'sourceSha256':sha(source),'sourceBytes':source.stat().st_size,'destination':name,'destinationSha256':sha(target),'destinationBytes':target.stat().st_size,'transformation':'byte-exact-copy'})

assert not OUT.exists();OUT.mkdir(mode=0o700)
final=Path('/tmp/story65-advisory-terminal-final-audit-20261108')
assert sha(final/'independent-final-audit.json')=='d3419f76cbcf84cb5c2a2530ed57d997766b711cf6a630b4eae754cf0fa335f5'
assert sha(final/'audit.py')=='347761033d1a6d4632aa1baf446e1fa84cab87b03db5bdf99122d2433aee395b'
audit=json.loads((final/'independent-final-audit.json').read_text());assert audit['passed']is True
copy(__file__,'tools/package.py')
# Frozen operative source/plans; omit the large historical dependency-binary census.
for p in sorted(PREP.iterdir()):
 if p.is_file()and(p.suffix in ['.py','.targets','.diff']or p.name in ['operational-pins.json','commands.json','source-qualification-binding.json','native-reference-case-set.json','mapping-pins.json','expected-managed-profile.json','private-directory-proof.json','operational-plan.json','adaptation-plan.json','writer-attribution-plan.json']):copy(p,'operation/'+p.name)
for p in (PREP/'root-release').iterdir():
 if p.is_file()and p.suffix=='.json':copy(p,'operation/root-release/'+p.name)
for n in ['latest-private-attempt.json','latest-native-attempt.json','latest-operational-result.json']:copy(PREP/n,'operation/'+n)
private=Path(json.loads((PREP/'latest-private-attempt.json').read_text())['attemptDirectory'])
native=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory'])
ordinary=PREP/'operational-ordinary-0leyzmb9'
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
for name,folder in [('private-native','/tmp/story65-advisory-terminal-native-audit-20261108'),('final','/tmp/story65-advisory-terminal-final-audit-20261108')]:
 d=Path(folder)
 for p in d.iterdir():
  if p.is_file()and p.suffix in ['.py','.json']:copy(p,'independent-audits/'+name+'/'+p.name)
copy(final/'history-before-archive/audit-before-archive.py','audit-history/audit-before-archive.py')
write('audit-history/classification.json',{'firstAuditScriptExecutedOnceReadOnly':True,'stoppedAtLine':73,'reason':'overly-strict-prepared-vs-fresh-managed-mapping-equality-assumption','reportEmitted':False,'notNativeBusinessOrRuntimeFailure':True,'sixCounterRowsWereValidatedInRamBeforeStop':True,'rawStderrExcluded':True,'provisionalSuccessfulReportSha256':'600fd32d750e0a75d57a649b8ca982d98de5149f17af2fd161bbdec347ca0a08','provisionalReportNotFinalAndOriginalBytesNotPresentHere':True,'onlyAcceptedFinalReportSha256':sha(final/'independent-final-audit.json'),'finalReadsImmutableArchiveAndSeparatesMappingGenerations':True})
for n in ['archive-before-removal.json','archive-cleanup.json','projects.closed.json']:copy(LIVE/'counter'/n,'counter/'+n)
raw=(LIVE/'counter/projects.jsonl').read_bytes();compressed=gzip.compress(raw,mtime=0);assert gzip.decompress(compressed)==raw
(OUT/'counter/projects.jsonl.gz').write_bytes(compressed)
provenance.append({'sourcePath':str((LIVE/'counter/projects.jsonl').resolve()),'sourceSha256':hashlib.sha256(raw).hexdigest(),'sourceBytes':len(raw),'destination':'counter/projects.jsonl.gz','destinationSha256':hashlib.sha256(compressed).hexdigest(),'destinationBytes':len(compressed),'transformation':'gzip-mtime0','decompressedSha256':hashlib.sha256(gzip.decompress(compressed)).hexdigest(),'decompressedBytes':len(gzip.decompress(compressed)),'rows':len(raw.splitlines())})
write('counter/compression.json',{'originalSha256':hashlib.sha256(raw).hexdigest(),'originalBytes':len(raw),'rows':len(raw.splitlines()),'compressedSha256':hashlib.sha256(compressed).hexdigest(),'decompressedBytesExactlyMatch':True,'controlContentsCopied':False})
for n in ['archive_closed.py','archive-plan.json']:copy(Path('/tmp/story65-advisory-terminal-archive-prep-20261108')/n,'archive-source/'+n)
published=ROOT/'_bmad-output/implementation-artifacts/evidence/story-6-5-advisory-terminal-qualified-20261108'
write('qualified-scratch-reference.json',{'durablePath':str(published.relative_to(ROOT)),'artifacts':151,'manifestSha256':sha(published/'manifest.json'),'sha256sumsSha256':sha(published/'SHA256SUMS'),'businessComparisons':196,'freshFlowCanaries':3,'stateProtocolCases':4,'controlledProcesses':23,'numericRows':167315,'notRepeatedHere':True,'notAdditionalNativeCases':True})
readme='''This packet records one current private Projects.Server advisory terminal diagnostic. Acceptance remains failed: SINGLE1/1 verified positive; MULTIPLE30/0, all HTTP200/null/incomplete/zero candidates; browser seed failed before browser assertions. The wrapper/pipeline completed restoration; the underlying managed launcher exited1. This is diagnostic evidence, not ordinary feature acceptance.

Only the reviewed temporary ProjectAdvisoryQueryExecutor, helper112, bootstrap8f58, bounded adapter9a and three closed enums were compiled into Projects.Server. Original observations, awaits/order, cancellation filters/bare throws, outputs and two-second CTS remain. SDK873d/Contracts f1cf stayed ordinary. Source65/current Debug/two proof flags/natural runtime CPU and privacy floor remained. Ten private builds and the retained native139 subset passed;139 is part of existing1,076 native cases. The separately published151-artifact scratch packet qualifies196 business comparisons+3canaries+4protocol cases; those are separate counts, not native inflation.

The one8s window opened after four genuine completed failed MULTIPLE observations. A writable-FD/dev/inode match ties one finite point to the exact owned Projects PID; it does not prove exclusive authorship of every row. Three local captures produced six strict numeric rows, with zero allocation/drop/I/O/active/queued final fields. Whole durations are1998.447402,1999.031979,2000.482882ms. Each terminal vector is last10(Response),firstFailure6(FinalCandidates),termination6(OperationFallbackEntered),exception1(Cancellation),caller0,linked1.

These are local control-flow observations. Marker6 does not prove a nonempty candidate set, any callback starting, or time consumed in that wave. The first existing-catch failure marker was saved before fallback Response changed the last marker. Cancellation bits observed in finally do not prove timer/deadline causality, exact throw site, dependency/refusal cause, serialized payload outcome, or downstream quiescence. Stage4 times terminal unary encoding, not the FinalCandidates await. Postserialization local omission observations are not payload classifications. No capture ordinal is joined to a readiness attempt, request, other service, or prior timing/profile. Whole includes post-admission bookkeeping; no overhead was subtracted.

The original managed launcher generated a fresh seven-value source mapping. Its values/hash are preserved separately from prepared build/native mapping; no all-seven cross-run identity is claimed. The wrapper adds only the reviewed exact Aspire--no-build argument to retain prebuilt diagnostic binaries; it is not labelled the unchanged ordinary npm acceptance command. No runtime environment/configuration/logger/deadline/privacy override was added.

Normal cleanup and eleven forced ordinary builds completed. All10 affected Projects.Server producer/copy/ref/refint artifacts match the ordinary producer by kind. SDK137/core8/active32 SDK+Contracts files and147+2 marker scopes passed. Domain98 remained baseline; Server66 comprised62 baseline+4 current-producer files; Gateway34 comprised30+4. Five of the 149 checked DLLs differ from the pre-run baseline in documented ordinary build scopes. This does not retrospectively qualify historical10/17 generation gaps or imply every copy is byte-identical to native1,076 generation. Source/full-manifest/frozen approval/sprint/index checks passed; process/native groups and five containers closed. Five initial/delayed cleanup observations are retained. Root copied only JSONL+closed metadata, then removed exactly the three validated owned files; the pinned directory is empty. Control bytes are not packaged.

The first independent final-audit attempt stopped at an overly strict prepared-vs-fresh-managed mapping equality assertion after validating the six rows in RAM; it emitted no report. Its source and a closed classification projection are retained as an audit-assumption failure. Provisional600fd is not the final audit. Accepted final d3419f76/source34776103 reads the immutable archive and separates mapping generations. Native audit a1d16 and guarded archive proofs are included. Raw stdout/logs, private credentials/configuration/graphs, binaries and runtime payloads are excluded. Original sanitized metadata is copied unchanged; larger/unsafe managed outputs use explicit projections with original hashes.
'''
(OUT/'README.md').write_bytes(readme.replace('\r\n','\n').replace('\n','\r\n').encode())
write('provenance.json',{'byteExactCopiesAndGzipTransformation':provenance,'projectionsCarryTheirOwnOriginalSha256AndBytes':True,'firstSealedGenerationPreservedAt':'/tmp/story65-advisory-terminal-live-evidence-history-20261108/first-sealed','firstManifestSha256':'f1a4b01da7430e1c6fee9f68f49ea008f30eea5c726272cf578c8769abf367f8','firstSha256sumsSha256':'4a6cf52e5a846d43b652be1ad1d95c2c2e0835dc49da9fc2d248a3e2c6667088'})
artifacts={str(p.relative_to(OUT)):sha(p)for p in sorted(OUT.rglob('*'))if p.is_file()}
write('manifest.json',{'diagnosticOnly':True,'featureAcceptance':False,'artifacts':len(artifacts),'filesSha256':artifacts,'finalIndependentAuditSha256':sha(final/'independent-final-audit.json'),'nativeIndependentAuditSha256':sha(Path('/tmp/story65-advisory-terminal-native-audit-20261108/independent-private-native-audit.json')),'scratchReferenceOnly':True})
files=sorted(p for p in OUT.rglob('*')if p.is_file());(OUT/'SHA256SUMS').write_text(''.join(f'{sha(p)}  {p.relative_to(OUT)}\n'for p in files))
assert all(sha(OUT/name)==h for name,h in artifacts.items())
print(json.dumps({'directory':str(OUT),'artifactsExcludingSums':len(files),'manifestSha256':sha(OUT/'manifest.json'),'sha256sumsSha256':sha(OUT/'SHA256SUMS'),'counterRows':len(raw.splitlines()),'sealed':True}))
