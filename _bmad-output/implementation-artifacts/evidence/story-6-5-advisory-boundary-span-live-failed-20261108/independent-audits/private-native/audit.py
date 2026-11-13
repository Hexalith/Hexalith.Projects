"""Finalized private-build/native metadata audit; no active managed/counter reads."""
from pathlib import Path
import collections,datetime,hashlib,json,xml.etree.ElementTree as ET
P=Path('/tmp/story65-advisory-boundary-span-live-prep-20261108');S=Path('/tmp/story65-advisory-boundary-span-prep-20261108');ROOT=Path('/home/administrator/projects/hexalith/projects');O=Path(__file__).parent;N=P/'native-private-139-giuq26ti';bound={}
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def uniq(pairs):
 d={}
 for k,v in pairs:assert k not in d;d[k]=v
 return d
def load(p):
 p=Path(p);bound[str(p)]=sha(p);return json.loads(p.read_bytes(),object_pairs_hook=uniq)
pins=load(P/'operational-pins.json');assert bound[str(P/'operational-pins.json')]=='9442f631f5732bcfb1a60eea11f1ae5bbfc59381c9cee942458345128309949c'and pins['executionReleased']and pins['sourceQualificationReady']
for group,base in [('files',P),('externalFiles',None)]:
 for n,h in pins[group].items():assert sha(Path(n)if base is None else base/n)==h
assert len(pins['files'])==25 and len(pins['externalFiles'])==160 and pins['mutableBinaryExternalPins']is False
work=Path(load(P/'latest-private-attempt.json')['attemptDirectory']);assert work.parent==P
r=load(work/'receipt.json');assert r['phase']=='private'and r['completed']and r['source65Match']and r['primaryBuilds']==10 and r['nativeOrdinaryBuilds']==0
assert load(work/'attempt-closed.json')['completed']is True
commands=load(work/'commands.json');assert len(commands)==10
primary=['service-defaults-producer','eventstore','tenants','projects','projects-workers','folders','folders-workers','folders-eventstore','apphost','integration'];inv=load(P/'affected-inventory.json')
required=['-t:Rebuild','-m:1','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-p:BuildProjectReferences=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true']
for name,c in zip(primary,commands):
 assert c['exitCode']==0 and c['command'][:3]==['dotnet','build',str(ROOT/inv['projects'][name])]and all(x in c['command']for x in required)and'-p:Story65TerminalPrivate=true'in c['command']
 assert sha(work/c['log'])==c['logSha256']
mapping=load(work/'mapping-receipt.json');mp=load(P/'mapping-pins.json');assert mapping['emittedEnvironment']==mp['environment']and len(mp['environment'])==7 and mapping['allSevenEmittedValuesRetained']and mapping['oneEffectiveAfterSdkPropertyExtendedOnlyForProjectsServer']and mapping['originalFinalMappingFirstObserverLast']and mapping['buildCpu2Only']
for p,h in mapping['mappingFilesUnchanged'].items():assert sha(p)==h
profile=load(work/'evaluated-eight-property-profile.json');assert profile['matchesCurrentStandardManagedProfile']and profile['profile']==load(P/'expected-managed-profile.json')['compilationProfile']
assert len(profile['profile'])==10
proof=load(work/'actual-projects-producer-compiled-proof.json');compiler=load(work/'actual-advisory-compiler-inputs.json');assert proof['private']and proof['sevenPrivateSourcesObserved']and not proof['originalOnlyObserved']and proof['observedInputsSha256']==bound[str(work/'actual-advisory-compiler-inputs.json')]
assert proof['producerSha256']==r['producerSha256']and proof['producerPdbSha256']==r['producerPdbSha256']
assert compiler['beforeCoreCompileNotTaskInterception']and compiler['insidePinnedSdkCscTransformsNotClaimedCaptured']
observed=compiler['observedInputs'];assert compiler['observedCount']==len(observed)
counts=collections.Counter(x['kind']for x in observed);actual=[x['path']for x in observed if x['kind']=='Compile'];private_names=['ProjectAdvisoryQueryExecutor.private.cs','CaptureCounters.cs','AdvisoryWaveBootstrap.cs','AdvisoryTerminalCapture.cs','AdvisoryBoundary.cs','AdvisoryTermination.cs','AdvisoryExceptionKind.cs']
for n in private_names:
 f=S/n;rows=[x for x in observed if x['kind']=='Compile'and x['path']==str(f)];assert len(rows)==1 and sha(f)==rows[0]['sha256']
