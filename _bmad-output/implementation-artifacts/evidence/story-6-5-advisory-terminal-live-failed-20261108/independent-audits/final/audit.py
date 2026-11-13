"""Read-only audit after closed managed cleanup and mandatory ordinary restoration."""
from pathlib import Path
import ast,collections,datetime,hashlib,json,os,re,stat,statistics,subprocess,xml.etree.ElementTree as ET
ROOT=Path('/home/administrator/projects/hexalith/projects');P=Path('/tmp/story65-advisory-terminal-live-prep-20261108');S=Path('/tmp/story65-advisory-terminal-prep-20261108');E=Path('/tmp/story65-advisory-terminal-diagnostic-20261108');W=P/'operational-ordinary-0leyzmb9';PRIVATE=Path('/tmp/story65-capture-counter-private-20261107');O=Path(__file__).parent;bindings={}
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def unique(pairs):
 d={}
 for k,v in pairs:assert k not in d;d[k]=v
 return d
def load(p):
 p=Path(p);bindings[str(p)]=sha(p);return json.loads(p.read_bytes(),object_pairs_hook=unique)
pins=load(P/'operational-pins.json');assert bindings[str(P/'operational-pins.json')]=='de8aa188d2341afd516e52d3370ec3d20a5446dc63f75a0c98e7e2722adb78c7'
for name,h in pins['files'].items():assert sha(P/name)==h
for name,h in pins['externalFiles'].items():assert sha(name)==h
assert len(pins['files'])==24 and len(pins['externalFiles'])==154
status=load(P/'latest-operational-result.json');assert all(status[k]for k in ['privateAttempted','native139Attempted','managedAttempted','ordinaryRestoreAttempted','measurementComplete'])
assert all(status[k]==0 for k in ['privateExitCode','native139ExitCode','managedExitCode','ordinaryExitCode'])and status['underlyingManagedLauncherExitCode']==1
supervisor=load(E/'supervisor.json');validation=load(E/'closed-recorder-validation.json');cleanup=load(E/'independent-cleanup-source-audit.json');supp=load(E/'supplemental-cleanup-qualification.json');early=load(E/'early-runtime-identities.json')['identities'];proof=load(E/'runtime-private-projects-proof.json');run=load(E/'managed/run.json');original=load(Path(load(P/'latest-private-attempt.json')['attemptDirectory'])/'receipt.json');native_dir=Path(load(P/'latest-native-attempt.json')['attemptDirectory']);native=load(native_dir/'receipt.json')
assert supervisor['launcherExitCode']==1 and supervisor['controlPublished']and supervisor['noTopologyCutoffOrSignals']and validation['complete']and validation['captures']==3 and validation['rows']==6
assert cleanup['runResult']==run['result']=='failed'and not cleanup['matrixCompleted']and not run['matrixCompleted']
assert [cleanup[k]for k in ['singleAttempts','singleVerifiedPositives','multipleAttempts','multipleVerifiedPositives']]==[1,1,30,0]
assert all(cleanup[k]for k in ['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent'])and cleanup['fullLiveManifestHashMismatches']==0
assert supp['safeForOrdinaryRestoration']and supp['earlyRuntimeIdentityCount']==12 and supp['allEarlyIdentitiesAbsent']and supp['allRecordedPidStartIdentitiesAbsentIncludingZombies']and supp['allNativeSeedGroupsFreshlyAbsentIncludingZombies']
assert len(early)==6 and len(cleanup['identities'])==21 and len(cleanup['containers'])==5
assert supervisor['failedMultipleBeforeAdmission']>=3 and supervisor['plannedControlIdentity']==supervisor['controlIdentity']
# Original private reads finished before Root archive. Only immutable copied numeric files are read here.
archive=load(E/'counter/archive-cleanup.json');before_archive=load(E/'counter/archive-before-removal.json')
assert archive['exactOwnedFilesRemoved']and archive['fixedDirectoryEmpty']and not archive['controlContentsCopied']and archive['copiedOnly']==['projects.jsonl','projects.closed.json']and archive['validated']==validation
raw={n:(E/'counter'/n).read_bytes()for n in ['projects.jsonl','projects.closed.json']};custody=validation['artifacts']
for n,data in raw.items():
 assert len(data)==custody[n]['bytes']and hashlib.sha256(data).hexdigest()==custody[n]['sha256'];bindings[str(E/'counter'/n)]=sha(E/'counter'/n)
