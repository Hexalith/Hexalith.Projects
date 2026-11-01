from record import ROOT,EVIDENCE,WORK,write,digest,run
from pathlib import Path
from collections import Counter
import datetime,json,socket,xml.etree.ElementTree as ET
selected=['projects-contracts-full','projects-core-full','routing-final-projects-server-full','conversations-qualified-full','folders-routing-full']
results={};trees={}
for name in selected:
    data=json.loads((EVIDENCE/(name+'-result.json')).read_text());assert data['exit_code']==0 and data['executable_closure_unchanged']
    tree=ET.parse(EVIDENCE/(name+'.xml'));trees[name]=tree;a=tree.find('assembly')
    counts={key:int(a.get(key,0)) for key in ['total','passed','failed','skipped','errors','not-run']}
    assert counts['total']>0 and counts['passed']==counts['total'] and all(counts[k]==0 for k in ['failed','skipped','errors','not-run'])
    command=json.loads((EVIDENCE/(name+'.command.json')).read_text());assembly=Path(command['argv'][1])
    current={p.name:digest(p) for p in assembly.parent.iterdir() if p.is_file() and p.suffix in ('.dll','.pdb','.json')}
    before=json.loads((EVIDENCE/(name+'-executables-before.json')).read_text());assert current==before,(name,'changed executable closure')
    results[name]={'command':name+'.command.json','xml':name+'.xml','binary_hashes':name+'-executables-before.json','current_executable_closure_matches':True,**counts}
write('selected-test-results.json',{'selected':results,'totals':{key:sum(v[key] for v in results.values()) for key in ['total','passed','failed','skipped','errors','not-run']},'full_affected_source_acceptance':True,'canonical_story_and_sprint':'in-progress','publication_deployment_consumer_acceptance':False})

def cases(name,methods=(),classes=()):
    return [{'name':test.get('name'),'method':test.get('method'),'type':test.get('type'),'result':test.get('result'),'xml':name+'.xml'} for test in trees[name].findall('.//test') if test.get('method') in methods or any(test.get('type','').endswith('.'+item) for item in classes)]
folders='folders-routing-full';conversation='conversations-qualified-full';server='routing-final-projects-server-full'
matrix={
 'host_endpoint_source_named_policies_and_routing':cases(folders,methods=['ProductionHostNamesEveryPolicyAndEnrollsTheSdkEndpointSource','ProductionHostStartsWithActorFoldedRepositories','ProductionHostRoutesBeforeSharedSecurityInEveryApiMode','ProductionCallbackUsesItsOperationPolicy']),
 'valid_catalog_dispatch_metadata_and_recovery':cases(conversation,classes=['ConversationResolutionNativeEnrollmentTest','ConversationResolutionSelectionRecoveryTest','ConversationResolutionRebuildCompletionTest']),
 'root_epoch_host_target_tenant_capability_and_service_refusals':cases(conversation,methods=['ReceiverRefusesUncommittedAuthorityWithoutChangingPersistedState']),
 'signed_workload_channel_operation_and_caller_refusals':cases(folders,methods=['ProductionCallbacksRejectMissingInvalidAndWrongOperationCredentials']),
 'signed_process_events_legacy_replay_and_no_operational_writes':cases(folders,methods=['SignedWorkloadAndHumanReceiptAreAcceptedByProductionProcessRegistration']),
 'repository_startup_failures':cases(folders,methods=['ProductionHostRefusesMissingOrMemoryRepository']),
 'transport_redirect_refusal_and_real_inner_handler':cases(conversation,methods=['LookupInvocationRefusesRedirectAndUncommittedTargetWithoutTransferringAuthority','OutboundClientsDisableRedirects']),
 'generic_api_binary_and_default_policy_compatibility':cases(conversation,methods=['ParticipantMappingPreservesBinarySignatureAndSelectedPolicy','ParticipantRouteMapsTheTokenSubjectAndRejectsAPresetNameIdentifier','ConversationClientNeverMixesCallerAndWorkerIdentities']),
 'persisted_pending_retry_quarantine_and_acknowledgement':cases(conversation,methods=['PumpAdvancesOnlyAfterAnIndependentExactAcknowledgement','MismatchedReceiptQuarantinesWithoutAcknowledgement']),
 'real_gateway_advisory_and_physical_zero_write_refusals':cases(server,methods=['GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures','GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites']),
 'advisory_owner_regressions':cases(conversation,classes=['ConversationAdvisoryMetadataQueryHandlerTest','ConversationResolutionMetadataQueryHandlerTest','ConversationResolutionScoringLabelProjectionTest'])+cases(folders,classes=['FolderOwnerReadQueryHandlerTests']),
}
assert all(value and all(test['result']=='Pass' for test in value) for value in matrix.values())
write('execution-matrix.json',{'coverage':matrix,'folders_standalone_source_build':'folders-routing-identifiers-build.command.json','folders_source_selection':'folders-supported-source-selection.json','actual_sdk_guard_persisted_snapshots':'The 11 receiver refusal cases seed a completed projection and compare every retained key byte SHA-256 and ETag before/after, in addition to successful mutation count.','workload_loop_inputs':['absent','channel-only','assertion-only','wrong-channel','wrong-operation','wrong-signature','wrong-caller'],'six_gateway_routes':['projects-list','projects-open','projects-suggest-conversation','projects-suggest-attachments','folders-owner-read','conversations-advisory-metadata']})
snapshots=[]
for test in trees[server].findall('.//test'):
    if test.get('method')!='GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites':continue
    for line in test.findtext('output','').splitlines():
        try:value=json.loads(line)
        except json.JSONDecodeError:continue
        if 'Before' not in value:continue
        assert value['Before']==value['After'] and len(value['Before'])==14
        snapshots.append({'case':test.get('name'),'result':test.get('result'),'xml':server+'.xml','observations':value})
