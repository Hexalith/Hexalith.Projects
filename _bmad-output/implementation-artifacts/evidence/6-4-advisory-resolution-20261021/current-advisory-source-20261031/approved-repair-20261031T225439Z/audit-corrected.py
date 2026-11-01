"""Read-only identity, preservation, and public-evidence checks."""
from record import ROOT,EVIDENCE,WORK,write,digest,state,sources,git,run
from pathlib import Path
import gzip,json,hashlib,os,re

before=json.loads((EVIDENCE/'git-before.json').read_text());after=state();write('git-after-final.json',after)
identity={name:{key:before[name][key]==after[name][key] for key in ['head','tree','symbolic_head','index_sha256','refs_sha256','gitlinks']} for name in before}
with gzip.open(EVIDENCE/'sources-before.json.gz','rt') as f:old=json.load(f)
new=sources();allchanged=[name for name in sorted(set(old)|set(new)) if old.get(name)!=new.get(name)]
allowed_evidence_prefix=str(EVIDENCE.relative_to(ROOT))+'/'
changed=[name for name in allchanged if not name.startswith(allowed_evidence_prefix)]
generated_evidence=[name for name in allchanged if name.startswith(allowed_evidence_prefix)]
with gzip.open(EVIDENCE/'sources-after-final.json.gz','wt') as f:json.dump(new,f,sort_keys=True)
expected=[
'src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs',
'src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs',
'tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs',
'tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectReadTests.cs',
'tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadAuthorityFixture.cs',
'references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/GetConversationAdvisoryMetadataDomainQueryHandler.cs',
'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAdvisoryMetadataQueryHandlerTest.cs',
'references/Hexalith.Folders/src/Hexalith.Folders.Server/Queries/FolderOwnerReadQueryHandler.cs',
'references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FolderOwnerReadQueryHandlerTests.cs']
historical=json.loads((EVIDENCE/'historical-evidence-before.json').read_text())
historical_changes=[name for name,value in historical.items() if not (EVIDENCE.parent/name).is_file() or digest(EVIDENCE.parent/name)!=value]
parties=ROOT/'references/Hexalith.Parties';oldparty=json.loads((EVIDENCE/'shared-parties-before.json').read_text());newparty={}
for directory,dirs,files in os.walk(parties):
 dirs[:]=[d for d in dirs if d not in ['.git','node_modules']]
 for name in files:
  path=Path(directory)/name
  if name=='.git':continue
  if path.is_file():newparty[str(path.relative_to(parties))]=digest(path)
partychanges=[name for name in sorted(set(oldparty)|set(newparty)) if oldparty.get(name)!=newparty.get(name)]
fixture='tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadAuthorityFixture.cs';s=(ROOT/fixture).read_bytes().decode()
s=s.replace('/// <summary>Owned Development test issuer and Production-configured receivers over real persisted Project and Folder handlers.</summary>','/// <summary>Owned production-mode broker, issuer and receiver hosts over real persisted Project and Folder handlers.</summary>',1)
start=s.index('    private async Task StartAuthorityAsync()');tail=s[start:].replace('EnvironmentName = Environments.Development','EnvironmentName = Environments.Production',1).replace('            options.NonconformingDevelopmentFixture = true;\r\n','',1);s=s[:start]+tail
reconstructed=hashlib.sha256(s.encode()).hexdigest()
fixtures=json.loads((EVIDENCE/'applied-repair.json').read_text())['preserved_fixture_sha256']
original_fixtures={name:(reconstructed if name==fixture else digest(ROOT/name))==value for name,value in fixtures.items()}
isolated=Path(json.loads((EVIDENCE/'isolated-workspace.json').read_text())['root'])
source_sync={name:digest(ROOT/name)==digest(isolated/name) for name in expected}
summary={'git_identity_preserved':identity,'all_git_identities_preserved':all(all(v.values()) for v in identity.values()),'changed_source_paths':changed,'new_generated_evidence_paths':generated_evidence,'audit_filter_correction':'Exclude only the command-owned evidence directory from source changes, and exclude Parties .git administrative file consistently with initial capture','only_authorized_source_changes':set(changed)==set(expected),'source_sha256':{name:new[name] for name in expected},'isolated_source_matches_real_current_source':source_sync,'historical_files_checked':len(historical),'historical_evidence_changes':historical_changes,'shared_parties_files_checked':len(oldparty),'shared_parties_changed_paths':partychanges,'original_dirty_fixture_bytes_preserved_except_authorized_issuer_compatibility':original_fixtures,'nested_submodule_updates_performed':False,'staged_committed_pushed_published_deployed':False}
write('preservation-final.json',summary)
for name,args in [('production-authority-story',['--story-id','6.4']),('production-authority-index',['--validate-index'])]:
 run('final-'+name,['python3','tools/planning/validate_production_authority.py',*args])
for name,repo in [('projects',ROOT),('conversations',ROOT/'references/Hexalith.Conversations'),('folders',ROOT/'references/Hexalith.Folders')]:
 run('final-'+name+'-whitespace',['git','-c','core.whitespace=cr-at-eol','diff','--check'],cwd=repo)
print(json.dumps({k:v for k,v in summary.items() if k not in ['git_identity_preserved','source_sha256','isolated_source_matches_real_current_source']},indent=2),flush=True)
assert summary['all_git_identities_preserved'] and summary['only_authorized_source_changes'] and not historical_changes and not partychanges and all(original_fixtures.values()) and all(source_sync.values())