control=f"{supervisor['runOrdinal']}\n{supervisor['frequency']}\n{supervisor['admissionStartTicks']}\n{supervisor['admissionEndTicks']}\n".encode()
assert len(control)==archive['controlBytes']==custody['window.control']['bytes']and hashlib.sha256(control).hexdigest()==archive['controlSha256']==custody['window.control']['sha256']
closed=json.loads(raw['projects.closed.json'],object_pairs_hook=unique);assert closed==validation['closed']
closed_fields={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
assert set(closed)==closed_fields and all(type(v)is int and v>=0 for k,v in closed.items()if k!='role')
assert closed['role']=='projects'and closed['runOrdinal']==supervisor['runOrdinal']and closed['admitted']==closed['disposedOrUnwound']==3 and closed['writtenRows']==6 and closed['writtenBytes']==len(raw['projects.jsonl'])
assert closed['frequency']==supervisor['frequency']==1000000000 and closed['windowStartTicks']==supervisor['admissionStartTicks']and closed['windowEndTicks']==supervisor['admissionEndTicks']and closed['windowEndTicks']-closed['windowStartTicks']==8*closed['frequency']
assert closed['diagnosticOnly']==closed['countersFinal']==1 and closed['featureAcceptance']==0 and closed['maximumRows']==65536 and closed['maximumBytes']==8388608 and closed['queueCapacity']==4096
assert all(closed[k]==0 for k in ['activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease'])
counts=['yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'];fields={'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency','allocationMode','allocatedBytes',*counts};groups={}
for line in raw['projects.jsonl'].splitlines():
 row=json.loads(line,object_pairs_hook=unique);assert set(row)==fields and row['role']=='projects'and row['stage']in ['Whole','SettingsEnumerationSortDictionary']and all(type(v)is int and v>=0 for k,v in row.items()if k not in ['role','stage'])
 assert row['runOrdinal']==supervisor['runOrdinal']and row['frequency']==1000000000 and row['allocationMode']==row['allocatedBytes']==0
 values=[row[k]for k in counts];assert all(v<=limit for v,limit in zip(values,[11,11,7,2,1,1]))and(row['stage']!='Whole'or values==[0]*6)
 g=groups.setdefault(row['captureOrdinal'],{});assert row['stage']not in g;g[row['stage']]=row
assert set(groups)=={1,2,3};whole=[];encoding=[];vectors=[]
for g in groups.values():
 assert set(g)=={'Whole','SettingsEnumerationSortDictionary'};a,b=g['Whole'],g['SettingsEnumerationSortDictionary'];assert b['elapsedTicks']<=a['elapsedTicks'];whole.append(a['elapsedTicks']/1e6);encoding.append(b['elapsedTicks']/1e6);vectors.append([b[k]for k in counts])
assert vectors==[[10,6,6,1,0,1]]*3
physical=supervisor['physicalWriterProof'];assert physical['observed']and physical['role']=='projects'and physical['exactFixedOutputInodeMatched']and physical['writable']and all(physical[k]==custody['projects.jsonl'][k]for k in ['device','inode'])
# Prove source selection/runtime ownership only at the recorded private boundary; current binaries are ordinary.
assert proof['source65Match']and proof['fullManifestAllMatch']and proof['fullManifestCount']==17361 and proof['sourceMappingValuesUnchangedByWrapper']and proof['noNewRuntimeEnvironmentKeys']and proof['allSixOwnedRuntimeEntryAndSdkContractsMapsRequired']and proof['privateProjectsEntryMapRequired']
assert proof['privateProducerSha256']==original['producerSha256']==native['privateServerBefore']['producerSha256']and proof['privateProducerPdbSha256']==native['privateServerBefore']['producerPdbSha256']
assert len(proof['copies'])==len(run['runtimeModes'])==6
for x in proof['copies']:
 r=run['runtimeModes'][x['role']];assert all(x[k]==r[k]for k in ['pid','startTicks','wrapperPid','wrapperStartTicks'])and x['entryAssemblySha256']==r['assemblySha256']
assert physical['pid']==next(x['pid']for x in proof['copies']if x['role']=='projects')
mapping=load(P/'mapping-pins.json')['environment'];managed_mapping=run['sourceMappingEnvironment'];assert set(managed_mapping)==set(mapping)and len(managed_mapping)==7 and managed_mapping['Configuration']=='Debug'and run['sourceMappingHash']==managed_mapping['HEXALITH_SOURCE_MAPPING_HASH']
assert all(Path(managed_mapping[k]).name==Path(mapping[k]).name for k in mapping if k not in ['Configuration','HEXALITH_SOURCE_MAPPING_HASH'])
profile=load(P/'expected-managed-profile.json')['compilationProfile'];assert run['compilationProfile']==profile and len(profile)==10
for v in profile.values():assert v['Configuration']=='Debug'and v['Optimize']=='false'and v['EnablePrivateCatalogInvocationProof']==v['EnablePrivateOwnerSourceReadProof']=='true'
substitution=load(E/'aspire-nobuild-substitution.json');assert substitution['exactOriginalCommandMatched']and substitution['onlyAddedArgument']=='--no-build'and substitution['actualCommand']==substitution['originalCommand']+['--no-build']and substitution['noRuntimeEnvironmentChange']
manifest=load(E/'managed/source-manifest.json');assert len(manifest)==17361 and bindings[str(E/'managed/source-manifest.json')]==proof['fullManifestSha256']
for rel,h in manifest.items():assert sha(ROOT/rel)==h
binding=load(P/'source-qualification-binding.json');source=load(binding['currentSourceControls'])['sourceFilesSha256'];assert len(source)==65
for rel,h in source.items():assert sha(ROOT/rel)==h
# Reuse only the pinned read-only census functions (no module import, build, command or write side effects).
ops=ast.parse((P/'ops_common.py').read_text());names={'marker','permitted_roots','census','expected_at'};nodes=[n for n in ops.body if isinstance(n,ast.FunctionDef)and n.name in names];markers=next(ast.literal_eval(n.value)for n in ops.body if isinstance(n,ast.Assign)and any(isinstance(t,ast.Name)and t.id=='MARKERS'for t in n.targets));env={'ROOT':ROOT,'Path':Path,'os':os,'re':re,'sha':sha,'ASSEMBLY':'Hexalith.Projects.Server','MARKERS':markers};exec(compile(ast.Module(body=nodes,type_ignores=[]),'<pinned-readonly-census>','exec'),env)
ordinary=load(W/'receipt.json');assert ordinary['phase']=='ordinary'and ordinary['completed']and ordinary['allPrivateMarkersAbsent']and ordinary['primaryBuilds']==10 and ordinary['nativeOrdinaryBuilds']==1
commands=load(W/'commands.json');assert len(commands)==11
for c in commands:assert c['exitCode']==0 and sha(W/c['log'])==c['logSha256']and all(v in c['command']for v in ['-t:Rebuild','-p:BuildProjectReferences=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-p:Story65TerminalPrivate=false'])
compiled=load(W/'actual-projects-producer-compiled-proof.json');observed=load(W/'actual-advisory-compiler-inputs.json');assert not compiled['private']and compiled['originalOnlyObserved']and not compiled['sevenPrivateSourcesObserved']and compiled['observedInputsSha256']==bindings[str(W/'actual-advisory-compiler-inputs.json')]
actual=[x['path']for x in observed['observedInputs']if x['kind']=='Compile'];assert actual.count(str(ROOT/'src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs'))==1 and not any(str(S)+'/'in x for x in actual)
after=load(W/'after-census.json');current=env['census']();assert {x['path']:x for x in current}=={x['path']:x for x in after}
affected=load(W/'affected-paths-union.json');initial={x['path']:x for x in load(Path(load(P/'latest-private-attempt.json')['attemptDirectory'])/'before-census.json')};target=ROOT/'src/Hexalith.Projects.Server';expected={k:sha(env['expected_at'](target,'Hexalith.Projects.Server',k))for k in ['full','pdb','ref','refint']}
assert len(after)==ordinary['censusCopies']==10 and len(affected['paths'])==affected['affected']==10 and affected['unrelatedExactBaseline']==0 and affected['includesPrivateOrdinaryAndNewAfterFallbackPaths']
for row in after:assert not row['privateMarker']and row['sha256']==(expected[row['kind']]if row['path']in affected['paths']else initial[row['path']]['sha256'])
inv=load(P/'affected-inventory.json');dependencies={}
for assembly,g in inv['independentModules'].items():
 rows=env['census'](assembly);old={x['path']:x for x in g['rows']};exact=new=0
 assert set(old)<={x['path']for x in rows}
 for row in rows:
  assert not row['privateMarker']
  if row['path']in old and row['sha256']==old[row['path']]['sha256']:exact+=1
  else:assert row['project']in inv['projects'].values()and row['sha256']==sha(env['expected_at'](ROOT/g['producer'],assembly,row['kind']));new+=1
 dependencies[assembly]={'census':len(rows),'exactBaseline':exact,'currentOwnedProducer':new}
 assert ordinary['independentDependencies'][assembly]['censusCount']==len(rows)
for n,h in inv['core8Sha256'].items():assert sha(n)==h
sdkrows=env['census']('Hexalith.EventStore.ServiceDefaults');old={r['path']:r for r in inv['sdk137']};assert len(sdkrows)==137
for row in sdkrows:assert not row['privateMarker']and row['path']in old and row['sha256']==old[row['path']]['sha256']
sdk=ordinary['ordinarySdkProof'];assert sdk['producerSha256']=='873dbb74ee259d91c99ea2804e5801134c993abb4ef7806e79f242e5d23faf96'and len(sdk['copies'])==8 and sdk['allEightOrdinarySdkAndContractsCopiesCoherent']
for row in sdk['copies']:
 f=ROOT/row['path'];assert sha(f)==row['sha256']and sha(f.with_suffix('.pdb'))==row['pdbSha256']and sha(f.with_name('Hexalith.EventStore.Contracts.dll'))==row['contractsSha256']and sha(f.with_name('Hexalith.EventStore.Contracts.pdb'))==row['contractsPdbSha256']
appchanges=[]
for group in ['app147Sha256','additional2Sha256']:
 for n,h in inv[group].items():
  f=Path(n)if Path(n).is_absolute()else ROOT/n;assert not env['marker'](f)
  if sha(f)!=h:appchanges.append(n)
assert len(inv['app147Sha256'])==147 and len(inv['additional2Sha256'])==2
# Fresh exact PID/start and PGID/SID checks include zombies and never signal targets.
identities=[(x['pid'],str(x['startTicks']))for x in cleanup['identities']]+[(supervisor['launcherPid'],str(supervisor['launcherStartTicks']))]
for x in early:identities.extend([(x['pid'],str(x['startTicks'])),(x['wrapperPid'],str(x['wrapperStartTicks']))])
ni=native['ownedNativeIdentity'];identities.append((ni['pid'],str(ni['startTicks'])));groupids={ni['processGroup']}|{x['groupId']for x in cleanup['nativeTestProcessCleanup']};seen={};permission_errors=0
for p in Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:
  f=(p/'stat').read_text().rsplit(')',1)[1].split();seen[int(p.name)]=(f[19],int(f[2]),int(f[3]),f[0])
 except(FileNotFoundError,ProcessLookupError):pass
 except PermissionError:permission_errors+=1
assert permission_errors==0 and all(seen.get(pid,(None,))[0]!=ticks for pid,ticks in identities)and all(v[1]not in groupids and v[2]not in groupids for v in seen.values())
assert len(groupids)==2
for x in cleanup['containers']:
 assert x['absenceVerified']and re.fullmatch('[0-9a-f]{64}',x['containerId']);q=subprocess.run(['docker','ps','--all','--quiet','--no-trunc','--filter','id='+x['containerId']],capture_output=True,timeout=15);assert q.returncode==0 and not q.stdout.strip()
# Frozen approval/sprint/index evidence is independent of mutable ordinary assembly generations.
protected_binding=load(Path('/tmp/story65-canonical-current-gateway-cpu-prep-20261108/watcher/ordinary-binding.json'))['protectedBaseline'];assert sha(protected_binding['path'])==protected_binding['sha256'];baseline=load(protected_binding['path']);spec=ROOT/'_bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md';frozen=re.search(rb'<frozen-after-approval.*?</frozen-after-approval>',spec.read_bytes(),re.S);assert frozen is not None and hashlib.sha256(frozen.group()).hexdigest()==baseline['frozen_sha256']and sha(ROOT/'_bmad-output/implementation-artifacts/sprint-status.yaml')==baseline['sprint_sha256'];assert hashlib.sha256(subprocess.check_output(['git','diff','--cached','--binary'],cwd=ROOT)).hexdigest()==baseline['index_sha256']
# Only the test type/result is retained; exception text and business payloads are never exposed.
xml=E/'managed/browser-seed.xml';b=xml.read_bytes();assert b'<!DOCTYPE'not in b and b'<!ENTITY'not in b;t=list(ET.fromstring(b).iter('test'));assert len(t)==1 and t[0].get('type')=='Hexalith.Projects.Integration.Tests.SupportedProjectBrowserSeedTests'and t[0].get('result')=='Fail';bindings[str(xml)]=sha(xml)
report={'passed':True,'diagnosticOnly':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'auditSourceSha256':sha(__file__),'evidenceSha256':bindings,'pipeline':{'privateBuilds':10,'nativeRepeatedCases':139,'nativeAdditionalUniqueCases':0,'ordinaryForcedBuilds':11,'recordedPipelineAndBuildExitCodesZeroExceptUnderlyingManagedOne':True,'underlyingManagedExitCode':1},'outcome':{'singleAttempts':1,'singleVerifiedPositives':1,'multipleAttempts':30,'multipleVerifiedPositives':0,'browserSeedFailedBeforeBrowserAssertions':True,'browserAcceptanceAssertions':0,'matrixCompleted':False},'records':{'captures':3,'rows':6,'rowFields':14,'closedFields':23,'schemaAndEnumsAndWholeBoundsPassed':True,'allocationAndAllDropIoActiveQueuedFieldsZero':True,'wholeMilliseconds':whole,'wholeMedianMilliseconds':statistics.median(whole),'terminalEncodingMilliseconds':encoding,'allTerminalVectors':[10,6,6,1,0,1],'decodedOnly':['last outer boundary marker: Response','first caught failure outer boundary marker: FinalCandidates','OperationFallbackEntered','caught exception kind: OperationCanceledException','caller cancellation observed at Close: false','linked cancellation observed at Close: true'],'physicalOwnedProjectsWritableFdInodeMatchedAtOnePoint':True,'physicalPointDoesNotProveAllRowExclusivity':True,'custody':custody,'originalPrivateReadsBeforeRootArchiveMatchedValidationCustody':True,'rootArchivedTwoNumericFilesAndRemovedExactlyThreeOwnedFiles':True,'archiveControlContentsCopied':False,'noPrivateCounterReadsAfterReadCompleteHandoff':True},'privateRuntimeProof':{'sixOwnedEntryAndSdkContractsMapsGuardedAtPrivateBoundary':True,'allSixRuntimePidStartEntryHashesMatchOriginalObserver':True,'privateProjectsDllPdbMatchNativeBoundary':True,'managedOriginalSevenReemittedKeysAndTenEightPropertyProfilesMatch':True,'managedMappingsAreFreshOriginalLauncherGeneration':True,'managedMappingEqualsPreparedBuildNativeMapping':managed_mapping==mapping,'onlyAspireNoBuildArgumentAdded':True,'source65AndFull17361CurrentMatch':True,'noCurrentPrivateDllHashGuaranteeAfterRestoration':True},'ordinaryRestoration':{'originalCompilePresentAndAllPrivateSevenAbsent':True,'dynamicProjectsServerCensus':10,'affectedKindCoherent':10,'unrelatedExactBaseline':0,'dependencies':dependencies,'sdk137MarkerFreeAndExactScopedBaseline':True,'core8Current':True,'activeSdkContractsFiles':32,'sdkSha256':sdk['producerSha256'],'app147Plus2MarkerChecked':True,'currentAppGenerationDifferences':appchanges,'allRuntimePrivateMarkersAbsentForExplicitMarkerList':True,'markerStringsChecked':[x.decode()for x in markers],'frozenApprovalSprintIndexMatch':True},'cleanup':{'managedRecordedIdentities':21,'earlyWrapperAndEntryIdentities':12,'uniqueAuditedPidStartIdentities':len(set(identities)),'freshPidStartAndTwoNativePgidSidAbsenceIncludingZombies':True,'freshProcPermissionErrors':0,'fiveKnownOwnedContainersFreshlyAbsent':True,'namespaceAndPrivateRuntimeDirectoryAbsenceFromOriginalClosedCleanupProof':True,'preexistingResourcesPreservedByOriginalClosedProof':True,'fixedCounterDirectoryIdentityUnchanged':True,'fixedCounterDirectoryEmptyAfterRootGuardedArchive':archive['fixedDirectoryEmpty']},'limits':['Terminal values are local control-flow observations. OCE plus cancellation flags observed at finally do not establish the exact throw site, timeout cause, deadline expiry, dependency, request, route or serialized payload outcome.','Whole begins at the helper admitted timestamp and includes posttimestamp bookkeeping; it excludes pre-admission name eligibility/initial lock waiting. Stage4 measures only terminal unary encoding, not the FinalCandidates await.','No joining of these capture ordinals to readiness attempts, requests, previous wave/client/controller measurements or another service is made.','The physical writable-FD proof attributes one point to the owned Projects PID and exact output inode; it does not prove exclusive authorship of every row.','Private build/native runtime hashes are historical boundary facts; only the independently checked current ordinary producer-kind/generation scopes are asserted now. Historical10/17 generation limits are retained.','Native139 repeats existing1076 coverage; synthetic196+3canary+4protocol remain separate. This diagnostic run did not achieve ordinary acceptance.']}
(O/'independent-final-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'report':str(O/'independent-final-audit.json'),'sha256':sha(O/'independent-final-audit.json'),'captures':3,'rows':6,'ordinaryBuilds':11,'dependencies':dependencies,'ordinaryCensus':10,'appGenerationChanges':len(appchanges),'auditedUniqueIdentities':len(set(identities))}))
