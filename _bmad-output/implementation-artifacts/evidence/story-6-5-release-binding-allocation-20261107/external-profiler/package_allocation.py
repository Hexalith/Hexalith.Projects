from pathlib import Path
import shutil,json,hashlib,gzip,re,datetime,statistics
P=Path('/tmp/story65-release-binding-allocation-evidence-20261107');L=Path('/tmp/story65-release-binding-alloc-owner-live-20261107');W=Path('/tmp/story65-release-binding-alloc-owner-profile-20261107');T=Path('/tmp/story65-allocation-prep-20261107')
def sha(data):return hashlib.sha256(data).hexdigest()
def save(path,data):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(data,indent=2)+'\n')
def put(source,target):target=P/target;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
for file in sorted(L.iterdir()):
 if file.is_file():put(file,Path('managed')/file.name)
for suffix in ['-command.json','-console.log','-supervisor.json']:put(Path(str(L)+suffix),Path('managed')/('launcher'+suffix))
for file in sorted(W.iterdir()):
 if file.is_file()and file.name!='private-identity.json':put(file,Path('observer')/file.name)
profile=W/'profile';compressed=[]
for file in sorted(profile.iterdir()):
 if not file.is_file():continue
 assert file.name in ['identity-checks.json','attached.json','collection-receipt.json','allocation-samples.jsonl','allocation-summary.json','supervisor-receipt.json']
 if file.name in ['allocation-samples.jsonl','allocation-summary.json']:
  raw=file.read_bytes();packed=gzip.compress(raw,mtime=0);dest=P/'sanitized-allocation'/(file.name+'.gz');dest.parent.mkdir(exist_ok=True);dest.write_bytes(packed);assert gzip.decompress(packed)==raw
  compressed.append({'originalName':file.name,'originalBytes':len(raw),'originalSha256':sha(raw),'retainedName':str(dest.relative_to(P)),'gzipBytes':len(packed),'gzipSha256':sha(packed),'decompressedMatchesOriginal':True})
 else:put(file,Path('sanitized-allocation')/file.name)
save(P/'sanitized-compression.json',compressed)
for file in sorted(T.iterdir()):
 if file.is_file():put(file,Path('external-profiler')/file.name)
for folder in ['failed-end-report-watcher-prep','failed-maps-split-watcher-prep','failed-detached-dcp-tree-watcher-prep']:
 for file in (T/folder).iterdir():
  if file.is_file():put(file,Path('external-profiler')/folder/file.name)
for file in (T/'bin/Debug/net10.0').iterdir():
 if file.is_file():put(file,Path('external-profiler/debug-tool')/file.name)
for folder in ['/tmp/story65-release-binding-native-20261107']:
 for file in Path(folder).rglob('*'):
  if file.is_file():put(file,Path('native-qualification')/file.relative_to(folder))
receipt=Path('/tmp/story65-owner-tests/release-binding-command-receipt.json');put(receipt,Path('native-qualification/owner-command-receipt.json'))
owner=json.loads(receipt.read_text())
# Preserve owner-native referenced command/XML/log receipts exactly, as evidence rather than rerunning tests.
references=set()
def scan(value):
 if isinstance(value,str)and value.startswith('/tmp/'):
  path=Path(value)
  if path.is_file()and path.suffix in ['.xml','.log','.json','.txt','.cs','.csproj']:references.add(path)
 elif isinstance(value,dict):
  for child in value.values():scan(child)
 elif isinstance(value,list):
  for child in value:scan(child)
scan(owner)
for file in sorted(references):put(file,Path('native-qualification/owner-referenced')/str(file).lstrip('/'))
# Preserve failed preparatory runs/guards and source-generation refusal without raw runtime output.
for folder in ['story65-release-binding-allocation-actual-20261107','story65-release-binding-allocation-current-generation-20261107']:
 for file in Path('/tmp',folder).rglob('*'):
  if file.is_file()and file.suffix=='.json'and not file.name.startswith('private-'):put(file,Path('earlier-profile-guards')/folder/file.relative_to(Path('/tmp',folder)))
