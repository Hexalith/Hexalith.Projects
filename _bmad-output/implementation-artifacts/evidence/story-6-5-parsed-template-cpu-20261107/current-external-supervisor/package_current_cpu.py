from pathlib import Path
import datetime,gzip,hashlib,json,re,shutil,statistics

P=Path('/tmp/story65-parsed-template-cpu-evidence-20261107')
L=Path('/tmp/story65-parsed-template-cpu-owner-live-20261107')
W=Path('/tmp/story65-parsed-template-cpu-owner-profile-20261107')
T=Path('/tmp/story65-cpu-prep-20261107')
C=Path('/tmp/story65-parsed-template-cpu-prep-20261107')
N=Path('/tmp/story65-parsed-template-native-20261107')

def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def sha_bytes(data):return hashlib.sha256(data).hexdigest()
def put(source,relative):
    target=P/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
def save(relative,value):
    target=P/relative;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(value,indent=2)+'\n')

assert (P/'source-snapshot.json').is_file() and (P/'independent-cleanup-source-audit.json').is_file()
for file in L.iterdir():
    if file.is_file():put(file,Path('managed')/file.name)
for suffix in ['-command.json','-console.log','-supervisor.json','-independent-audit.json']:
    file=Path(str(L)+suffix)
    if file.is_file():put(file,Path('managed')/('launcher'+suffix))
for file in W.iterdir():
    if file.is_file():
        assert file.name in ['fresh-generation-proof.json','watcher-receipt.json']
        put(file,Path('observer')/file.name)
console=Path(str(W)+'-console.log')
if console.is_file():put(console,Path('observer/console.log'))
compressed=[]
for file in (W/'profile').iterdir():
    assert file.is_file() and file.name in ['identity-checks.json','attached.json','collection-receipt.json','cpu-summary.json','cpu-samples.jsonl','supervisor-receipt.json']
    if file.name in ['cpu-summary.json','cpu-samples.jsonl']:
        raw=file.read_bytes();packed=gzip.compress(raw,mtime=0)
        target=P/'sanitized-cpu'/(file.name+'.gz');target.parent.mkdir(exist_ok=True);target.write_bytes(packed)
        assert gzip.decompress(packed)==raw
        compressed.append(dict(originalName=file.name,originalBytes=len(raw),originalSha256=sha_bytes(raw),retainedName=str(target.relative_to(P)),gzipBytes=len(packed),gzipSha256=sha_bytes(packed),decompressedMatchesOriginal=True))
    else:put(file,Path('sanitized-cpu')/file.name)
save('sanitized-compression.json',compressed)

for file in N.rglob('*'):
    if file.is_file():put(file,Path('native-qualification')/file.relative_to(N))
receipt=Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json')
assert sha(receipt)=='2a031e4d183152beb50b56428805fab3af7b08b787dbad3fc6240ca3dd6e1fb8'
put(receipt,Path('owner/command-receipt.json'));owner=json.loads(receipt.read_text());refs=set()
def gather(value):
    if isinstance(value,str) and value.startswith('/tmp/'):
        file=Path(value)
        if file.is_file() and file.suffix in ['.xml','.json','.log','.txt','.cs','.csproj']:refs.add(file)
    elif isinstance(value,dict):
        for child in value.values():gather(child)
    elif isinstance(value,list):
        for child in value:gather(child)
gather(owner)
for file in refs:put(file,Path('owner/referenced')/str(file).lstrip('/'))
bench=owner['benchmark'];benchreceipt=Path(bench['receipt']);assert sha(benchreceipt)==bench['receiptSha256']
for file in benchreceipt.parent.iterdir():
    if file.is_file() and file.suffix not in ['.dll','.pdb','.exe'] and not file.name.endswith('.deps.json'):
        put(file,Path('benchmark/final-compiled')/file.name)
save('benchmark/provenance.json',dict(actualFinalCompiledBenchmark=True,receiptSha256=bench['receiptSha256'],notNativeOrBrowserAcceptance=True,binObjDepsRetained=False))

