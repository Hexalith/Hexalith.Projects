from pathlib import Path
import argparse,atexit,datetime,json,subprocess,tempfile
from ops_common import *
p=argparse.ArgumentParser();p.add_argument('--phase',choices=['private','ordinary'],required=True);p.add_argument('--execute',action='store_true');p.add_argument('--pins-sha256');args=p.parse_args()
inv=json.loads((PREP/'affected-inventory.json').read_text());projects=inv['projects']
if not args.execute:
 print(json.dumps({'preparationOnly':True,'phase':args.phase,'primaryOrder':PRIMARY,'ordinaryAddsNativeTests':True,'forcedOrdinaryMinimum':11,'dynamicAffectedProjectServerUnion':True}));raise SystemExit()
operational_pins(args.pins_sha256);sources()
work=Path(tempfile.mkdtemp(prefix='operational-'+args.phase+'-',dir=PREP))
if args.phase=='private':save(PREP/'latest-private-attempt.json',{'attemptDirectory':str(work),'ordinaryRestoreRequiredEvenIfPrivateBuildFails':True})
before=census();save(work/'before-census.json',before)
if args.phase=='private':
 private_directory(require_empty=True);ordinary_sdk_proof();independent_dependencies();app_marker_proof(False)
env=clean_environment();env.update(DOTNET_PROCESSOR_COUNT='2',HexalithBuildsRoot=str(ROOT/'references/Hexalith.Builds'));mapping=json.loads((PREP/'mapping-pins.json').read_text())
assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(path)==h for path,h in mapping['files'].items())
assert set(mapping['environment'])==MAPPING and mapping['environment']['Configuration']=='Debug';env.update(mapping['environment'])
save(work/'mapping-receipt.json',{'allSevenEmittedValuesRetained':True,'emittedEnvironment':mapping['environment'],'oneEffectiveAfterSdkPropertyExtendedOnlyForProjectsServer':True,'originalFinalMappingFirstObserverLast':True,'buildCpu2Only':True,'mappingFilesUnchanged':mapping['files']})
commands=[];completed=False
@atexit.register
def end():
 try:save(work/'after-census.json',census());save(work/'attempt-closed.json',{'completed':completed,'ordinaryRestoreRequired':args.phase=='private'})
 except Exception:pass

def compiler_proof(private):
 root=PRODUCER/'obj/Debug/net10.0';names=['compiler-inputs','writable-paths','precore-flags','postcore-flags'];documents={}
 for n in names:
  f=root/('story65-advisory-'+n+'.txt');assert f.is_file();data=f.read_bytes();(work/f.name).write_bytes(data);documents[n]=data.decode('utf-8-sig')
 observed=[]
 for line in documents['compiler-inputs'].splitlines():
  values=line.split('|');assert len(values)>=2;path=Path(values[1]);assert path.is_absolute()and path.is_file()
  observed.append({'kind':values[0],'path':str(path),'sha256':sha(path),'metadata':values[2:]})
 required=[SOURCE_PREP/n for n in ['ProjectAdvisoryQueryExecutor.private.cs','CaptureCounters.cs','AdvisoryWaveBootstrap.cs','AdvisoryTerminalCapture.cs','AdvisoryBoundary.cs','AdvisoryTermination.cs','AdvisoryExceptionKind.cs']]
 actual=[r['path']for r in observed if r['kind']=='Compile'];original=str(PRODUCER/'Queries/ProjectAdvisoryQueryExecutor.cs')
 if private:assert original not in actual and all(actual.count(str(f))==1 for f in required)
 else:assert actual.count(original)==1 and all(str(f)not in actual for f in required)
 for line in documents['writable-paths'].splitlines():
  x=Path(line).resolve();assert x.is_relative_to(PRODUCER/'bin/Debug')or x.is_relative_to(PRODUCER/'obj/Debug')
 save(work/'actual-advisory-compiler-inputs.json',{'observedInputs':observed,'observedCount':len(observed),'beforeCoreCompileNotTaskInterception':True,'insidePinnedSdkCscTransformsNotClaimedCaptured':True,'documentsSha256':{n:sha(work/('story65-advisory-'+n+'.txt'))for n in names},'privateSevenExactSourcesObserved':private,'ordinaryExactOriginalObserved':not private})
 producer=ordinary_expected('full')
 save(work/'actual-projects-producer-compiled-proof.json',{'private':private,'producerSha256':sha(producer),'producerPdbSha256':sha(ordinary_expected('pdb')),'observedInputsSha256':sha(work/'actual-advisory-compiler-inputs.json'),'sevenPrivateSourcesObserved':private,'originalOnlyObserved':not private,'noBinaryEquivalenceAssumed':True})

