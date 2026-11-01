"""Read-only source and repository audit; compare task-owned changes explicitly."""
from record import ROOT,EVIDENCE,WORK,state,sources,digest,write,git
import gzip,json
before=json.loads((EVIDENCE/'git-before.json').read_text());after=state()
identity_fields=('head','tree','symbolic_head','index_sha256','refs_sha256','gitlinks')
identities={repo:{key:{'before':entry[key],'after':after[repo][key]} for key in identity_fields if entry[key]!=after[repo][key]} for repo,entry in before.items()}
identities={repo:fields for repo,fields in identities.items() if fields}
old=json.loads((EVIDENCE/'sources-before.json').read_text());new=sources()
differences={name:{'before':old.get(name),'after':new.get(name)} for name in old.keys()|new.keys() if old.get(name)!=new.get(name)}
expected={'tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadAuthorityFixture.cs','tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs','tests/Hexalith.Projects.Server.Tests/Queries/GatewayFixtureRouteCatalog.cs'}
generated=json.loads((EVIDENCE/'generated-inputs.json').read_text())
expected.update(str((EVIDENCE/name).relative_to(ROOT)) for name in generated)
generated_match=all(digest(EVIDENCE/name)==value for name,value in generated.items())
fixed={}
for name in ('_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md','_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md','_bmad-output/implementation-artifacts/sprint-status.yaml'):
    import hashlib
    fixed[name]=hashlib.sha256(git(ROOT,'show',before['.']['head']+':'+name)).hexdigest()==digest(ROOT/name)
original=json.loads((WORK/'aspire-before.stdout.log').read_text());current=json.loads((WORK/'aspire-after.stdout.log').read_text())
def app_identity(item):return (item['appHostPath'],item['appHostPid'])
apps={'before':[{'path':a['appHostPath'],'pid':a['appHostPid']} for a in original],'after':[{'path':a['appHostPath'],'pid':a['appHostPid']} for a in current],'original_apphost_identities_present':set(map(app_identity,original))<=set(map(app_identity,current)),'owned_projects_apphost_absent':not any(a['appHostPath']==str(ROOT/'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj') for a in current)}
write('git-after-verified.json',after)
write('preservation-verified.json',{'repository_identity_differences':identities,'repository_identities_unchanged':not identities,'source_count_before':len(old),'source_count_after':len(new),'source_differences':differences,'only_expected_fixture_and_exact_generated_evidence_changes':set(differences)<=expected,'generated_evidence_bytes_match_capture':generated_match,'canonical_story_and_sprint_unchanged_from_clean_baseline':fixed,'aspire':apps,'limits':'Source/configuration inventory excludes ignored build/cache outputs and repository skill directories; no private runtime output is copied into evidence.'})
print(json.dumps({'repository_identities_unchanged':not identities,'source_count':len(old),'source_difference_count':len(differences),'unexpected_source_paths':sorted(set(differences)-expected),'fixed_tracking_unchanged':all(fixed.values()),'aspire_preserved':apps['original_apphost_identities_present'],'owned_apphost_absent':apps['owned_projects_apphost_absent']},indent=2))

if identities or set(differences)-expected or not generated_match or not all(fixed.values()) or not apps["original_apphost_identities_present"] or not apps["owned_projects_apphost_absent"]:
    raise SystemExit(1)
