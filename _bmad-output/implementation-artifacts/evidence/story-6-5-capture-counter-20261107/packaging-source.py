from pathlib import Path
import hashlib,json,re,shutil

P=Path('/tmp/story65-capture-counter-evidence-20261107')
ROOT=Path('/home/administrator/projects/hexalith/projects')
VERSIONS=[('v1','/tmp/story65-capture-counter-prep-20261107','/tmp/story65-capture-counter-diagnostic-20261107'),('v2','/tmp/story65-capture-counter-nobuild-prep-20261107','/tmp/story65-capture-counter-nobuild-diagnostic-20261107'),('v3','/tmp/story65-capture-counter-proof-prep-20261107','/tmp/story65-capture-counter-proof-diagnostic-20261107')]
def sha(path):
 with Path(path).open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def put(source,relative):
 source=Path(source);assert source.is_file()and not source.is_symlink();target=P/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target);assert sha(source)==sha(target)
def save(relative,value):
 target=P/relative;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(value,indent=2)+'\n')

for version,prep,evidence in VERSIONS:
 prep=Path(prep);evidence=Path(evidence);pins=json.loads((prep/'operational-pins.json').read_text())
 put(prep/'operational-pins.json',Path(version)/'prep/operational-pins.json')
 for name,digest in pins['files'].items():
  assert sha(prep/name)==digest;put(prep/name,Path(version)/'prep'/name)
 for name in['preparation-review.json','syntax-dryrun-receipt.json','latest-operational-result.json','parent-ordinary-restoration-audit.json']:
  if(prep/name).is_file():put(prep/name,Path(version)/'prep'/name)
 for attempt in prep.glob('operational-*'):
  if not attempt.is_dir():continue
  for file in attempt.rglob('*'):
   if file.is_file():
    assert file.suffix in['.json','.log','.txt'];put(file,Path(version)/'build-and-restore'/attempt.name/file.relative_to(attempt))
 for file in evidence.iterdir():
  if file.is_file():
   assert file.suffix in['.json','.log'];put(file,Path(version)/'managed'/file.name)
 for file in(evidence/'managed').iterdir():
  if file.is_file():
   assert file.suffix in['.json','.jsonl','.log','.xml'];put(file,Path(version)/'managed/artifacts'/file.name)

v1=Path(VERSIONS[0][1])
for name in['differential-cases.json','differential-build-1-receipt.json','differential-build-1.log','differential-build-2-receipt.json','differential-build-2.log','differential-build-3-receipt.json','differential-build-3.log','differential-build-final-receipt.json','differential-build-final.log','run_differential.py']:
 if(v1/name).is_file():put(v1/name,Path('shim-qualification')/name)
for name in['Differential.csproj','NuGet.Config','Program.cs','InstrumentedInputs.cs']:put(v1/'differential'/name,Path('shim-qualification/source')/name)
for directory in['results','results-initial']:
 for file in(v1/directory).rglob('*'):
  if file.is_file()and file.suffix=='.json':put(file,Path('shim-qualification')/directory/file.relative_to(v1/directory))
mapping=json.loads((v1/'mapping-pins.json').read_text());put(mapping['mappingLog'],Path('mapping-provenance/mapping.log'))
for index,(name,digest)in enumerate(mapping['files'].items()):assert sha(name)==digest;put(name,Path('mapping-provenance/files')/str(index)/Path(name).name)
for name in['/tmp/story65-receiver-acceptance-prep-20261107/audit.py',str(ROOT/'tools/qualification/run_supported_project_reads.py')]:put(name,Path('external-sources')/Path(name).name)

owner=Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json');receipt=json.loads(owner.read_text());assert len(receipt['sourceSha256'])==48
for relative,digest in receipt['sourceSha256'].items():
 source=ROOT/'references/Hexalith.EventStore'/relative;assert sha(source)==digest;put(source,Path('owner-source-snapshot')/relative)
extra=ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/DevelopmentRouteCatalogInputs.cs';assert sha(extra)=='5e85827847578384a7dcca9fe448b97f21cdf4a201263c2d9ec9d0f3eda6be4d';put(extra,Path('owner-source-snapshot/src/Hexalith.EventStore.ServiceDefaults/Routing/DevelopmentRouteCatalogInputs.cs'))
save('source-preservation.json',{'owner48AllMatchedAfterOrdinaryRestoration':True,'extraCaptureOriginalMatched':True,'extraCaptureSourceSha256':sha(extra),'ownerHead':receipt['ownerHead'],'ownerReceiptSha256':sha(owner),'noProductSourceGitBuildOrHostChangesByPackaging':True})

durable=[]
for name in['story-6-5-parsed-template-ordinary-20261107','story-6-5-parsed-template-cpu-20261107']:
 location=ROOT/'_bmad-output/implementation-artifacts/evidence'/name;assert(location/'SHA256SUMS').is_file();durable.append({'packet':str(location.relative_to(ROOT)),'SHA256SUMSSha256':sha(location/'SHA256SUMS')})