assert len(snapshots)==3;write('final-projects-zero-write-snapshots.json',snapshots)

# Dedicated runtimes were already stopped in each driver's finally block; verify absence again.
cleanups={name:json.loads((EVIDENCE/name).read_text()) for name in ['cleanup.json','api-final-cleanup.json','routing-final-cleanup.json']}
for value in cleanups.values():assert value['all_owned_processes_absent'] and value['all_owned_listeners_absent']
remaining=[]
for proc in Path('/proc').iterdir():
    if not proc.name.isdecimal():continue
    try:args=(proc/'cmdline').read_bytes().decode().split('\0')
    except (OSError,UnicodeError):continue
    if len(args)>2 and args[0]=='dotnet' and any(str(EVIDENCE/(name+'.xml')) in args for name in selected):remaining.append(int(proc.name))
assert not remaining
write('final-runtime-cleanup.json',{'all_dedicated_runtime_receipts_pass':True,'cleanup_receipts':list(cleanups),'selected_test_processes_remaining':remaining,'owned_test_hosts_are_in_process_and_their_test_assemblies_have_exited':True,'shared_resources_stopped':False,'verified_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()})

# Preserve historical outcome sections; record this approved successor and finish only scoped tasks.
owner=ROOT/'_bmad-output/implementation-artifacts/spec-6-4-owner-host-qualification.md';body=owner.read_bytes().decode();body=body.replace('- [ ]','- [x]')
body+='''\r\n## Executed owner-host qualification outcome\r\n\r\nAll five affected Debug/source projects pass their full native xUnit v3 runs without failures, skips, errors or unexecuted cases: Projects Contracts 230/230, core 684/684, Server 1309/1309; Conversations Server 1522/1522; Folders Server 1287/1287. The total is 5032 passing cases. Final selected executable closures match both their before/after run bindings and the current source-built files.\r\n\r\nFolders now composes the canonical SDK host services, endpoint source and security, explicit human/channel/workload policies and callback catalog guards. Shared HTTP security runs after route selection in all three API routing modes. Canonical `/alive` and `/ready` probes are available; legacy health paths retain explicit human policies. The extra signed-process fixture uses that actual host and retains its event, replay, credential refusal and legacy no-write assertions.\r\n\r\nConversations fixtures supply validated committed receiver catalog/capability/auxiliary bindings, exact host identity, generation headers and service surfaces while running the actual SDK guards. Eleven negative authority cases compare persisted byte/ETag snapshots. Deletion fixtures provide a separately owned invocation transport beneath the SDK wrapper; an actual 307 redirect and uncommitted recipient cannot transfer authority. The original one-parameter participant API CLR signature and generic default-policy semantics remain; its additive named-policy overload is explicit.\r\n\r\nFresh public producer mappings for Projects and Conversations were applied unchanged. Folders has no supported v2 manifest: its ordinary individual Debug/source build passes with the existing documented global `-p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false` inputs and complete dependency rebuilding. The owner passes its selected source mode and independently located Platform root into Memories. Standalone selection has an empty mapping hash and one source Commons.UniqueIds assembly. Plain Debug without the global source inputs still mixes sibling package defaults; those failures and the abandoned item-default attempt are retained. No no-project-rebuild fallback is selected as acceptance.\r\n\r\nReceipt: `evidence/6-4-advisory-resolution-20261021/owner-host-qualification-20261101/README.md`. It binds exact commands, generated inputs, source and binary hashes, native XML, executed matrix coverage, real-Dapr physical zero-write digests, preserved failures and owned runtime cleanup. Static receiver-fixture admission labels are SDK-valid fixture labels, not the owner's verbatim or qualified production admission schema; no digest-key availability or installation authority is inferred. Original dirty work, historical evidence, all real repository identities/indexes/refs/gitlinks and shared Parties bytes remain preserved. Canonical Story 6.4/sprint stay in-progress; publication/deployment/consumer gates and the Parties publication hold remain independent.\r\n'''
owner.write_bytes(body.encode())
prior=ROOT/'_bmad-output/implementation-artifacts/spec-6-4-current-advisory-source-revalidation.md';body=prior.read_bytes().decode().replace('- [ ] Run the existing focused and full affected test projects','- [x] Run the existing focused and full affected test projects',1)
body+='''\r\n## Executed successor: owner-host qualification\r\n\r\nThe approved owner continuation now passes all affected full projects: Projects Contracts 230/230, core 684/684, Server 1309/1309; Conversations Server 1522/1522; Folders Server 1287/1287 (5032 total, no failures/skips/errors). SDK security/catalog/redirect guards remain active, including eleven persisted-state authority controls, an actual redirect control, original participant CLR/default-policy compatibility, all three Folders routing modes and the signed process/replay fixture. The final gateway run repeats all six query routes and three real-Dapr catalog refusals with fourteen unchanged physical byte/ETag digest observations per refusal.\r\n\r\nCurrent receipt and exact selected evidence: `evidence/6-4-advisory-resolution-20261021/owner-host-qualification-20261101/README.md`. Earlier repair/source/full failures are retained. Folders uses its documented global Debug/source properties with full dependency rebuilding and an empty mapping hash; plain Debug without those inputs is not the selected source environment. Both production-authority validators, owning whitespace and preservation checks pass. Canonical Story 6.4/sprint remain in-progress; no publication/deployment/consumer acceptance or release of the Parties publication hold is implied.\r\n'''
prior.write_bytes(body.encode())
write('scoped-spec-bindings.json',{str(path.relative_to(ROOT)):digest(path) for path in [owner,prior]})
print('Selected full suites and matrix verified; scoped specs updated; canonical story/sprint unchanged.',flush=True)
