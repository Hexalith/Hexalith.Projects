from pathlib import Path
import ast,hashlib,json
OLD=Path('/tmp/story65-receiver-phase-qualified-prep-20261107/audit_closed_q2.py')
OUT=Path(__file__).parent
assert hashlib.sha256(OLD.read_bytes()).hexdigest()=='149591f45146252991f2d7b54e46b5afc505527d1f4791be85481b2233888ff4'
s=OLD.read_text()
controller=ast.parse(Path('/tmp/story65-domain-receiver-phase-prep-20261108/qualification_controller.py').read_text())
constants={}
for node in controller.body:
 if isinstance(node,ast.Assign)and len(node.targets)==1 and isinstance(node.targets[0],ast.Name)and node.targets[0].id in ['GROUPS','NATIVE','EXPECTED']:
  constants[node.targets[0].id]=ast.literal_eval(node.value)
def replace(a,b):
 global s
 assert a in s,a[:100]
 s=s.replace(a,b)
replace('/tmp/story65-receiver-phase-prep-20261107','/tmp/story65-domain-receiver-phase-prep-20261108')
replace('/tmp/story65-receiver-phase-qualification-retry-20261107','/tmp/story65-domain-receiver-phase-qualification-20261108')
replace("PREP_SHA='83bb2d0938e32482f1a459df945e7441904e84bcd64c605d9eb223e76f2fc623'","PREP_SHA='d91e15a7ce3cff1e9ee2b30fb024923b2d9d281f9c8b48c1882bd4958e4f6374'")
replace("MANIFEST=Path('/tmp/story65-receiver-phase-runtime-manifest-retry-20261107.json')\nMANIFEST_SHA='2e127e4f3ecab5617cfcca8d847a32447cf2f3f5e65b89be1584a020c4e5ba4c'", "# Exact new runtime-manifest path/SHA is supplied by Root after its independent postbuild review.")
replace('/tmp/story65-receiver-phase-producer-prep-20261107/producer-attempts/attempt-bv7kbrua','/tmp/story65-domain-receiver-phase-producer-prep-20261108/producer-attempts/attempt-i0bt41o7')
replace("PRODUCER_SHA='d7d77a65c2ff3a8d3d767eae1eecfabb847bc597504106008e1e7f4b15f0c4fd'", "PRODUCER_SHA='39ba5bdd1ba6a1760d2e12368ab59852c8769ae204d1bef79776df722834b644'")
replace("PRODUCER_AUDIT_SHA='e0a954336da6fd64877a722d55dcb48d45482637e0a4b7a2a2851afe3e412ef7'", "PRODUCER_AUDIT_SHA='6e369fd5f63e8e0fac3ef92a761e3fc3b53c5f40f6b87c7f7bd1b45c0505e5b5'")
replace("CORE='f426ca5a31a53b26d92ed218151d1d2aa1eab4dc64b6a2f0cd35da142c0eaed1'", "CORE="+repr({'DomainRouteCatalogFilter':'121ae30a04c76268f6fee5fa725730188579d7ec741ad3e293df27b1e5bb9a06','DomainRouteCatalogGuard':'50ba9c6b9200c6463aded2bb13f64af2ce5b775b94ce6520aded74e3d186f953','DomainRouteCatalogResult':'304eba32f8ec5ef2bc8832425a8996a2ebd721b2d58ec9b6bf777d39420f8ca7'}))
replace("REFERENCE=Path('/tmp/story65-receiver-phase-ops-prep-20261107/native-reference-case-set.json')", "REFERENCE=PREP/'native-reference-case-set.json'")
replace("REFERENCE_SHA='188290c760bcd13dbad01dcf133845cda5d82b42a46889ac6c6c2aa7e43f018b'", "REFERENCE_SHA='c692fb41ac9e9cf01841b28c7d56e597ea504cc587d6dedce6d8fead9e9d176a'")
lines=s.splitlines()
for i,line in enumerate(lines):
 if line.startswith('GROUPS='):lines[i]='GROUPS='+repr(constants['GROUPS'])
 if line.startswith('NATIVE='):lines[i]='NATIVE='+repr(constants['NATIVE'])
 if line.startswith('CASES='):
  lines[i]='CASES='+repr({n:(v['entries'],v['authorityAcquireCalls'],v['nextCalls'],v['releaseCallbacks'],v['injectedFaults'],v['maskPerCapture'],v['errorType'])for n,v in constants['EXPECTED'].items()})