save('qualification-and-historical-references.json',{'currentNativeUniquePassingCases':1038,'ownerNativeCases':412,'projectsNativeCases':626,'nativeEvidenceDuplicatedHere':False,'durablePackets':durable,'scratchCoreMatrixCells':285,'scratchSupplementalCells':45,'scratchTotalCells':330,'scratchCorePairedExecutions':665,'scratchSupplementalPairedExecutions':105,'scratchRecorderStressCases':16,'scratchCellsAreNotAdditionalNativeOrFeatureAcceptanceCases':True,'featureAcceptance':False})

v3=Path(VERSIONS[2][2]);run=json.loads((v3/'managed/run.json').read_text());readiness=[json.loads(line)for line in(v3/'managed/browser-advisory-seed-readiness.jsonl').read_text().splitlines()]
save('actual-managed-result.json',{'diagnosticOnly':True,'featureAcceptance':False,'standardManagedLaneAllowedToComplete':True,'noCutoffOrTargetSignals':True,'singleAttempts':sum(row['multiple']is False for row in readiness),'singleVerifiedPositiveAttempts':[row['attempt']for row in readiness if row['multiple']is False and row['positiveCandidatesVerified']is True],'multipleAttempts':sum(row['multiple']is True for row in readiness),'multipleVerifiedPositiveAttempts':[row['attempt']for row in readiness if row['multiple']is True and row['positiveCandidatesVerified']is True],'managedResult':run['result'],'browserCases':0,'authenticatedBrowserPersistencePrivacyAccessibilityAcceptance':'not exercised; native seed failed','runtimePrivateTimingIsNotFeatureAcceptance':True,'canonicalRevisions':run['revisions'],'originalFailurePreserved':run['failure']})

forbidden=[];badnames=[]
for file in P.rglob('*'):
 if not file.is_file():continue
 if file.suffix in['.dll','.pdb','.pfx','.pem','.key','.der','.nettrace','.etlx']or file.name in['window.control','fixture.json','endpoints.json','apphost-session.json']:badnames.append(str(file.relative_to(P)))
 data=file.read_bytes()
 if re.search(rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}',data)or(bytes([45])*5+b'BEGIN PRIVATE KEY'+bytes([45])*5)in data or(bytes([45])*5+b'BEGIN RSA PRIVATE KEY'+bytes([45])*5)in data:forbidden.append(str(file.relative_to(P)))
assert not forbidden and not badnames
save('privacy-and-exclusions.json',{'closedCounterSchemaTypesLabelsPartitionsValidatedBeforeCopy':True,'copiedOnlyApprovedSixCounterMetadataFiles':True,'noRawConfigurationKeyValueProviderTraceObservationObjectCredentialTrustProofControlFileOrRuntimeTraceCopied':True,'counterRowsContainNoInheritedActivityRequestOrSubjectIdentifiers':True,'noJwtOrPrivateKeyPatterns':True,'noBinaryPrivateKeyCertificateTraceOrControlFiles':True,'matchingValuesPrinted':False,'counterPrivateDirectoryEmptyAfterExactOwnedRemoval':True,'historicalSetupAndFailedCleanupObservationsPreserved':True,'ordinaryRestorationIndependentProofRetained':True})