# The current invocation source is distinct from historical tool-build/guard proofs.
for file in C.iterdir():
    if file.is_file():put(file,Path('current-external-supervisor')/file.name)
for file in T.iterdir():
    if file.is_file() and file.name not in ['package_cpu.py','own_live.py','analyze_cpu.py']:
        put(file,Path('reused-profiler-build-and-primary-sources')/file.name)
for file in (T/'bin/Debug/net10.0').iterdir():
    if file.is_file():put(file,Path('reused-profiler-build-and-primary-sources/debug-tool')/file.name)
put(Path('/tmp/story65-receiver-acceptance-prep-20261107/audit.py'),Path('current-external-supervisor/generic-cleanup-audit.py'))

r=json.loads((W/'profile/cpu-summary.json').read_text())
c=json.loads((W/'profile/collection-receipt.json').read_text())
s=json.loads((W/'profile/supervisor-receipt.json').read_text())
run=json.loads((L/'run.json').read_text());watch=json.loads((W/'watcher-receipt.json').read_text())
audit=json.loads((P/'independent-cleanup-source-audit.json').read_text())
ids={g['Id'] for g in r['stackGroups']};count=0;violations=0
for line in (W/'profile/cpu-samples.jsonl').open():
    row=json.loads(line);count+=1
    assert set(row)=={'timeUtc','relativeMilliseconds','threadId','sampleType','stackId'} and row['stackId'] in ids and row['sampleType'] in ['Managed','External','Error','Unknown']
for group in r['stackGroups']:
    assert len(group['Frames'])<=192
    for frame in group['Frames']:
        if frame in ['[unresolved]','[unique-key-cap]']:continue
        if any(ch in frame for ch in ['/',chr(92),'"',"'"]) or any(ord(ch)<32 for ch in frame) or re.search(r'\b0x[0-9a-fA-F]+\b|\.(cs|dll|exe|so|pdb)(\b|:)',frame,re.I):violations+=1
assert violations==0 and count==r['samples']==r['sanitizedRows'] and sum(g['Count'] for g in r['stackGroups'])==count
save('privacy-and-coverage.json',dict(diagnosticOnly=True,featureAcceptance=False,allSampleRowsRetained=count,fullPopulationAggregateSamples=r['samples'],stackGroups=len(r['stackGroups']),zeroMetadataNameViolations=True,rowSchemaClosed=True,stackDepthCap=192,truncatedStacks=r['truncatedStacks'],unresolvedFrames=r['unresolvedFrames'],missingStack=r['missingStack'],eventLoss=r['traceEventsLost'],conversionLoss=r['lostOnConvert'],uniqueGroupCapOverflowSamples=r['uniqueGroupCapOverflowSamples'],sampleRowCapReached=r['sanitizedRowsCapped'],rawAndAllDerivativesAbsent=s['privateRuntimeMetadataRemoval']['allNonAllowlistedPrivateFilesAbsent'],noSymlinkTargetsFollowedOrHashed=True,runtimeInfo187CommandLineProtection='Raw nettrace/ETLX private0700/0600, hashed/deleted; strict argv preflight and no unrelated event payload emitted.',collectorCompleted=not c['incomplete'],parserSupervisorCompleted=not s['incomplete']))
rows=[json.loads(line) for line in (L/'browser-advisory-seed-readiness.jsonl').read_text().splitlines()]
single=[row for row in rows if not row['multiple']];multi=[row for row in rows if row['multiple']]
save('managed-result-and-revisions.json',dict(diagnosticOnly=True,featureAcceptance=False,result=run['result'],nativeSeedCases=1,nativeSeedFailed=1,browserCases=0,singleAttempts=len(single),singleVerifiedPositiveAttempts=[x['attempt'] for x in single if x.get('positiveCandidatesVerified')],multipleAttempts=len(multi),multipleVerifiedPositiveAttempts=[x['attempt'] for x in multi if x.get('positiveCandidatesVerified')],allMultipleEmptyIncomplete=all(x['result'] is None and x['incomplete'] and x['candidateCount']==0 for x in multi),multipleExternalElapsedMedianMilliseconds=statistics.median(x['elapsedMilliseconds'] for x in multi),externalFixtureElapsedIncludesLaterEvidenceReads=True,fullLaneAllowedToFinishNormally=True,noCutoffOrTargetLauncherSignals=True,standardReadinessTimeoutNotExtended=True,diagnosticAttachmentCanPerturbRuntime=True,canonicalRevisions=run['revisions'],sourceDiffSha256=run['sourceDiffSha256'],sourceManifestSha256=run['sourceManifestSha256'],sourceMappingHash=run['sourceMappingHash'],compilationProfile=run['compilationProfile'],freshGeneration=watch['freshGenerationProof'],nativeQualificationUniquePass=1038,nativeOwnerPass=412,nativeProjectsPass=626,nativePriorBinaryGenerationNotClaimedByteEquivalent=True,noProductionSourceOrPrivateOverlayChange=True,noApplicationBinaryRestoreNeededFromProfiler=True))

