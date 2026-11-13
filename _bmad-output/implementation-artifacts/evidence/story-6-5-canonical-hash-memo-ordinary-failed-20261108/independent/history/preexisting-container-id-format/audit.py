"""Independent read-only audit of one closed ordinary run; writes only allowlisted TEMP metadata."""
from pathlib import Path
import ast,hashlib,json,os,re,stat,subprocess,xml.etree.ElementTree as ET
ROOT=Path('/home/administrator/projects/hexalith/projects')
LIVE=Path('/tmp/story65-canonical-hash-acceptance-live-20261108')
OUT=Path('/tmp/story65-canonical-hash-acceptance-independent-audit-20261108')
BINDING=Path('/tmp/story65-canonical-hash-acceptance-release-census-corrected-20261108.json')
MARKERS=['Story65Timing','Story65PrivateDiagnostic','Hexalith.PrivateOwnerTiming','PrivateFocusedTiming','TimingHexalith','Hexalith.PrivateCaptureCounter','InvocationTransportBootstrap','GatewayControllerBootstrap','ReceiverPhaseBootstrap','CaptureCounters.cs']
MAPPING={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'}

def pairs(items):
 d={}
 for k,v in items:assert k not in d,'duplicate-json-key';d[k]=v
 return d
def read(p):return json.loads(Path(p).read_bytes(),object_pairs_hook=pairs)
def sha(p):
 fd=os.open(p,os.O_RDONLY|os.O_NOFOLLOW|os.O_CLOEXEC|os.O_NONBLOCK)
 with os.fdopen(fd,'rb') as stream:
  assert stat.S_ISREG(os.fstat(stream.fileno()).st_mode),'regular-input'
  return hashlib.file_digest(stream,'sha256').hexdigest()
def hashbytes(b):return hashlib.sha256(b).hexdigest()
def coordinate(name):
 p=Path(name);assert not p.is_absolute() and '..' not in p.parts and (ROOT/p).resolve().is_relative_to(ROOT),'contained-source-coordinate'
 return ROOT/p
def proc(pid):
 try:
  values=Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split()
  return {'state':values[0],'parent':int(values[1]),'group':int(values[2]),'start':int(values[19])}
 except (FileNotFoundError,ProcessLookupError):return None
def run(args,cwd=None):return subprocess.run(args,cwd=cwd,capture_output=True,timeout=15)
def save(name,value):
 p=OUT/name;assert not p.exists(),p;p.write_text(json.dumps(value,indent=2)+'\n')

def main():
 r=read(LIVE/'run.json');status=read(Path(str(LIVE)+'-supervisor.json'));command=read(Path(str(LIVE)+'-command.json'));binding=read(BINDING)
 assert status['phase']=='ordinary-managed-complete' and status['managedExitCode']==1
 assert command['command']==['npm','--prefix','tests/e2e','run','test:live:managed'] and command['runtimeCpuOverrideAbsent'] is True
 assert status['ordinaryAcceptanceOnly'] is True and all(status[k] is False for k in ['profilerEnabled','watcherEnabled','privateDiagnosticOverride','cutoffEnabled','targetSignalsSent'])
 assert command['noLauncherReplacementNoBuildCutoffOrTargetSignals'] is True
 assert status['releaseSha256']==sha(BINDING)
 # Source manifest uses the original launcher's compact sorted-object digest, not the pretty file digest.
 manifest=read(LIVE/'source-manifest.json');assert isinstance(manifest,dict) and all(re.fullmatch('[0-9a-f]{64}',v) for v in manifest.values())
 assert hashbytes(json.dumps(manifest,sort_keys=True).encode())==r['sourceManifestSha256']
 full_mismatches=[p for p,h in manifest.items() if sha(coordinate(p))!=h]
 assert not full_mismatches
 controls=read(binding['sourceControls']['path'])['sourceFilesSha256'];assert len(controls)==65 and sha(binding['sourceControls']['path'])==binding['sourceControls']['sha256']
 assert all(sha(coordinate(p))==h for p,h in controls.items())
 # Public compiler metadata and finite emitted mapping proof; never persist raw environment/path values.
 mappings=r['sourceMappingEnvironment'];assert set(mappings)==MAPPING and mappings['Configuration']=='Debug'
 assert mappings['HEXALITH_SOURCE_MAPPING_HASH']==r['sourceMappingHash'] and re.fullmatch('[0-9a-f]{64}',r['sourceMappingHash'])
 profile=r['compilationProfile'];assert len(profile)==10
 profile_rows=[]
 for name,v in profile.items():
  expected={'Configuration':'Debug','Optimize':'false','UseHexalithProjectReferences':'true','UseNuGetDeps':'false','EnablePrivateCatalogInvocationProof':'true','EnablePrivateOwnerSourceReadProof':'true'}
  assert all(v[k]==x for k,x in expected.items())
  assert 'DEBUG' in v['DefineConstants'].split(';')
  profile_rows.append({'project':name,**expected,'debugConstantObserved':True,'debugType':v['DebugType']})
 # All selected new producer and active/native consumer pairs remain exact.
 producers=binding['producerFilesSha256'];assert len(producers)==8 and all(sha(p)==h for p,h in producers.items())
 before=read(Path(str(LIVE)+'-preflight.json'))['generations'];post=read(Path(str(LIVE)+'-postflight-generations.json'))
 assert before['producerFilesSha256']==post['producerFilesSha256']==producers
 copies=[]
 for directory in binding['applicationBinaryDirectories']:
  for module in ['Hexalith.EventStore.Contracts','Hexalith.EventStore.ServiceDefaults']:
   for extension in ['dll','pdb']:
    p=Path(directory)/(module+'.'+extension)
    producer=ROOT/'references/Hexalith.EventStore/src'/module/'bin/Debug/net10.0'/p.name
    digest=sha(p);assert digest==producers[str(producer)]
    copies.append({'path':str(p),'producer':str(producer),'sha256':digest})
 assert len(copies)==32 and len(binding['applicationBinaryDirectories'])==8
 baseline=binding['currentApp147Sha256'];assert len(baseline)==147
 actual={p:sha(p) for p in baseline};assert actual==post['currentApp147Sha256']
 all_dll=set()
 for d in binding['applicationBinaryDirectories']:all_dll.update(str(p) for p in Path(d).glob('Hexalith.*.dll'))
 extra=all_dll-set(baseline);assert len(extra)==2 and set(post['additionalHexalithAssemblyFilesSha256'])==extra
 for p in all_dll:
  data=Path(p).read_bytes();assert not any(m.encode() in data or m.encode('utf-16-le') in data for m in MARKERS)
 assert all(sha(p)==post['additionalHexalithAssemblyFilesSha256'][p] for p in extra)
 changes={p:h for p,h in actual.items() if baseline[p]!=h};assert len(changes)==6
 assert set(changes)==set(post['permittedFreshStandardBuildGenerationDifferences'])
 graph={}
 for row in r['liveHostSourceReferences'].values():
  assert row['allSourceProjects'] is True
  for name,project in row['sourceProjects'].items():
   if name in graph:assert graph[name]==project
   graph[name]=project
 for project in graph.values():assert str(Path(project).relative_to(ROOT)) in manifest
 deltas=[]
 for path,digest in changes.items():
  owners=[]
  for name,project in graph.items():
   producer=Path(project).parent/'bin/Debug/net10.0'/Path(path).name
   if producer.exists() and sha(producer)==digest:owners.append({'sourceProject':project,'producer':str(producer),'sha256':digest})
  assert owners,'changed-output-current-source-producer-not-found'
  deltas.append({'path':path,'priorSha256':baseline[path],'currentSha256':digest,'matchingCurrentSourceProducers':owners})
 # Source-selected entry hashes are recorded by the original owned-process observer. No post-exit map claim.
 tree=ast.parse((ROOT/'tools/qualification/run_supported_project_reads.py').read_text())
 expression=next(node.value for node in tree.body if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='RUNTIME_PROJECTS' for t in node.targets))
 assert isinstance(expression,ast.Call) and isinstance(expression.func,ast.Name) and expression.func.id=='dict' and len(expression.args)==1
 zipped=expression.args[0];assert isinstance(zipped,ast.Call) and isinstance(zipped.func,ast.Name) and zipped.func.id=='zip' and len(zipped.args)==2
 runtime_projects=dict(zip(ast.literal_eval(zipped.args[0]),ast.literal_eval(zipped.args[1]),strict=True))
 assert set(r['runtimeModes'])==set(runtime_projects) and len(runtime_projects)==6
 runtime_flags=next(ast.literal_eval(node.value) for node in tree.body if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='RUNTIME_FLAGS' for t in node.targets))
 runtime_rows=[]
 for role,v in r['runtimeModes'].items():
  project=ROOT/runtime_projects[role];entry=project.parent/'bin/Debug/net10.0'/(project.stem+'.dll')
  assert sha(entry)==v['assemblySha256'] and v['environment']=='Development'
  assert all(v['options'][key]==value for key,value in runtime_flags.items())
  assert v['options']['EventStore__RouteCatalog__ReuseHeldMetadataDispatch']==('true' if role=='folders' else 'false')
  runtime_rows.append({'role':role,'sourceProject':str(project),'entrySha256':v['assemblySha256'],'recordedOwnedStartAndOptionProof':True,'postExitLiveMapsNotReobserved':True})
 # Fresh process identity and complete seed process-group absence, without argv/environment reads or signals.
 ids=[]
 for role in ['appHost','cli']:
  s=r['ownedAppHostSession'];ids.append({'role':role,'pid':s[role+'Pid'],'startTicks':int(s[role+'StartTicks'])})
 for role,row in r['runtimeModes'].items():
  for label,p,s in [('entry','pid','startTicks'),('wrapper','wrapperPid','wrapperStartTicks')]:ids.append({'role':role+'-'+label,'pid':row[p],'startTicks':int(row[s])})
 for index,row in enumerate(r['discovery']['ownedSidecars']):ids.append({'role':'sidecar-'+str(index+1),'pid':row['pid'],'startTicks':int(row['startTicks'])})
 ids.append({'role':'managed-launcher','pid':command['launcherPid'],'startTicks':int(command['launcherStartTicks'])});assert len(ids)==21
 for row in ids:
  now=proc(row['pid']);row['sameRecordedIdentityPresent']=now is not None and now['start']==row['startTicks'];assert not row['sameRecordedIdentityPresent']
 groups=r['testProcessCleanup'];assert len(groups)==1 and all(row['descendantsAbsent'] is True for row in groups)
 members=[]
 for p in Path('/proc').iterdir():
  if p.name.isdigit():
   now=proc(int(p.name))
   if now is not None and now['group'] in {row['groupId'] for row in groups}:members.append({'pid':int(p.name),'startTicks':now['start'],'state':now['state']})
 assert not members
 # Docker reads are exact recorded owned IDs and their exact namespace; no start/remove or raw outputs.
 containers=[]
 for row in r['cleanup']:
  assert row['absenceVerified'] is True
  if 'containerId' not in row:continue
  cid=row['containerId'];assert re.fullmatch('[0-9a-f]{64}',cid)
  result=run(['docker','inspect','--format','{{.Id}}',cid]);absent=result.returncode!=0 and b'no such object' in result.stderr.lower();assert absent
  containers.append({'ordinal':len(containers)+1,'recordedContainerIdSha256':hashbytes(cid.encode()),'absenceVerified':True})
 assert len(containers)==5
 oidc=next(row['resource'] for row in r['cleanup'] if row['resource'].endswith('-oidc'));namespace=oidc[:-5]
 result=run(['docker','ps','--all','--filter','label=hexalith.supported-reads='+namespace,'--format','{{.ID}}']);assert result.returncode==0 and not result.stdout.strip()
 result=run(['docker','ps','--all','--no-trunc','--format','{{.ID}}']);assert result.returncode==0
 current_containers=set(result.stdout.decode().split());assert set(r['preexistingResources']['containers']).issubset(current_containers)
 # Original private material and protected workspace facts, rechecked without configuration reads.
 private=set()
 for value in mappings.values():
  for parent in Path(value).parents:
   if parent.name.startswith('supported-project-reads-private-'):private.add(parent)
 assert len(private)==1 and all(not p.exists() for p in private)
 assert all(r[k] is True for k in ['ownedSidecarsAbsent','preexistingResourcesPreserved','privateMaterialRemoved'])
 protected=read(binding['protectedBaseline']['path']);assert sha(binding['protectedBaseline']['path'])==binding['protectedBaseline']['sha256']
 frozen=re.search(rb'<frozen-after-approval.*?</frozen-after-approval>',(ROOT/'_bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md').read_bytes(),re.S)
 assert frozen and hashbytes(frozen.group())==protected['frozen_sha256']
 assert sha(ROOT/'_bmad-output/implementation-artifacts/sprint-status.yaml')==protected['sprint_sha256']
 # Required read-only Git sanity inspection: all output stays in RAM, never publication material.
 for args in [['git','branch','--show-current'],['git','status','--porcelain'],['git','remote','-v'],['git','log','-3','--format=%H']]:assert run(args,ROOT).returncode==0
 result=run(['git','diff','--cached','--binary'],ROOT);assert result.returncode==0 and hashbytes(result.stdout)==protected['index_sha256']
 # Only closed scalar fields are retained from readiness; original details/owner states remain private.
 raw=(LIVE/'browser-advisory-seed-readiness.jsonl').read_bytes();assert raw.endswith(b'\n')
 readiness=[]
 fields=['multiple','attempt','elapsedMilliseconds','httpStatus','result','incomplete','candidateCount','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied']
 for line in raw.splitlines():
  row=json.loads(line,object_pairs_hook=pairs);assert all(k in row for k in fields)
  assert row['httpStatus']==200 and row['actualPublicGateway'] is True and row['nonBootstrapCredential'] is True and row['fabricatedProjectionWrite'] is False and row['runtimeAuthoritySupplied'] is False
  readiness.append({key:row[key] for key in fields})
 single=[x for x in readiness if x['multiple'] is False];multi=[x for x in readiness if x['multiple'] is True]
 assert len(single)==1 and single[0]['positiveCandidatesVerified'] is True and single[0]['candidateCount']==1
 assert len(multi)==30 and [x['attempt'] for x in multi]==list(range(1,31))
 assert all(x['result']=='None' and x['incomplete'] is True and x['candidateCount']==0 and x['positiveCandidatesVerified'] is False for x in multi)
 browser_commands=sum(any(Path(token).name in {'playwright','playwright.cmd'} for token in cmd if isinstance(token,str)) for cmd in r['commands'])
 assert browser_commands==0 and r['result']=='failed' and r['matrixCompleted'] is False
 xml=ET.parse(LIVE/'browser-seed.xml');cases=xml.findall('.//test');assert len(cases)==1 and cases[0].attrib['result']=='Fail'
 xml_projection={'sourceSha256':sha(LIVE/'browser-seed.xml'),'pass':0,'fail':1,'skip':0,'testDisplayNameSha256':hashbytes(cases[0].attrib['name'].encode()),'failureExceptionTypes':[item.attrib.get('exception-type') for item in xml.findall('.//failure')],'browserJourneyNotExecuted':True,'failureMessageStackAndOutputNotPersisted':True}
 save('safe-readiness.json',{'sourcePath':str(LIVE/'browser-advisory-seed-readiness.jsonl'),'sourceSha256':sha(LIVE/'browser-advisory-seed-readiness.jsonl'),'rows':readiness,'externalElapsedIncludesAdditionalEvidenceReads':True,'notRequestLatency':True})
 save('safe-seed-xml.json',xml_projection)
 save('source-generation-profile-proof.json',{'source65ReceiptSha256':sha(binding['sourceControls']['path']),'source65AllMatch':True,'fullManifestEntries':len(manifest),'fullManifestFileSha256':sha(LIVE/'source-manifest.json'),'fullManifestOriginalCompactDigestMatch':True,'fullManifestMismatches':0,'producer8Sha256':producers,'currentSdkAndContractsCopyRows':copies,'sourceGraphProjectCount':len(graph),'app147CurrentSha256':actual,'app147UnchangedCount':147-len(changes),'sixCurrentStandardSourceGenerationDeltas':deltas,'additional2MarkerCheckedFilesSha256':{p:sha(p) for p in sorted(extra)},'noPrivateMarkersAcross147Plus2':True,'sevenOriginalMappingKeys':sorted(MAPPING),'originalMappingHash':r['sourceMappingHash'],'compilationProfile10':profile_rows,'runtimeEntry6':runtime_rows,'historical1048ByteEqualityClaimed':False,'prior10And17ProvenanceLimitsRetained':True,'hashScopeEligibilityNotInferredFromRuntimeFlagsAlone':True})
 audit={'passed':True,'ordinaryAcceptanceFailed':True,'featureAcceptance':False,'standardManagedExitCode':1,'wrapperExitCodeReported':0,'singleAttempts':1,'singleVerifiedPositive':1,'multipleAttempts':30,'multipleVerifiedPositive':0,'allMultipleHttp200NoneIncompleteZeroCandidates':True,'allReadinessGenuineFlagsPresent':True,'seedXmlFailures':1,'browserCommands':0,'matrixCompleted':False,'browserPersistedPrivacyA11yAcceptanceNotReached':True,'source65AllMatch':True,'fullSourceManifestEntries':len(manifest),'fullSourceManifestMismatches':0,'producer8AndAll8SdkContractsActivePairsMatch':True,'app147MarkerCleanWith6SourceProducerMatchedDeltas':True,'additional2NotPartOf147MarkerChecked':True,'original7MappingsDebugSourceAnd2ProofFlagsPass':True,'noProfileWatcherOverlayCutoffOrSignals':True,'freshRecorded21IdentitiesAbsent':ids,'freshSeedGroupsAbsent':[{'groupId':x['groupId'],'allMembersAbsent':True} for x in groups],'exact5OwnedContainersAbsent':containers,'ownedNamespaceAbsent':True,'preexistingContainerSetPreserved':True,'preexistingAppHostsCount':len(r['preexistingResources']['apphosts']),'preexistingSidecarsCount':len(r['preexistingResources']['sidecars']),'originalPreservationAndPrivateRemovalFlagsTrue':True,'privateRuntimeDirectoryAbsent':True,'frozenSprintIndexPreserved':True,'failureCauseInferred':False,'rawRunReadinessPrivateObjectsEnvArgvLogsNotPublished':True,'originalEvidenceSha256':{'run.json':sha(LIVE/'run.json'),'browser-advisory-seed-readiness.jsonl':sha(LIVE/'browser-advisory-seed-readiness.jsonl'),'browser-seed.xml':sha(LIVE/'browser-seed.xml'),'source-manifest.json':sha(LIVE/'source-manifest.json')},'boundReleaseSha256':sha(BINDING),'auditSourceSha256':sha(Path(__file__))}
 save('independent-ordinary-acceptance-audit.json',audit)
 print(json.dumps({k:audit[k] for k in ['passed','ordinaryAcceptanceFailed','singleAttempts','singleVerifiedPositive','multipleAttempts','multipleVerifiedPositive','browserCommands','source65AllMatch','fullSourceManifestEntries','fullSourceManifestMismatches','producer8AndAll8SdkContractsActivePairsMatch','frozenSprintIndexPreserved']}))
if __name__=='__main__':main()
