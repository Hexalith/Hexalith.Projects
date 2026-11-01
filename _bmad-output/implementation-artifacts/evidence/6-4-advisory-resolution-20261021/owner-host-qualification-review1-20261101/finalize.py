"""Seal focused review-fix evidence without altering earlier qualification receipts."""
from record import ROOT,EVIDENCE,WORK,write,digest,sources,state,git
from pathlib import Path
import gzip,json,hashlib,shutil

original_write=write
def write(name,value):
 target=EVIDENCE/name
 if target.exists():
  assert json.loads(target.read_text())==value,name
 else:original_write(name,value)

selected=['folders-host-final-focused','folders-observability-focused','routing-schema-projects-query-focused']
results={name:json.loads((EVIDENCE/(name+'-result.json')).read_text()) for name in selected}
for name,result in results.items():
 assert result['exit_code']==0 and result['passed']==result['total'] and result['total']>0 and result['failed']==0 and result['errors']==0 and result['skipped']==0 and result['executable_closure_unchanged'],name
before=json.loads(gzip.decompress((EVIDENCE/'sources-before.json.gz').read_bytes()))
after=sources()
changes={name:{'before_sha256':before.get(name),'after_sha256':h} for name,h in after.items() if before.get(name)!=h}
for name in ['README.md','deploy/observability/production/observability.yaml']:
 full='references/Hexalith.Folders/'+name
 previous=git(ROOT/'references/Hexalith.Folders','show','HEAD:'+name)
 changes[full]={'before_sha256':hashlib.sha256(previous).hexdigest(),'after_sha256':digest(ROOT/full),'baseline':'tracked clean file at git-before capture'}
expected={
 'tests/Hexalith.Projects.Server.Tests/Queries/GatewayFixtureRouteCatalog.cs',
 'tests/Hexalith.Projects.Server.Tests/Queries/GatewayFixtureRouteCatalogTests.cs',
 'tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs',
 *('references/Hexalith.Folders/'+name for name in [
 'README.md','deploy/observability/production/observability.yaml','docs/operations/production-observability.md',
 'src/Hexalith.Folders.Server/FoldersServerHostComposition.cs',
 'tests/Hexalith.Folders.Contracts.Tests/Deployment/ProductionObservabilityConformanceTests.cs',
 'tests/Hexalith.Folders.Server.Tests/FoldersProductionTestHost.cs',
 'tests/Hexalith.Folders.Server.Tests/FoldersServerHostCompositionTests.cs',
 'tests/Hexalith.Folders.Server.Tests/HostDefaultsInvocationHandler.cs',
 'tests/Hexalith.Folders.Server.Tests/HostModeFolderAclReadModel.cs',
 'tests/Hexalith.Folders.Server.Tests/HostModuleMetricExporter.cs',
 'tests/Hexalith.Folders.Server.Tests/HostReadinessSnapshotSource.cs',
 'tests/tools/run-production-observability-gates.ps1'])}
assert set(changes)==expected,(set(changes)-expected,expected-set(changes))
source=Path(json.loads((EVIDENCE/'workspace.json').read_text())['source'])
assert all(digest(source/name)==row['after_sha256'] for name,row in changes.items())
write('final-source-changes.json',changes)
write('sources-after.json',after)
aftergit=state();write('git-after.json',aftergit)
prior=json.loads((EVIDENCE/'git-before.json').read_text())
checks={repo:{key:row[key]==prior[repo][key] for key in ['head','tree','symbolic_head','index_sha256','refs_sha256','gitlinks']} for repo,row in aftergit.items()}
assert all(all(row.values()) for row in checks.values())
old=Path(json.loads((EVIDENCE/'workspace.json').read_text())['previous_receipt'])
old_before=json.loads((EVIDENCE/'sealed-receipt-before.json').read_text())
# Match the historical seal's policy; these pre-existing ignored caches are not sealed artifacts.
old_after={str(p.relative_to(old)):digest(p) for p in old.rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.suffix != '.pyc'}
assert old_before==old_after
sdk='references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/RouteCatalogInvocationHandler.cs'
assert before[sdk]==after[sdk]
failed=json.loads((EVIDENCE/'folders-host-focused-result.json').read_text())
assert failed['failed']==2
write('otlp-delivery-limitation.json',{
 'status':'delivery not qualified; requested provider startup and module subscription pass separately',
 'pre_existing_sdk_source':sdk,'unchanged_before_after_sha256':after[sdk],
 'cause':'HttpProtobuf OTLP traces/metrics use synchronous HttpClient.Send; SDK RouteCatalogInvocationHandler implements only SendAsync, producing NotSupportedException.',
 'native_failed_xml':'folders-host-focused.xml','native_failed_xml_sha256':digest(EVIDENCE/'folders-host-focused.xml'),
 'exact_failed_case':'Hexalith.Folders.Server.Tests.FoldersServerHostCompositionTests.ConfiguredOtlpStartsOneProviderPerSignalAndExportsFoldersSignals',
 'private_diagnostics':json.loads((EVIDENCE/'folders-host-focused.command.json').read_text()),
 'remediation':'Parent will triage the pre-existing SDK producer issue. No SDK transport or dependency changes in this repair.',
 'final_test_scope':'ConfiguredOtlpStartsOneProviderPerSignalAndSubscribesFoldersSignals verifies actual configured provider startup, one provider per signal, trace source subscription and local metric-reader subscription; it makes no OTLP delivery claim.'})
write('preservation.json',{'git_invariants':checks,'previous_sealed_receipt_unchanged':True,'previous_receipt_artifacts':len(old_after),'source_scope_exact':True,'source_overlay_matches_verified_owner_files':True,'nested_submodules_updated':False,'dependencies_changed':False,'full_suites_run':False})
write('selected-results.json',results)
for cache in EVIDENCE.glob('__pycache__'):
 shutil.rmtree(cache)
print(json.dumps({'selected_cases':sum(r['total'] for r in results.values()),'owner_source_files':len(changes),'sealed_previous_artifacts':len(old_after),'all_selected_passing':True}))