def rebuild(relative,label):
 assert relative in projects.values()
 command=['dotnet','build',str(ROOT/relative),'-c','Debug','-t:Rebuild','-m:1','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-p:BuildProjectReferences=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-p:CustomBeforeMicrosoftCommonTargets='+str(PREP/'advisory-terminal.targets'),'-p:Story65TerminalPrivate='+str(args.phase=='private').lower()]
 if relative==projects['integration']:command+=['-p:SupportedProjectReadDistributed=true']
 log=work/(label+'.log');started=datetime.datetime.now(datetime.timezone.utc).isoformat()
 with log.open('wb')as stream:r=subprocess.run(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 commands.append({'command':command,'cwd':str(ROOT),'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':r.returncode,'log':log.name,'logSha256':sha(log)});save(work/'commands.json',commands)
 assert r.returncode==0,'Explicit source-mapped build failed; exact log retained'
 if relative==projects['projects']:compiler_proof(args.phase=='private')

for index,alias in enumerate(PRIMARY):rebuild(projects[alias],str(index+1)+'-'+alias+'-force-refsfalse')
if args.phase=='ordinary':rebuild(projects['native-tests'],'11-native-tests-force-refsfalse')
# Same original standard-managed profile, evaluated with the same existing flags.
expected=json.loads((PREP/'expected-managed-profile.json').read_text())['compilationProfile'];profile={}
props=['Configuration','Optimize','DefineConstants','DebugType','UseHexalithProjectReferences','UseNuGetDeps','EnablePrivateCatalogInvocationProof','EnablePrivateOwnerSourceReadProof']
for relative in expected:
 command=['dotnet','msbuild',str(ROOT/relative),'-nologo','-p:Configuration=Debug','-p:SupportedProjectReadDistributed=true','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-getProperty:'+','.join(props)]
 r=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True);assert r.returncode==0
 profile[relative]=json.loads(r.stdout)['Properties'];assert profile[relative]==expected[relative]
save(work/'evaluated-eight-property-profile.json',{'matchesCurrentStandardManagedProfile':True,'profile':profile,'inactiveFoldersEventstoreAndNativeBuildFlagsExplicitButNotInventedStandardProfileRows':True})
if args.phase=='ordinary':
 pointer=PREP/'latest-private-attempt.json';pw=None
 if pointer.exists():pw=Path(json.loads(pointer.read_text())['attemptDirectory']);assert pw.parent==PREP
 initial_rows=json.loads((pw/'before-census.json').read_text())if pw is not None and(pw/'before-census.json').exists()else inv['knownServerCopies']
 initial={r['path']:r for r in initial_rows}
 private_after=json.loads((pw/'after-census.json').read_text())if pw is not None and(pw/'after-census.json').exists()else[]
 save(work/'restoration-baseline-source.json',{'privateBeforeCensusAvailable':pw is not None and(pw/'before-census.json').exists(),'freshInventoryFallbackUsed':pw is None or not(pw/'before-census.json').exists()})
 after_primary=census();observed=initial_rows+private_after+before+after_primary
 affected={r['path']for r in observed if r['path']not in initial or r['privateMarker']or r['sha256']!=initial[r['path']]['sha256']}
 affected|={r['path']for r in inv['knownServerCopies']};coordinates={r['path']:r for r in observed}
 expected_hashes={k:sha(ordinary_expected(k))for k in ['full','pdb','ref','refint']};remaining={r['path']:r for r in after_primary}
 stale=[coordinates[path]for path in affected if path not in remaining or remaining[path]['privateMarker']or remaining[path]['sha256']!=expected_hashes[remaining[path]['kind']]]
 for index,project in enumerate(sorted({r['project']for r in stale})):
  assert project in projects.values();rebuild(project,'remaining-affected-carrier-'+str(index))
 after=census()
 # Include NEW ordinary/fallback paths, not just paths seen during the private phase.
 for row in after:
  if row['path']not in initial or row['privateMarker']or row['sha256']!=initial[row['path']]['sha256']:affected.add(row['path'])
 assert affected<={r['path']for r in after}
 for row in after:
  assert not row['privateMarker']
  if row['path']in affected:assert row['sha256']==expected_hashes[row['kind']]
  else:assert row['sha256']==initial[row['path']]['sha256']
 save(work/'affected-paths-union.json',{'paths':sorted(affected),'affected':len(affected),'unrelatedExactBaseline':len(after)-len(affected),'includesPrivateOrdinaryAndNewAfterFallbackPaths':True})
else:
 after=census();producer=ordinary_expected('full');assert marker(producer)and marker(ordinary_expected('pdb'))
 for alias in ['projects','integration']:
  f=(ROOT/projects[alias]).parent/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');assert sha(f)==sha(producer)and sha(f.with_suffix('.pdb'))==sha(ordinary_expected('pdb'))
save(work/'after-census.json',after);sources();sdk=ordinary_sdk_proof();dependencies=independent_dependencies();apps=app_marker_proof(args.phase=='private')
save(work/'receipt.json',{'diagnosticOnly':True,'featureAcceptance':False,'phase':args.phase,'completed':True,'source65Match':True,'producerSha256':sha(ordinary_expected('full')),'producerPdbSha256':sha(ordinary_expected('pdb')),'censusCopies':len(after),'allPrivateMarkersAbsent':not any(r['privateMarker']for r in after),'ordinarySdkProof':sdk,'independentDependencies':dependencies,'applicationMarkerProof':apps,'primaryBuilds':10,'nativeOrdinaryBuilds':1 if args.phase=='ordinary'else 0})
completed=True
print(json.dumps({'phase':args.phase,'completed':True,'censusCopies':len(after)}))