assert str(ROOT/'src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs')not in actual
assert sum(str(S)+'/'in x for x in actual)==7
for name,h in compiler['documentsSha256'].items():
 f=work/('story65-advisory-'+name+'.txt');assert sha(f)==h;bound[str(f)]=h
parsed=[]
for line in(work/'story65-advisory-compiler-inputs.txt').read_text(encoding='utf-8-sig').splitlines():
 parts=line.split('|');parsed.append((parts[0],parts[1],parts[2:]))
assert parsed==[(x['kind'],x['path'],x['metadata'])for x in observed]
pre=(work/'story65-advisory-precore-flags.txt').read_text(encoding='utf-8-sig').splitlines();assert all(x in pre for x in ['Snapshot|before-CoreCompile-not-Csc-interception','Configuration|Debug','Optimize|false','EnablePrivateCatalogInvocationProof|true','EnablePrivateOwnerSourceReadProof|true','PrivateAdvisoryCore|true'])
for line in(work/'story65-advisory-writable-paths.txt').read_text(encoding='utf-8-sig').splitlines():
 p=Path(line).resolve();assert p.is_relative_to(ROOT/'src/Hexalith.Projects.Server/bin/Debug')or p.is_relative_to(ROOT/'src/Hexalith.Projects.Server/obj/Debug')