summary=json.loads((profile/'allocation-summary.json').read_text());collection=json.loads((profile/'collection-receipt.json').read_text());supervisor=json.loads((profile/'supervisor-receipt.json').read_text());run=json.loads((L/'run.json').read_text());watcher=json.loads((W/'watcher-receipt.json').read_text())
# Closed-schema privacy audit of all retained per-sample metadata, streamed to avoid unbounded memory.
allowed={'timeUtc','relativeMilliseconds','threadId','version','kind','type','sampledBytes','observedObjectBytes','frames'};sample_rows=0;frame_count=0;violations=0
for line in (profile/'allocation-samples.jsonl').open():
 row=json.loads(line);sample_rows+=1;assert set(row)==allowed and len(row['frames'])<=96;frame_count+=len(row['frames'])
 for name in [row['type'],*row['frames']]:
  if name=='[unresolved]':continue
  if any(char in name for char in ['/',chr(92),'"',"'"])or any(ord(c)<32 for c in name)or re.search(r'\b0x[0-9a-fA-F]+\b|\.(cs|dll|exe|so|pdb)(\b|:)',name,re.I):violations+=1
assert violations==0
save(P/'privacy-and-coverage.json',{'diagnosticOnly':True,'featureAcceptance':False,'samplesSchemaClosed':True,'retainedSampleRows':sample_rows,'retainedSampleFrames':frame_count,'metadataNameViolations':violations,'noObjectAddressesIpModulePathsOrOtherEventPayloadEmitted':True,'rawRuntimeInfo187MayContainCommandLine':'protected raw/ETLX were hashed and deleted, not retained in packet','rawAndAllDerivativesAbsent':supervisor['privateRuntimeMetadataRemoval']['allNonAllowlistedPrivateFilesAbsent'],'noSymlinkTargetsFollowedOrHashed':True,'sampleRowCapBytes':134217728,'sampleRowsCapped':summary['sanitizedRowsCapped'],'fullPopulationAggregateSamples':summary['samples'],'fullPopulationTypeGroups':len(summary['types']),'fullPopulationStackGroups':len(summary['stacks']),'typeOrStackUniqueKeyCapHit':any(x['type']=='[unique-key-cap]'for x in summary['types'])or any(x['typeAndStack']=='[unique-key-cap]'for x in summary['stacks']),'eventLoss':summary['traceEventsLost'],'conversionEventLoss':summary['lostOnConvert'],'missingType':summary['missingType'],'missingStack':summary['missingStack'],'unresolvedFrames':summary['unresolvedFrames'],'stacksTruncatedAt96Frames':summary['truncatedStacks'],'parserAndSupervisorIncompleteDueToIndividualRowCap':supervisor['incomplete'],'completeAggregateNotCompleteIndividualRowCoverage':True})
save(P/'tested-revisions-and-profile.json',{'canonicalRevisions':run['revisions'],'sourceDiffSha256':run['sourceDiffSha256'],'sourceManifestSha256':run['sourceManifestSha256'],'sourceMappingHash':run['sourceMappingHash'],'compilationProfile':run['compilationProfile'],'runtimeModes':run['runtimeModes'],'freshGenerationProof':watcher['freshGenerationProof'],'nativePriorGenerationNotClaimedByteEquivalent':True,'noPrivateApplicationOverlay':True,'ordinaryApplicationRebuildRequiredForProfilerRestoration':False})
rows=[json.loads(line)for line in(L/'browser-advisory-seed-readiness.jsonl').read_bytes().split(b'\n')[:-1]];multi=[r for r in rows if r['multiple']]
save(P/'allocation-findings.json',{'diagnosticOnly':True,'featureAcceptance':False,'collectionBeginUtc':collection['beginUtc'],'collectionEndUtc':collection['endUtc'],'collectionReason':collection['reason'],'durationStopDrainRundownSucceeded':collection['stopCompleted']and collection['drainCompleted'],'collectedBytes':collection['writtenBytes'],'sampledAllocationByteEstimates':summary['sampledBytes'],'sampleCount':summary['samples'],'topTypes':summary['types'][:12],'noExactPerTypeAllocationClaim':True,'individualRowsAndFrameDepthCapped':'See privacy-and-coverage.json; aggregate groups cover all54733 events.','conclusion':'Large allocation persists in this ordinary no-overlay Gateway; tracked image pool rent allocations were separately proven small. Full caller/stack analysis is needed to select a repair.','managedMultipleAttempts':len(multi),'managedMultipleVerifiedPositives':sum(r['positiveCandidatesVerified']is True for r in multi),'managedMultipleExternalMedianMs':statistics.median(r['elapsedMilliseconds']for r in multi),'fixtureElapsedIncludesLaterEvidenceReads':True,'nativeQualificationUniquePasses':986,'nativeQualificationOwner':360,'nativeQualificationProjects':626,'nativeQualificationDoesNotReplaceActualBrowserAcceptance':True})
(P/'README.md').write_text('''This temporary packet retains the first successful bounded ordinary-Gateway managed allocation capture. It is diagnostic evidence, not Story6.5 acceptance. The exact standard managed npm command ran ordinary Debug/source applications with DOTNET_PROCESSOR_COUNT unset, no private application source overlay and unchanged checks/order/deadline/expectations. It completed its full normal path: SINGLE passed attempt2, MULTIPLE failed30/30, one native seed failed, zero browser cases. No topology cutoff or target/launcher signal occurred. All20 recorded process identities,5 containers,namespace and private runtime directory are independently absent, with original preservation/sidecar/private/native-descendant flags verified.\n\nAll41 owner source snapshots and17183 live manifest inputs matched after cleanup. Captured sources, full canonical revisions, source mapping/compilation properties, command/environment/log and exact owned process/resource/config hash proofs are retained. The live source producer and mapped API Gateway copy share35b4925e4c6818911f564dc1123dcd9381d56a7c5616fb4373aa93e52affae00 with timing markers absent; this fresh ordinary generation is distinct from prior native321786... bytes. Source/behavior equivalence is evidenced by the frozen inputs/profile; byte equivalence is not claimed. Native qualification remains360 owner+626 Projects=986 unique passes, within its recorded selections/exclusions.\n\nThe watcher was identity READY during seeding05:57:33, before advisory results, after exact fresh AppHost/CLI ownership, source resource graph/DCP wrapper, same private config/XDG/AppID, actual mapped Gateway and all source checks. It attached only after a genuine MULTIPLE failure with no genuine positive and at least3 remaining windows. The one explicit provider was Microsoft-Windows-DotNETRuntime keyword0x1/Verbose5, stackwalk and rundown enabled,64MiB runtime buffer,8second duration with finite256MiB file cap and CPU/RSS/time safeguards. Official pinned manifest/client/reader source and tool Debug build/commands/dependencies are retained under external-profiler/. No heap dump, forced GC, network/logging/body provider or authority result/token caching occurred.\n\nCollection ran05:59:55.1372142Z–06:00:03.4709052Z and stopped normally with rundown/drain complete,45,227,418 bytes,zero file-cap/CPU/RSS guard hit. It parsed54,733 GCAllocationTick samples totaling6,897,291,872 sampled allocation-byte estimates. Complete510 type and3004 stack groups cover every sample, with zero event loss, missing type or missing stack and no unique-group cap. Individual JSONL rows reached the128MiB output cap at12,820 rows; later individual rows are omitted.25,607 stacks reach the96-frame limit and4,355 frames are unresolved/fixed-labelled. Parser/supervisor exit2/incomplete is retained honestly for that row cap; it does not mean the full-population type/stack aggregates were omitted. Allocation weights are sampled estimates, physical stacks are not logical async roots, and profiling is not ordinary latency proof.\n\nByte[] contributes2,881,200,096 sampled bytes and String1,984,906,232; full callers must be inspected before assigning a repair. This confirms large allocation exists without the previous timing overlay. Separate image-pool evidence ruled out tracked inner Rent calls as its main source. No unsupported pure transport/actor/GC-pressure cause or measured savings is claimed.\n\nSanitized metadata is byte-preservingly gzip-compressed under sanitized-allocation/ with original/compressed SHA256 and decompression proof. The CLR manifest warns event187 can include CommandLine/RuntimeDllPath: raw nettrace and ETLX stayed0700/0600, were hashed then deleted after parsing, all nonallowlisted derivatives/empty directories were removed without following symlink targets, and absence is verified. No raw activation, TrustProof, credential, object address, IP, module/source path or unrelated event payload is in allocation output. Previous missed-window/SDK-generation refusals and pre-attach failed recovery remain separately truthful. Earlier root evidence packets were unchanged. No production source/spec/Git change or application binary restoration was required by this external profiler. Parent may implement against captured source snapshots after the cleanup/source release.\n''')
files=sorted(file for file in P.rglob('*')if file.is_file()and file.name!='SHA256SUMS');(P/'SHA256SUMS').write_text(''.join(f'{sha(file.read_bytes())}  {file.relative_to(P)}\n'for file in files))
for line in(P/'SHA256SUMS').read_text().splitlines():digest,name=line.split('  ',1);assert sha((P/name).read_bytes())==digest
print(json.dumps({'packet':str(P),'checksumEntries':len(files),'checksumsSha256':sha((P/'SHA256SUMS').read_bytes()),'privacyViolations':violations,'allOwnedCleanupVerified':True,'featureAcceptance':False},indent=2))