native=json.loads((N/'binary-source-audit.json').read_text())
nativeGateway=[x for x in native['files'] if x['path'].endswith('Hexalith.EventStore.Gateway.dll')]
save('generation-and-selection-provenance.json',dict(sourceCount=48,ownerReceiptSha256=sha(receipt),nativeAuditSha256=sha(N/'binary-source-audit.json'),nativeGatewayFiles=nativeGateway,currentFreshMappedGatewaySha256=watch['freshGenerationProof']['freshGenerationGatewaySha256'],nativePriorFieldInReusedWatcherIsHistoricalSeedPin=True,legacySeedPinIsNotClaimedCurrentNativeGatewayHash=True,sourceProfileEquivalenceVerifiedByCurrent48AndFull17188Inputs=True,byteEquivalenceToNativeNotClaimed=True,currentSourceFromCaptured48SnapshotsNotMovingPostReleaseTree=True))
primary=[('runtime sampler','60629d14374c56f1cb51819049ad1fa529307f8d','src/native/eventpipe/ep-sample-profiler.c'),('runtime adaptation','60629d14374c56f1cb51819049ad1fa529307f8d','src/coreclr/vm/eventing/eventpipe/ep-rt-coreclr.cpp'),('reader parser','4b14fe0c8c6101cb3c1c6bfed2e317a9d29f68ee','src/TraceEvent/EventPipe/SampleProfilerTraceEventParser.cs'),('reader interpretation','4b14fe0c8c6101cb3c1c6bfed2e317a9d29f68ee','src/TraceEvent/Computers/SampleProfilerThreadTimeComputer.cs')]
save('primary-source-provenance.json',dict(sources=[dict(name=name,revision=revision,path=path,url=f'https://github.com/{"microsoft/perfview" if "reader" in name else "dotnet/runtime"}/blob/{revision}/{path}') for name,revision,path in primary],runtimeSourceIsNet10BaseContract='Installed patch behavior is interpreted with actual sample classifications/coverage. No exact CPU interpretation follows from nominal interval.',toolReusedWithoutNewCompile=True,debugToolSha256=sha(T/'bin/Debug/net10.0/CpuProbe.dll'),providers=[dict(name='Microsoft-DotNETCore-SampleProfiler',keywords=0,level='Informational4'),dict(name='Microsoft-Windows-DotNETRuntime',keywords='0x1',level='Informational4')],requestStackwalk=True,requestRundown=True,collectionDurationSeconds=8,runtimeBufferMiB=64,hardFileLimitMiB=256,stopBeforeLimitMiB=224,additionalApplicationNetworkBodyProviders=False,heapDumpOrForcedGc=False))