s='\n'.join(lines)+'\n'
replace('MiddlewareEntries','FilterEntries');replace('AcquireCalls','AuthorityAcquireCalls');replace('TokenMatched','TokensMatched');replace('LifetimeMatched','LifetimeAndHandoffMatched')
replace('Story65.ReceiverPhase.Excluded','Story65.DomainReceiverPhase.Excluded')
replace("need(r['FilterEntries']==1,'one-actual-entry')", "need(r['FilterEntries']==CASES[r['Case']][0],'actual-filter-entry-count')")
replace("==(CASES[r['Case']])", "==CASES[r['Case']]") if '==(CASES' in s else None
replace("(r['AuthorityAcquireCalls'],r['NextCalls'],r['ReleaseCallbacks'],r['Faults'],r['ExpectedStageMask'],r['ErrorType'])==CASES[r['Case']]", "(r['FilterEntries'],r['AuthorityAcquireCalls'],r['NextCalls'],r['ReleaseCallbacks'],r['Faults'],r['ExpectedStageMask'],r['ErrorType'])==CASES[r['Case']]")
replace("need(r['FirstDependencyProbeObserved']==bool(r['AuthorityAcquireCalls']or r['NextCalls']),'dependency-observed-when-called')", "need(r['FirstDependencyProbeObserved']is True,'real-first-dependency-observed')")
replace('technical-double-dispose-','filter-double-dispose-')
replace("parser.add_argument('--execute',action='store_true');", "parser.add_argument('--execute',action='store_true');parser.add_argument('--runtime-manifest',required=True);parser.add_argument('--runtime-manifest-sha256',required=True);")
replace("args=parser.parse_args();need(args.execute,'explicit-audit-execute-required')", "args=parser.parse_args();need(args.execute,'explicit-audit-execute-required')\n MANIFEST=Path(args.runtime_manifest);MANIFEST_SHA=args.runtime_manifest_sha256\n need(MANIFEST.is_absolute()and MANIFEST.is_relative_to('/tmp')and re.fullmatch('[0-9a-f]{64}',MANIFEST_SHA)is not None,'root-reviewed-runtime-manifest-pin')")
replace("summary['processes']==62", "summary['processes']==68")
replace("{'ordinary':47,'private':47}", "{'ordinary':103,'private':103}")
replace("len(preparation['sourceSha256'])==56", "len(preparation['sourceSha256'])==59")
replace("need(sha(PREP/'TechnicalHttpRouteCatalogReceiver.private.cs')==CORE and sha(PREP/'CaptureCounters.cs')==HELPER and sha(PREP/'ReceiverPhaseBootstrap.cs')==BOOTSTRAP,'exact-core-helper-bootstrap')", "need(all(sha(PREP/(name+'.private.cs'))==h for name,h in CORE.items())and sha(PREP/'CaptureCounters.cs')==HELPER and sha(PREP/'ReceiverPhaseBootstrap.cs')==BOOTSTRAP,'exact-three-core-helper-bootstrap')")
replace("manifest['sdk']", "manifest['domainService']")
replace("'Hexalith.EventStore.ServiceDefaults.dll':sdk['dll'],'Hexalith.EventStore.ServiceDefaults.pdb':sdk['pdb']", "'Hexalith.EventStore.DomainService.dll':sdk['dll'],'Hexalith.EventStore.DomainService.pdb':sdk['pdb']")
replace("{'Hexalith.EventStore.ServiceDefaults.dll','Hexalith.EventStore.ServiceDefaults.pdb'}", "{'Hexalith.EventStore.DomainService.dll','Hexalith.EventStore.DomainService.pdb'}")
replace("'only-sdk-pair-differs'", "'only-domain-pair-differs'")
replace("producer['sourceCoreInputsUnchanged']", "producer['coreInputsUnchanged']")
replace("pa['actualCompilerInputsRehashed']==502", "producer['sourceCorePrivateSha256']==CORE and len(bind(PRODUCER/'observed-compiler-inputs.json')['observedInputs'])==636")
replace("'producer-runtime-sdk-binding'", "'producer-runtime-domain-binding'")
replace("m,'native-success'", "m,'concrete-delayed-result'")
replace("'active','execution'", "'active','returns'")
replace("'excluded','native-success'", "'excluded','concrete-delayed-result'")
replace("len(expected_order)==62", "len(expected_order)==68")
replace("'all-62-receipts'", "'all-68-receipts'")
replace("facts['HistoricalSdkFallback']is False and facts['SdkSha256']==manifest['domainService'][variant]['dll']['sha256']", "facts['HistoricalSdkOrDomainFallback']is False and facts['SameOrdinarySdkBothVariants']is True and facts['DomainServiceSha256']==manifest['domainService'][variant]['dll']['sha256']and facts['SdkSha256']==manifest['commonFiles']['Hexalith.EventStore.ServiceDefaults.dll']['sha256']")
replace("'default-context-no-sdk-fallback'", "'default-context-no-sdk-or-domain-fallback'")
replace("{'Result','CaptureOrdinal','AdmittedDelta','ActualStageRowsRequired','SyntheticDurationBracketOnly'}", "{'Result','CaptureOrdinals','AdmittedDelta','ActualStageRowsRequired','HandoffCounterObservationRequired','HandoffChecks','SyntheticDurationBracketOnly'}")
replace("integers(item,['CaptureOrdinal','AdmittedDelta']);need(item['AdmittedDelta']==int(recording)and item['ActualStageRowsRequired']==(recording and mode=='active'),'all-eligible-cases-actually-admitted')", "integers(item,['AdmittedDelta','HandoffChecks']);ordinals=item['CaptureOrdinals'];need(type(ordinals)is list and all(type(n)is int and n>0 for n in ordinals),'typed-own-capture-ordinals')\n    need(item['AdmittedDelta']==(item['Result']['FilterEntries']if recording else 0)and item['ActualStageRowsRequired']==(recording and mode=='active'),'all-eligible-filter-entries-actually-admitted')\n    need((not recording and ordinals==[])or(recording and len(ordinals)==item['Result']['FilterEntries']and ordinals==list(range(ordinals[0],ordinals[0]+len(ordinals)))),'consecutive-separate-nested-captures')\n    deferred=item['Result']['ExpectedStageMask']==255;required=recording and deferred\n    need(type(item['HandoffCounterObservationRequired'])is bool and item['HandoffCounterObservationRequired']==required and item['HandoffChecks']==int(required),'actual-live-captures-at-deferred-handoff')")
replace("business['ExpectedStages']==191", "business['ExpectedStages']==255")
replace("business['ProbeChecks']>=4", "business['ProbeChecks']==14")
replace("observed=by_capture[item['CaptureOrdinal']];mask=item['Result']['ExpectedStageMask'];need(set(observed)=={STAGES[i]for i in range(8)if mask&(1<<i)},'exact-actual-middleware-stage-mask')\n     if item['SyntheticDurationBracketOnly']:\n      a=item['Result'];need(a['MinimumLastDisposeTicks']<=observed[STAGES[5]]<=a['MaximumLastDisposeTicks']<a['FirstDisposeHoldTicks'],'actual-last-release-interval')", "mask=item['Result']['ExpectedStageMask']\n     for ordinal in item['CaptureOrdinals']:\n      observed=by_capture[ordinal];need(set(observed)=={STAGES[i]for i in range(8)if mask&(1<<i)},'exact-actual-filter-result-stage-mask')\n      if item['SyntheticDurationBracketOnly']:\n       a=item['Result'];need(a['MinimumLastDisposeTicks']<=observed[STAGES[5]]<=a['MaximumLastDisposeTicks']<a['FirstDisposeHoldTicks'],'actual-last-release-interval')")
# The new first-attempt qualifier has no old failed-Q1 lineage; preserve only actual build/source-correction history.
start=s.index(' # Preserve both the initial failed source/runtime lineage')
end=s.index(" audit={'qualified':True",start)
s=s[:start]+" # Bind the exact completed fixture-only build generation and its command/artifact receipts.\n build=bind('/tmp/story65-domain-receiver-phase-fixture-build-20261108/receipt.json','a5971947cc0959099bf1cd07c235a5ee1ce6ec2a4b9bcf8bef1f29ef57fecd56')\n need(build['all12CommandsClosedZero']is True and build['allSixDebugBuildsZeroWarningsAndErrors']is True and build['all1570CurrentInputsUnchanged']is True,'actual-fixture-build-inputs-closed')\n"+s[end:]
replace("'sourceCount':56", "'sourceCount':59")
replace("'controlledProcesses':62", "'controlledProcesses':68")
replace("'all62ActualPidsAndGroupsAbsent'", "'all68ActualPidsAndGroupsAbsent'")
replace("'diagnosticNativeExecutions':94", "'diagnosticNativeExecutions':206")
replace("'onlySdkDllPdbDifferBetweenVariantClosures'", "'onlyDomainServiceDllPdbDifferBetweenVariantClosures'")
replace("'noDefaultSdkFallback'", "'noDefaultSdkOrDomainFallback'")
replace("'priorAttemptFailureAndCorrectionPreserved':True", "'sourceCorrectionsPreservedOutsideRuntimeInputs':True,'actualNestedTwoFilterCapturesAndDeferredHandoffVerified':True,'focusedFilterEntriesPerVariant':72,'sameOrdinarySdkBothVariants':True")
replace("'last completed or unwound original DisposeAsync interval per capture; not total, callback duration or count'", "'last original disposal interval per capture; result await-using region includes only disposal/unwind glue; not total, callback duration or count'")
replace("'excludes entry-name eligibility and initial admission-lock wait; includes post-timestamp capture construction/admission bookkeeping; stops before queue writes'", "'owned filter-to-result lifetime including delayed handoff; excludes entry eligibility and initial helper-lock wait, includes post-timestamp bookkeeping; unowned/refusal cases close locally'")
replace("'original and private native47 are diagnostic executions; ordinary native1048 is unchanged'", "'original and private native103 are diagnostic executions; ordinary native1048 is unchanged','two Folders-style nested filters preserve both acquisitions/results/captures; overlapping stages are not summed','static installation witness and typed scratch pipeline are not fresh live endpoint-factory inspection'")
replace("'diagnosticNativeExecutions':94,'processes':62", "'diagnosticNativeExecutions':206,'processes':68") if "'diagnosticNativeExecutions':94,'processes':62"in s else None
replace("'processes':62", "'processes':68") if "'processes':62"in s else None
# Require the same ordinary SDK bytes and all bound observed compiler inputs independently.
anchor=" producer=bind(PRODUCER/'receipt.json',PRODUCER_SHA);pa=bind(PRODUCER/'parent-producer-audit.json',PRODUCER_AUDIT_SHA)"
replace(anchor," need(manifest['commonFiles']['Hexalith.EventStore.ServiceDefaults.dll']['sha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'and manifest['commonFiles']['Hexalith.EventStore.ServiceDefaults.pdb']['sha256']=='ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6','same-ordinary-sdk-pair')\n"+anchor)
replace(" need(producer['candidateDllSha256']", " for row in bind(PRODUCER/'observed-compiler-inputs.json')['observedInputs']:need(sha(row['path'])==row['sha256'],'actual-producer-compiler-input-current-hash')\n need(producer['candidateDllSha256']")
# Closed count proof for actual entry counts (cases64, filter entries72 per variant).
replace("need(pair_count==64,'all-64-semantic-pairs')", "need(pair_count==64,'all-64-semantic-pairs')\n for variant in ['ordinary','private']:\n  need(sum(item['Result']['FilterEntries']for (mode,group,v),business in by_key.items()if v==variant for item in business['Cases'])==72,'all72-focused-filter-entries-per-variant')")
# Namespace change in comments/docstrings only, no claim of previous runtime Q2 here.
s=s.replace('receiver Q2','domain receiver qualifier').replace('CLOSED receiver Q2','CLOSED domain receiver qualifier')
ast.parse(s)
(OUT/'audit_closed_qualification.py').write_text(s)
(OUT/'adaptation-provenance.json').write_text(json.dumps({'filePreparationOnly':True,'sourceAudit':str(OLD),'sourceAuditSha256':hashlib.sha256(OLD.read_bytes()).hexdigest(),'newAuditSha256':hashlib.sha256(s.encode()).hexdigest(),'namespaceCarrier':'DomainService variant only; same ordinary SDK','caseCount':64,'nativePerVariant':103,'processes':68,'nestedFilterEntriesPerVariant':72,'scriptExecuted':False},indent=2)+'\n')
print(json.dumps({'script':str(OUT/'audit_closed_qualification.py'),'sha256':hashlib.sha256(s.encode()).hexdigest(),'lines':len(s.splitlines()),'syntaxOnly':True,'executed':False}))