(P/'README.md').write_text('''This staging packet records a bounded private, metadata-only diagnostic of DevelopmentRouteCatalogInputs.CaptureAsync. Story 6.5 acceptance remains failed. Current native qualification is 412 owner + 626 Projects = 1,038 unique passes, referenced through the already durable parsed-template ordinary and CPU packets; no native packet or application binary is duplicated here.

The unchanged capture iterator still enumerates all configuration entries exactly once, reads Value before filtering, retains every original provider/key/value/file/token/actor observation and order, sorts OrdinalIgnoreCase and constructs the same dictionary. The one explicit DeclaredInvocations tail lookup remains, in addition to the full-enumeration value observation when present. No production source changed. The reviewed helper/private-copy content hashes are 112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498 and c266cbdb2ea9325daa818dab74e2d30ea0077a3ac5a59ab39cfa89f7238745ab. Scratch qualification includes 285 core plus 45 supplemental matrix cells (665 + 105 actual paired executions), with equal output bytes, provider read traces, error/parameter behavior and expected successful/failing outcomes, including mutable providers/repeated captures/cancellation. Sixteen recorder stresses passed. These diagnostic cells are separate from native or feature acceptance. The stronger 330-cell source snapshot and final two-focused-test source/commands are retained; weaker initial comparisons are historical and do not substitute for outcome assertions.

Three setup generations are preserved separately. v1 privately rebuilt then let Aspire rebuild normally: the private SDK disappeared and the guard refused before seeding; no timing was recorded. Original cleanup audit initially could not establish complete proof, and the parent performed an independent startup recovery and forced ordinary restoration. v2 added the installed documented --no-build option only to the exact Aspire start command and used nine forced projects with BuildProjectReferences=false. It then exposed omitted existing compiler selectors: the prebuilt EventStore and Tenants hosts lacked EnablePrivateCatalogInvocationProof/EnablePrivateOwnerSourceReadProof, while the standard managed launcher enables both. Their disabled guard branches and closed InvalidOperation categories explain the failed startup; the broad source-project/PID guard did not retain which individual disjunct failed. v2 has its own original failure, primary-source cause receipt, cleanup recovery and ordinary restoration. No private configuration or raw graph is retained.

v3 selected the same two existing proof flags in both private and ordinary builds, checked the evaluated eight-property profile against the standard managed profile and required enabled EventStore/Tenants metadata with disabled guard literals absent before launch. All six actual owned runtimes then passed source-project, SDK/PDB and mapped-assembly checks. Runtime CPU override remained unset. No recorder configuration/environment keys, public logger/exporter, privacy-filter change, profiler, request or inherited Activity linkage was used. The private --no-build CLI substitution changes build timing only; authorization observations/order, replay/lease rules, policy, deadline and expectations stayed unchanged. Native integration compilation later used its original run-private artifacts directory.

Recording admitted captures for eight monotonic seconds after the first genuine failed MULTIPLE, with no positive observed and sufficient attempts remaining. It used only generated opaque run/capture ordinals, fixed role/stage labels and numeric counts/times/allocations. All 423 admitted captures disposed/unwound; 3,360 rows passed strict schema/type/partition checks. Projects has 27 complete captures, Folders 108, Gateway 284 complete plus four Whole/Component-only partial captures. Partial reasons were deliberately unobserved. All three closed receipts are final, with zero active/pending/dropped/capped/I/O counts. No files were published for the other three roles; this does not prove zero calls or cost.

The settings segment dominates within this method: Gateway median 1.147695 ms versus whole Capture 1.336661 ms, Folders 0.587146 versus 0.751899 ms, Projects 0.591695 versus 0.756334 ms. Settings same-thread allocated bytes have medians 641,160 / 389,360 / 378,448. Observed yielded/null/accepted/non-auth/sensitive/other partitions are Gateway 367=69+17+259+7+15, Folders 254=36+10+197+5+6, Projects 255=36+10+198+5+6. Per-role/stage medians, nearest-rank P95, maxima and paired within-capture shares are in counter-aggregation.json. These are instrumented wall values. Allocation covers one synchronous same-thread segment including instrumentation overhead, not async/whole allocation. Background recording can perturb execution. No overhead was subtracted; no exact CPU, ordinary latency gain, request/root call count or advisory critical-path attribution is claimed. The roughly 0.75–1.34 ms whole-Capture medians do not by themselves explain or resolve the two-second advisory failure. No configuration pruning or production repair is recommended from this evidence.

The v3 full managed lane completed normally: SINGLE passed attempt 2; MULTIPLE failed all 30 attempts; native seed failed and zero browser cases ran. Authenticated browser persistence/privacy/accessibility acceptance remains unexercised. Initial cleanup observation found three source-host identities still present shortly after stop and therefore truthfully deferred ordinary rebuilding. A later independent parent audit found all 21 recorded process identities, five containers, namespace/private material and native descendants absent without additional signals, with all 17,188 live source inputs unchanged. These initial negative and delayed positive receipts are both retained. Parent-authorized ordinary nine-project restoration then verified the standard proof profile, enabled entry metadata, 147 clean runtime DLL copies, eight SDK/PDB copies coherent with ordinary producer 5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872, 137 dynamic DLL/PDB/ref/refint entries and 24 affected coherent outputs. All 48 owner sources plus the extra original capture source matched. Build generations remain separately recorded; native byte equivalence is not inferred from source equality.

The approved six counter files were validated, hashed and copied before removal. Then only those six original files and numeric window.control were removed from the exact owned 0700 UID/device/inode directory. The control file was never copied. Independent emptiness/same-inode and no-symlink-follow proof is retained. No runtime private configuration, key, token, TrustProof, observed object, provider trace, raw graph, profiler trace or application binary is included. Existing packets and repository sources were not changed by packaging. This packet is ready for parent audit before any repository evidence copy or spec update.
''')
put('/tmp/story65-capture-counter-package.py',Path('packaging-source.py'))
files=sorted(f for f in P.rglob('*')if f.is_file()and f.name!='SHA256SUMS');(P/'SHA256SUMS').write_text(''.join(sha(file)+'  '+str(file.relative_to(P))+'\n'for file in files))
for line in(P/'SHA256SUMS').read_text().splitlines():digest,name=line.split('  ',1);assert sha(P/name)==digest
print(json.dumps({'packet':str(P),'entries':len(files),'checksumsSha256':sha(P/'SHA256SUMS'),'noPrivacyPatternViolations':True,'featureAcceptance':False}))