textviolations=[]
for file in P.rglob('*'):
    if file.is_file() and file.suffix in ['.json','.jsonl','.log','.txt','.md','.py','.cs','.xml','.props','.targets']:
        data=file.read_bytes()
        if re.search(rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}',data) or (bytes([45])*5+b'BEGIN PRIVATE KEY'+bytes([45])*5) in data or (bytes([45])*5+b'BEGIN RSA PRIVATE KEY'+bytes([45])*5) in data:textviolations.append(str(file.relative_to(P)))
assert not textviolations
save('packet-preservation-and-privacy.json',dict(noJwtOrPrivateKeyPatternValues=True,matchingValuesPrinted=False,rawTraceEtlxPrivateConfigActivationTrustProofCredentialCopied=False,earlierEvidencePacketsUnchanged=True,noRepositorySourceGitChangesByCollector=True,all48CapturedBeforeSourceRelease=True,all17188SourcesMatchedBeforeRelease=True,all23ExactOwnedProcessIdentitiesAbsent=True,all5ExactContainersAbsent=True,originalCleanupPreservationFlagsVerified=True,privateRawAndDerivativeCleanupVerified=True,browserPersistencePrivacyAcceptanceNotExercised=True))

(P/'README.md').write_text('''This temporary packet retains the current parsed-template CPU diagnostic and failed standard managed lane. It is not Story 6.5 acceptance. The current pre-live qualification is 412 owner plus 626 Projects = 1,038 unique native passes. All 48 captured owner sources, owner receipt 2a031e4d183152beb50b56428805fab3af7b08b787dbad3fc6240ca3dd6e1fb8, parent native audit 696d669ab030d532653567ebdb7deac1481fe63dd68f27c79fda3e3a277d72e8, native XML/commands/logs/matrix/guards and final compiled parsed-template benchmark are retained. Benchmark and native gains do not establish actual browser acceptance.

One fresh standard npm managed command ran natural CPU (DOTNET_PROCESSOR_COUNT unset), ordinary Debug/source binaries, no application overlay, no Debug/private logging sink, unchanged authorization observations/order/two-second deadline/expectations, and no cutoff or target/launcher signals. SINGLE yielded one verified positive on attempt 10. MULTIPLE yielded 22 null/empty/incomplete attempts, zero positives, before the unchanged standard readiness timeout. The lane was allowed to finish normally. The external fixture median was 2507.68205 ms and includes later evidence reads; it is not internal execution time. Native browser seed failed and zero browser cases ran. Authenticated browser persistence/privacy/accessibility acceptance remains unexercised. Profiling can perturb execution, so this diagnostic lane is separate from the preceding uninstrumented 48-source ordinary failure packet (30 MULTIPLE failures, SINGLE attempt 2 positive).

The existing external Debug CPU tool was reused without a new compile. Exact build sources, Debug binaries, cache-only build receipt/logs, primary runtime/TraceEvent sources, provider/API smoke and prior CLI PPID-reparenting guard proof are retained as historical tool provenance, separate from current invocation scripts. Current watcher verifies the new 48-source receipt and full 17,188-input manifest, exact fresh owned AppHost/config/CLI/resource graph/DCP wrapper/runtime start identities, XDG/AppID, producer/API mapped Gateway and absence of forbidden timing markers. Current mapped Gateway and producer/API generation is bc66922b2468e6da885289eaa95680f8dae809e3e0e30c17ad8922652161b7b5. The inherited 321786... seed field in the reused watcher is historical bootstrap pin metadata, not the current native-audit binary hash. Actual native Gateway file hashes and fresh runtime generation are separately recorded; no byte equivalence between generations is claimed. Identity was ready during seeding and revalidated before attach. After the first genuine failed MULTIPLE, no positive ever observed and at least three windows remaining, the watcher attached only to its proven owned Gateway runtime.

Only Microsoft-DotNETCore-SampleProfiler keywords 0 / Informational 4 plus Microsoft-Windows-DotNETRuntime GC keywords 0x1 / Informational 4 were requested, with stackwalk and rundown. No app/network/body tracing, allocation provider, heap dump, forced GC or target signal was used. Collection 08:30:58.4260664Z–08:31:06.8230504Z completed stop/rundown/drain normally: 33,227,230 raw bytes, within 8-second/64-MiB-buffer/256-MiB-file/CPU/RSS guards. All 114,640 sample rows and 1,244 complete groups are retained: 6,436 Managed and 108,204 External. Reported event/conversion loss, missing stack, depth truncation, group overflow and row cap are zero. 13,515 frames are fixed unresolved labels. Raw/compressed sanitized files have original/compressed hashes and byte-for-byte decompression proofs.

Managed leaf locations include PollGC 2,612 (safepoint/sampler bias, not assigned production CPU), Monitor.Enter 591, Memmove 398, SHA256 381, buffered Read 354, PEReader.Dispose 145 and SequenceEqual 136. First retained owned managed frames include AssemblyIdentity 716, WriteCanonical 420, DevelopmentRouteCatalogInputs 392, DescribeCore 331, action Hash 277, CanonicalBytes 240 and ImageDigestMemo.TryCompute 216. External first-owned AssemblyIdentity has 611 samples; only 17 External samples fall in the crypto heuristic. These current physical stacks replace no logical-root stage trace and prove no particular advisory call count or deadline stage. Inclusive owned frames overlap and must not be added. External wait-looking API categories include idle threads; External is preemptive mode and can mean native computation, transitions or waits. It is not a blocked or exact native-CPU fraction. The runtime sampler covers walked nonempty managed-thread stacks, sleeps nominally 1 ms plus work, suspends managed execution itself and skips other suspensions. Sample fractions/counts cannot establish exact CPU time.

GC data remains distinct: 68 GCStart→GCStop cycle pairs have wall union 635.84624 ms and can include background/concurrent work, not exact pause/CPU. SuspendEE→RestartEE pairs group into four GC (0.964801 ms), 26 GCPrep (47.507833 ms), and 6,361 Other (950.64817 ms), including sampler effects. There are 67 overwritten starts and 67 unpaired restarts, so exact complete GC-pause totals cannot be claimed. cpu-interpretation.json retains actual intervals and qualifications. No request transport-versus-GC bottleneck is inferred from these overlapping intervals.

Raw execution.nettrace/ETLX may contain runtime event 187 commandline/module-path metadata. They stayed in a unique 0700/0600 private output, were hashed and deleted after sanitized parse, and all nonallowlisted derivatives and empty directories were removed without following symlinks. Only whitelisted method/type metadata, fixed unresolved labels, counts and compact rows were published; no raw trace, private identity/config/activation/TrustProof/credential input is copied here. All 23 exact runtime/diagnostic process identities, five containers, namespace and private runtime directory are independently absent. All six original cleanup observations, preexisting-resource preservation, sidecar absence, private-material removal and native-descendant absence passed. All 48 source snapshots and 17,188 live input hashes matched after cleanup before source/build exclusion release. Generic cleanup auditor's ordinary-only default is explicitly corrected in independent-cleanup-source-audit.json because this lane attached an external profiler. No application rebuild restoration is needed from this external sampler. Earlier packets remain immutable; packaging performs no source edits, builds, tests or live work.
''')
files=sorted(f for f in P.rglob('*') if f.is_file() and f.name!='SHA256SUMS')
(P/'SHA256SUMS').write_text(''.join(sha(f)+'  '+str(f.relative_to(P))+'\n' for f in files))
for line in (P/'SHA256SUMS').read_text().splitlines():digest,name=line.split('  ',1);assert sha(P/name)==digest
print(json.dumps(dict(packet=str(P),entries=len(files),checksumsSha256=sha(P/'SHA256SUMS'),sampleRows=count,completeGroups=len(r['stackGroups']),featureAcceptance=False,privacyPatternViolations=0),indent=2))
