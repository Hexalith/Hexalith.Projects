from pathlib import Path
import gzip,hashlib,json,re,stat,xml.etree.ElementTree as ET
OUT=Path(__file__).parent
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-domain-receiver-phase-prep-20261108')
Q=Path('/tmp/story65-domain-receiver-phase-qualification-native-output-retry-20261108')
LIVE=Path('/tmp/story65-domain-receiver-phase-diagnostic-20261108')
OPS=Path('/tmp/story65-domain-receiver-phase-ops-prep-20261108')
CLOSED=Path('/tmp/story65-domain-receiver-phase-closed-recorders-20261108')
PRODUCER=Path('/tmp/story65-domain-receiver-phase-producer-prep-20261108')
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
load=lambda p:json.loads(Path(p).read_bytes())
provenance=[]
def save(name,value):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);data=(json.dumps(value,indent=2)+'\n').encode();assert not p.exists()or p.read_bytes()==data;p.write_bytes(data)if not p.exists()else None
def put(source,name,projection=None,compress=False):
 source=Path(source);assert source.is_file()and not source.is_symlink();original=source.read_bytes()
 data=gzip.compress(original,mtime=0)if compress else((json.dumps(projection,indent=2)+'\n').encode()if projection is not None else original)
 dest=OUT/name;dest.parent.mkdir(parents=True,exist_ok=True);assert not dest.exists()or dest.read_bytes()==data;dest.write_bytes(data)if not dest.exists()else None
 provenance.append({'originalPath':str(source),'originalSha256':hashlib.sha256(original).hexdigest(),'originalBytes':len(original),'packetPath':str(dest.relative_to(OUT)),'packetSha256':sha(dest),'packetBytes':len(data),'encoding':'gzip-mtime0'if compress else'fixed-allowlist-projection'if projection is not None else'exact-copy'})
def scalars(value,fields):
 result={}
 for k in fields:
  if k in value:
   x=value[k];assert x is None or type(x)in[bool,int,float]or(isinstance(x,str)and re.fullmatch('[0-9a-f]{64}',x));result[k]=x
 return result
aggregate=load(Q/'qualified-receipt.json');assert sha(Q/'qualified-receipt.json')=='594d4cd13f58a3c0f6db246795d396f87edb6590eacbf7f605a0841fc8e798cb'
assert aggregate['qualified']is True and aggregate['featureAcceptance']is False and aggregate['controlledProcesses']==68 and aggregate['passingFocusedSemanticPairs']==64 and aggregate['diagnosticNativeExecutions']==206 and aggregate['strictValidatedRows']==167479
prep=load(PREP/'preparation-receipt.json');assert sha(PREP/'preparation-receipt.json')==aggregate['sourcePreparationSha256'];assert len(prep['sourceSha256'])==59
for name,h in prep['filesSha256'].items():assert sha(PREP/name)==h;put(PREP/name,Path('source-preparation')/name)
put(PREP/'preparation-receipt.json','source-preparation/preparation-receipt.json')
# Exact closed scratch metadata only; runtime DLL/PDB copies are deliberately excluded.
for base,bucket in [(Q,'qualified-q3'),(Path('/tmp/story65-domain-receiver-phase-qualification-20261108'),'failed-q1'),(Path('/tmp/story65-domain-receiver-phase-qualification-protocol-retry-20261108'),'failed-q2')]:
 for f in sorted(base.rglob('*')):
  if not f.is_file()or'runtime'in f.relative_to(base).parts:continue
  rel=f.relative_to(base)
  if f.suffix=='.jsonl':put(f,Path(bucket)/(str(rel)+'.gz'),compress=True)
  elif f.suffix in['.json','.xml','.log','.py']:
   if f.suffix=='.xml':assert b'http://'not in f.read_bytes()and b'https://'not in f.read_bytes()
   put(f,Path(bucket)/rel)
