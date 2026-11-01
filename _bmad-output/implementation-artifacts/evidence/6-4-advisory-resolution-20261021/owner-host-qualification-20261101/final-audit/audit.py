from record import ROOT,EVIDENCE,WORK,write,state,sources,digest,run
from pathlib import Path
import json,gzip,os
before=json.loads((EVIDENCE/'git-before.json').read_text());after=state();write('git-after.json',after)
identity={name:{key:value[key]==after[name][key] for key in ['head','tree','symbolic_head','index_sha256','refs_sha256','gitlinks']} for name,value in before.items()}
old=json.loads(gzip.decompress((EVIDENCE/'sources-before.json.gz').read_bytes()));new=sources()
receipt_prefix=str(EVIDENCE.parent.relative_to(ROOT))+'/'
changed=[name for name in sorted(set(old)|set(new)) if old.get(name)!=new.get(name) and not name.startswith(receipt_prefix)]
with gzip.open(EVIDENCE/'sources-after.json.gz','wt') as f:json.dump(new,f,sort_keys=True)
historical=ROOT/'_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/current-advisory-source-20261031';old_history=json.loads((EVIDENCE/'historical-evidence-before.json').read_text());history_changes=[name for name,h in old_history.items() if not (historical/name).is_file() or digest(historical/name)!=h]
parties=ROOT/'references/Hexalith.Parties';old_parties=json.loads((EVIDENCE/'shared-parties-before.json').read_text());current_parties={str(p.relative_to(parties)):digest(p) for p in parties.rglob('*') if p.is_file() and '.git' not in p.parts};parties_changes=[name for name in sorted(set(old_parties)|set(current_parties)) if old_parties.get(name)!=current_parties.get(name)]
canonical={name:digest(ROOT/name)==value for name,value in json.loads((EVIDENCE/'canonical-before.json').read_text()).items()}
# Every original dirty path outside the owner qualification scope must keep its original bytes.
changed_prefixes=('references/Hexalith.Conversations/','references/Hexalith.Folders/')
original_dirty={name:new.get(name)==h for name,h in old.items() if not name.startswith(changed_prefixes) and not name.startswith(receipt_prefix)}
allowed={
 'references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Agents/ConversationDeletionRuntimeHostExtensions.cs',
 'references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Agents/ConversationDeletionRuntimeHostTests.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Agents/DeletionRuntimeTestDoubles.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Agents/DeletionRuntimeRouteCatalog.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionNativeEnrollmentTest.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionReceiverGuardTest.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/ResolutionEnrollmentFixture.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/ResolutionRouteCatalog.cs',
 'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/ResolutionRebuildDelegatedTransport.cs',
 'references/Hexalith.Folders/Directory.Build.props',
 'references/Hexalith.Folders/src/Hexalith.Folders.Server/FoldersDomainServiceEndpoints.cs',
 'references/Hexalith.Folders/src/Hexalith.Folders.Server/FoldersServerHostComposition.cs',
 'references/Hexalith.Folders/src/Hexalith.Folders.Server/Hexalith.Folders.Server.csproj',
 'references/Hexalith.Folders/src/Hexalith.Folders.ServiceDefaults/Extensions.cs',
 'references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FolderRepositoryStartupAssertionTests.cs',
 'references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FoldersCallbackBoundaryTests.cs',
 'references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FoldersCallbackRouteCatalog.cs',
 'references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FoldersProductionTestHost.cs',
 'references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FoldersServerHostCompositionTests.cs',
 'references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/ServerEndpointRegistrationTests.cs',
}
assert set(changed)<=allowed,(set(changed)-allowed)
source=Path(json.loads((EVIDENCE/'workspace.json').read_text())['source']);sync={name:digest(ROOT/name)==digest(source/name) for name in changed}
write('preservation-final.json',{'git_identity':identity,'all_git_identities_preserved':all(all(v.values()) for v in identity.values()),'changed_source_paths':changed,'all_source_changes_in_approved_owner_scope':set(changed)<=allowed,'final_source_sha256':{name:new[name] for name in changed},'scratch_source_matches':sync,'historical_files_checked':len(old_history),'historical_changes':history_changes,'shared_parties_files_checked':len(old_parties),'shared_parties_changes':parties_changes,'canonical_story_and_sprint_preserved':canonical,'non_owner_original_source_bytes_preserved':all(original_dirty.values()),'nested_initialization':False,'real_index_refs_stage_commit_push_publication_deployment_operations':False})
write('generated-inputs-final.json',{owner:{str(p.relative_to(WORK/(owner+'-mapping'))):digest(p) for p in (WORK/(owner+'-mapping')).rglob('*') if p.is_file()} for owner in ['projects','conversations']})
assert json.loads((EVIDENCE/'generated-inputs.json').read_text())==json.loads((EVIDENCE/'generated-inputs-final.json').read_text())
assert all(all(v.values()) for v in identity.values()) and not history_changes and not parties_changes and all(canonical.values()) and all(original_dirty.values()) and all(sync.values())
for name,args in [('production-authority-story',['--story-id','6.4']),('production-authority-index',['--validate-index'])]:
 code=run(name,['python3','tools/planning/validate_production_authority.py',*args]);assert code==0
for name,repo in [('projects',ROOT),('conversations',ROOT/'references/Hexalith.Conversations'),('folders',ROOT/'references/Hexalith.Folders')]:
 code=run(name+'-whitespace',['git','-c','core.whitespace=cr-at-eol','diff','--check'],cwd=repo);assert code==0
print('Preservation, current generated inputs, production authority and owning whitespace validated.',flush=True)