n=load(N/'receipt.json');ref=load(P/'native-reference-case-set.json');assert sha(ref['referenceXml'])==ref['referenceXmlSha256']
assert n['passed']and n['caseCount']==139 and n['exactHistoricalCaseSetRetained']and n['notAdditionalUniqueCasesBeyondCurrent1076']and n['counterDirectoryEmpty']and n['nativeProcessAndDescendantsAbsent']
nb=n['build'];assert nb['exitCode']==0 and nb['allSevenMappingValues']==mp['environment']and all(x in nb['command']for x in required)and nb['noOverlayHookOnTestBuild']and nb['sameTwoProofFlags']
assert sha(N/'build.log')==nb['logSha256']
before=n['privateServerBefore'];assert before['producerSha256']==before['testCopySha256']==proof['producerSha256']and before['producerPdbSha256']==before['testCopyPdbSha256']==proof['producerPdbSha256']and before['privateMarkersPresent']
nv=n['native'];identity=n['ownedNativeIdentity'];assert identity==nv['identity']==load(N/'native-identity.json')and identity['pid']==identity['processGroup']==identity['processSession']and str(identity['startTicks']).isdigit()
assert nv['exitCode']==n['ownedNativeExitCode']==0 and nv['privateServerObservedMapped']and nv['mappedObservationRequired']and nv['acceptedMappedAliases']==['test-copy']and nv['processAndDescendantsAbsent']and nv['remainingOwnedGroupIdentities']==[]and not nv['targetOrGroupSignalsSent']
assert sha(N/'native.log')==nv['logSha256']
xml=N/'native-139.xml';assert sha(xml)==nv['xmlSha256'];bound[str(xml)]=sha(xml);raw=xml.read_bytes();assert b'<!DOCTYPE'not in raw and b'<!ENTITY'not in raw
x=ET.fromstring(raw);tests=list(x.iter('test'));actual=[(v.get('type'),v.get('method'),v.get('name'))for v in tests];want=[tuple(v)for v in ref['canonicalCaseSet']]
assert len(actual)==len(set(actual))==len(want)==139 and set(actual)==set(want)and all(v.get('result')=='Pass'for v in tests)
assemblies=x.findall('assembly');assert len(assemblies)==1 and int(assemblies[0].get('passed'))==139 and all(int(assemblies[0].get(k,'0'))==0 for k in ['errors','failed','skipped','not-run'])
for f,h in ref['sourceFilesSha256'].items():assert sha(ROOT/f)==h
sdk=n['ordinarySdkBefore'];assert sdk==n['ordinarySdkAfter']==r['ordinarySdkProof']and sdk['producerSha256']=='873dbb74ee259d91c99ea2804e5801134c993abb4ef7806e79f242e5d23faf96'and sdk['sdkCensusCount']==137 and sdk['allEightOrdinarySdkAndContractsCopiesCoherent']and sdk['allSdkCensusMarkerFree']and len(sdk['copies'])==8
assert all(v['contractsSha256']=='f1cf82fe57d5ae0037772d3ccea77766a141cd49ec66c2079c3c8b046c8089df'for v in sdk['copies'])
source=load(Path(load(P/'source-qualification-binding.json')['currentSourceControls']))['sourceFilesSha256'];assert len(source)==65
for f,h in source.items():assert sha(ROOT/f)==h
report={'passed':True,'diagnosticOnly':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'auditSourceSha256':sha(__file__),'bindingsSha256':bound,'privateBuild':{'primaryBuilds':10,'allClosedExitZero':True,'sevenOriginalMappingValuesRetained':True,'effectiveAfterSdkExtendedOnlyForProjectsServer':True,'eightPropertyProfiles':10,'source65Current':True,'compilerSnapshotKinds':dict(counts),'compilerObservedInputs':len(observed),'exactPrivateCompileSources':7,'ordinaryExecutorAbsentFromPrivateCompile':True,'writablePathsOwnedDebugOnly':True,'preCoreCompileSnapshotNotActualCscInterception':True,'privateProducerSha256':proof['producerSha256'],'privateProducerPdbSha256':proof['producerPdbSha256']},'native':{'cases':139,'exactReferenceCaseSet':True,'pass':139,'errors':0,'failed':0,'skipped':0,'notRun':0,'mappedPrivateAlias':'test-copy','samePidStartAndPrivateHashRequiredByPinnedObserver':True,'producerTestCopyDllPdbMatchedAtNativeBoundary':True,'ownedIdentity':identity,'closedReceiptGroupAndSessionAbsenceVerified':True,'counterDirectoryEmptyAtNativeBoundary':True,'targetSignalsSent':False,'sdk137OrdinaryAtNativeBoundary':True,'eightSdkContractsPairsOrdinaryAtNativeBoundary':True,'native139IsRepeatedSubsetOf1076NotAdditionalTests':True},'limits':['Only finalized private-build/native artifacts and frozen source/pin inputs were read; no active managed evidence, private counter directory or application process was inspected.','Private DLL/PDB hashes are historical native/build-boundary observations. Ordinary restoration will replace them; no future/current mutable live-binary equality guarantee is made.','Natural native runtime processor count is supported by the pinned launcher removing its build-only override; this native receipt does not independently measure Environment.ProcessorCount. Earlier scratch runtime facts remain separate.','Compiler item/flag snapshots are before CoreCompile and after prior pinned targets, not Csc task interception or a complete claim about SDK-internal Csc transforms.','Actual managed result, terminal rows, physical writer proof, full cleanup and forced ordinary restoration remain pending and are not qualified by this native gate.']}
(O/'independent-private-native-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'report':str(O/'independent-private-native-audit.json'),'sha256':sha(O/'independent-private-native-audit.json'),'privateBuilds':10,'compilerInputs':len(observed),'kinds':dict(counts),'nativeCases':139}))