# Preserve original source receipts/controllers/indexes and runtime-manifest source-binding history.
for directory in [Path('/tmp/story65-domain-receiver-phase-history-20261108/d91e15a7-protocol-attempt1'),Path('/tmp/story65-domain-receiver-phase-history-20261108/8686-native-line-bound')]:
 for f in sorted(directory.rglob('*')):
  if not f.is_file()or any(t in f.relative_to(directory).parts for t in ['runtime','bin','obj']):continue
  if f.name in ['preparation-receipt.json','qualification_controller.py','owned-source-index.json']or('runtime-manifest'in f.name and f.suffix=='.json'):
   put(f,Path('protocol-history')/directory.name/f.relative_to(directory))
for directory,bucket in [(Path('/tmp/story65-domain-receiver-phase-fixture-build-20261108'),'fixture-build'),(Path('/tmp/story65-domain-receiver-phase-qualified-prep-20261108'),'aggregate-audit')]:
 for f in sorted(directory.rglob('*')):
  if f.is_file()and not any(t in f.relative_to(directory).parts for t in ['bin','obj','__pycache__'])and f.suffix in['.json','.log','.py','.md','.diff']:put(f,Path(bucket)/f.relative_to(directory))
for f in sorted(PRODUCER.rglob('*')):
 if f.is_file()and not any(t in f.relative_to(PRODUCER).parts for t in ['isolated-domain','isolated-oracle','__pycache__'])and f.suffix in['.json','.log','.py','.props','.targets','.diff']:put(f,Path('isolated-producer')/f.relative_to(PRODUCER))
for name in ['story65-domain-receiver-phase-runtime-manifest-20261108.json','story65-domain-receiver-phase-runtime-manifest-protocol-retry-20261108.json','story65-domain-receiver-phase-runtime-manifest-native-output-retry-20261108.json']:
 put(Path('/tmp')/name,Path('runtime-bindings')/name)
# Preserve source-only correction snapshots, not old binaries or entire unrelated packets.
for directory in sorted(Path('/tmp/story65-domain-receiver-phase-prep-history-20261108').iterdir()):
 if directory.is_dir():
  for f in sorted(directory.rglob('*')):
   if f.is_file()and f.suffix in['.cs','.json','.diff','.md','.py']and not any(t in f.relative_to(directory).parts for t in ['bin','obj']):put(f,Path('source-correction-history')/directory.name/f.relative_to(directory))
# Partial receiver captures: only strict helper112 enum/integer rows; original control is never copied.
zero={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
fields=zero|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
counts={};total=0
for role,expected in [('projects',(3,24)),('folders',(36,288))]:
 file=CLOSED/(role+'.jsonl');lines=file.read_bytes().splitlines();captures=set()
 for line in lines:
  x=json.loads(line);assert set(x)==fields and x['role']==role and all(type(x[k])is int and x[k]>=0 for k in fields-{'role','stage'})and all(x[k]==0 for k in zero);captures.add(x['captureOrdinal'])
 assert(len(captures),len(lines))==expected;counts[role]={'captures':len(captures),'rows':len(lines)};total+=len(lines)
 put(file,Path('partial-live')/(role+'.jsonl.gz'),compress=True);put(CLOSED/(role+'.closed.json'),Path('partial-live')/(role+'.closed.json'))
assert total==312 and not(CLOSED/'tenants.jsonl').exists()
put(CLOSED/'partial-validation.json','partial-live/partial-validation.json')
put(CLOSED/'archive-receipt.json','actual-partial-archive/archive-receipt.json')
for name in ['archive_partial.py','validate_partial.py','audit_partial.py']:
 put(Path('/tmp/story65-domain-receiver-phase-partial-analysis-prep-20261108')/name,Path('actual-partial-archive')/name)
analysis=Path('/tmp/story65-domain-receiver-phase-partial-analysis-20261108/independent-domain-receiver-phase-audit.json');report=load(analysis);assert report['coverage']['complete']is False and report['coverage']['missingRoles']==['tenants'];put(analysis,'partial-live/independent-domain-receiver-phase-audit.json')
parent=load(LIVE/'parent-post-restoration-audit.json');assert parent['passed']is True and parent['ordinaryBuildCommands']==13 and parent['fixedCounterDirectoryEmptyAtAudit']is False
# Parent restoration proof contains closed binary/source paths/hashes/groups, not runtime credentials or URLs.
put(LIVE/'parent-post-restoration-audit.json','ordinary-restoration/parent-post-restoration-audit.json')
for area,count in [('operational-private-z1zqybdv',12),('operational-ordinary-l7aq0d71',13)]:
 directory=OPS/area;commands=load(directory/'commands.json');assert len(commands)==count and all(x['exitCode']==0 for x in commands)
 for f in directory.iterdir():
  if f.is_file()and f.suffix=='.log':put(f,Path('build-restoration')/area/f.name)
 for name in ['commands.json','receipt.json','attempt-closed.json','sdk-proof.json','domain-proof.json','native147-marker-scope.json','after-census.json','after-native-artifacts.json','independent-dependency-generations.json']:
  f=directory/name
  if f.exists():put(f,Path('build-restoration')/area/name)
# Actual separately selected installed private native103 gate; no ordinary1048 inflation.
native=OPS/'native-private-domain-receiver-aodh4sm1';nr=load(native/'receipt.json');assert nr['passed']is True and nr['caseCount']==103
nprojection=scalars(nr,['diagnosticOnly','featureAcceptance','passed','nativeStarted','nativeProcessAndDescendantsAbsent','counterDirectoryEmpty','caseCount','sourceDerivedExpectedCases','historicalAllSelectionRetentionClaimed','ordinaryUniqueNativeCountUnchanged','source59Match','allSelectedProcessesMappedPrivateDomainService','sameOrdinarySdkBothVariants'])
assert nr['build']['exitCode']==0
nprojection['nativeTestArtifactBuild']={k:nr['build'][k]for k in ['command','cwd','exitCode','logSha256']}
nprojection['processes']=[];native_count=0
for i,r in enumerate(nr['processes'],1):
 f=native/f'selection-{i}.xml';assert sha(f)==r['xmlSha256']and b'http://'not in f.read_bytes()and b'https://'not in f.read_bytes();tests=ET.fromstring(f.read_bytes()).findall('.//test');assert all(t.attrib['result']=='Pass'for t in tests);native_count+=len(tests);put(f,Path('installed-private-native103')/f.name)
 row=scalars(r,['nativeStarted','processAndDescendantsAbsent','mappedPrivateDomainService','mappedOrdinarySdk','passed','nativeRuntimeProcessorCountOverrideAbsent','nativeRuntimeCpuUnmodified','exitCode','xmlSha256','logSha256','caseCount','ownedExitCode']);row.update(command=r['command'],cwd=r['cwd']);nprojection['processes'].append(row)
assert native_count==103;put(native/'receipt.json','installed-private-native103/receipt.fixed-projection.json',nprojection)
# Frozen operational source only. Exclude emitted environment values and all mutable/new profiling drafts.
pins=load(OPS/'operational-pins.json');put(OPS/'operational-pins.json','operational-source/operational-pins.json')
for name,h in pins['files'].items():
 f=OPS/name;assert sha(f)==h
 if f.suffix in['.py','.targets']:put(f,Path('operational-source')/name)
for name in ['source-qualification-binding.json','native-reference-case-set.json','entry-metadata-contract.json','ordinary-handoff.json','producer-transfer-interface.json','affected-inventory.json','private-directory-proof.json']:
 if(OPS/name).exists():put(OPS/name,Path('operational-source')/name)
# Strictly project private managed/cleanup/supervisor inputs. Never copy raw run/URLs/auth/env/CIDs/output.
run=load(LIVE/'managed/run.json');lc=run['launcherControls'];assert lc['total']==lc['passed']==142 and lc['failed']==lc['skipped']==0
projection={'diagnosticOnly':True,'featureAcceptance':False,'matrixCompleted':run['matrixCompleted'],'acceptedEquivalence':run['acceptedEquivalence'],'productionReadiness':run['productionReadiness'],'launcherControls':{k:lc[k]for k in ['total','passed','failed','skipped']},'preexistingResourcesPreserved':run['preexistingResourcesPreserved'],'privateMaterialRemoved':run['privateMaterialRemoved'],'cleanupEntries':len(run['cleanup']),'allCleanupRemovedAndAbsenceVerified':all(x['removed']and x['absenceVerified']for x in run['cleanup']),'outcomes':report['outcomes'],'rawRuntimeValuesCopied':False}
put(LIVE/'managed/run.json','managed/closed-run.fixed-projection.json',projection)
for name in ['supervisor.json','independent-cleanup-source-audit.json','closed-recorder-validation.json','supplemental-cleanup-qualification.json','execution-entry-counts.json']:
 x=load(LIVE/name);allowed=['diagnosticOnly','featureAcceptance','complete','matrixCompleted','singleAttempts','singleVerifiedPositives','multipleAttempts','multipleVerifiedPositives','namespaceContainersAbsent','privateRuntimeDirectoryAbsent','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved','fullLiveManifestSourceCount','fullLiveManifestHashMismatches','allIndependentRecordedProcessAndContainerAbsences','privateDomainReceiverPhaseOverlay','sdkOrdinary','controlPublished','launcherExitCode','failedMultipleBeforeAdmission','identityReadyBeforeAdmission','naturalCpuOverrideAbsent','noRuntimeSourceHook','noNewRuntimeEnvironmentOrConfigurationKeys','noCutoffOrTargetSignals','noTopologyCutoffOrSignals','safeForOrdinaryRestoration','noOwnedHostsStarted','nativeInvocationEntries','browserInvocationEntries'];v=scalars(x,allowed)
 if name=='independent-cleanup-source-audit.json':
  v.update(recordedIdentities=len(x['identities']),allRecordedIdentitiesAbsent=all(row['recordedIdentityAbsent']for row in x['identities']),recordedOwnedContainers=len(x['containers']),allRecordedOwnedContainersAbsent=all(row['absenceVerified']for row in x['containers']))
 put(LIVE/name,Path('managed')/name.replace('.json','.fixed-projection.json'),v)
manifest=load(LIVE/'managed/source-manifest.json');assert len(manifest)==17350 and all(re.fullmatch('[0-9a-f]{64}',h)for h in manifest.values());put(LIVE/'managed/source-manifest.json','managed/source-manifest.json.gz',compress=True)
# Retain original evidence digests even when privacy rules require projecting/omitting the original.
save('omitted-private-inputs.json',{'noRawControlContentsOrRuntimePayloads':True,'inputs':[{'originalPath':str(LIVE/name),'originalSha256':sha(LIVE/name),'handling':'fixed closed projection only'if name.endswith('run.json')else'original digest only; private output/coordinates omitted'}for name in ['managed/run.json','launcher-console.log','managed-command.json','early-runtime-identities.json','runtime-private-domain-proof.json']]})
save('outcome.json',{'qualifiedProbeOnly':True,'featureAcceptance':False,'scratchControlledProcesses':68,'scratchFocusedSemanticPairs':64,'scratchDiagnosticNativeExecutions':206,'scratchStrictMetadataRows':167479,'ordinaryNative1048NotInflated':True,'installedPrivateNativeDiagnosticExecutions':103,'privateBuildCommandsIncludingNativeArtifact':13,'privatePrimaryBuildCommands':12,'privateNativeTestArtifactBuildCommands':1,'ordinaryRestorationBuildCommands':13,'partialLiveCoverageComplete':False,'availableLiveRolesComplete':True,'missingRoles':['tenants'],'partialLiveRoles':counts,'partialLiveStrictRows':312,'liveOutcomes':report['outcomes'],'source59AndFull17350ManifestUnchangedAsAudited':True,'ownedCleanupAndOrdinaryRestorationComplete':True,'core3Dict':aggregate['coreSha256'],'onlyDomainServiceVariantInScratch':True,'sameOrdinarySdk6947PairInScratchAndLive':True,'wholeAndNestedIntervalsNotDisjoint':True,'noCausalOrHostDeadlineRepairConclusion':True,'packagingStartsNoRuntimeBuildProbeOrCleanup':True})
save('copy-provenance.json',provenance)
print(json.dumps({'artifacts':len(provenance),'scratchNative206':True,'partialRows':312,'missingTenants':True,'featureAcceptance':False}))
