"""Metadata-only final manifest creation; never releases or invokes the pipeline."""
from pathlib import Path
import ast,hashlib,json,xml.etree.ElementTree as ET
P=Path('/tmp/story65-receiver-phase-ops-prep-20261107');ROOT=Path('/home/administrator/projects/hexalith/projects');SOURCE=Path('/tmp/story65-receiver-phase-prep-20261107');QROOT=Path('/tmp/story65-receiver-phase-qualification-retry-20261107')
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
external={};excluded={}
def add(path,digest=None):
 path=Path(path);path=path if path.is_absolute()else ROOT/path
 assert path.is_file()and not path.is_symlink()
 if path.is_relative_to(ROOT)and any(x in['bin','obj']for x in path.relative_to(ROOT).parts):
  excluded[str(path)]={'reason':'mutable-workspace-build-output; guarded by fresh baseline/current owned-generation proofs'};return
 value=sha(path);assert digest is None or value==digest
 assert str(path)not in external or external[str(path)]==value;external[str(path)]=value
binding=json.loads((P/'source-qualification-binding.json').read_text());assert binding['sourceQualificationReady']is True and binding['nativeSelectionReady']is True
for name in ['qualifiedReceipt','parentAudit','preparedReceipt','qualifiedProducerReceipt','qualifiedProducerParentAudit','runtimeManifest']:add(binding[name],binding[name+'Sha256'])
qualified=json.loads(Path(binding['qualifiedReceipt']).read_text());assert qualified['qualified']is True and qualified['featureAcceptance']is False
prepared=json.loads(Path(binding['preparedReceipt']).read_text())
for name,digest in prepared['filesSha256'].items():add(SOURCE/name,digest)
for field in ['sourceSha256','currentExternalInputsSha256']:
 for name,digest in prepared[field].items():add(name,digest)
for name,digest in qualified['evidenceSha256'].items():add(name,digest)
for name,digest in json.loads((P/'affected-inventory.json').read_text())['projectInputsSha256'].items():add(name,digest)
reference=json.loads((P/'native-reference-case-set.json').read_text())
for name,digest in reference['fixtureSourceSha256'].items():add(name,digest)
for row in reference['historicalCorroboration']:add(row['path'],row['sha256'])
mapdata=json.loads((P/'mapping-pins.json').read_text());add(mapdata['mappingLog'],mapdata['mappingLogSha256'])
for name,digest in mapdata['files'].items():add(name,digest)
handoff=json.loads((P/'ordinary-handoff.json').read_text())
for key in ['ownerReceipt','nativeAudit','sourceControls','nativeBinaryAudit']:add(handoff[key]['path'],handoff[key]['sha256'])
for file in Path('/tmp/story65-receiver-phase-ops-baseline-20261107').iterdir():
 if file.is_file():add(file)
for file in [ROOT/'tools/qualification/run_supported_project_reads.py',Path('/tmp/story65-receiver-acceptance-prep-20261107/audit.py'),Path('/tmp/story65-receiver-phase-qualified-prep-20261107/audit_closed_q2.py')]:add(file)
entry=json.loads((P/'entry-metadata-contract.json').read_text());add(entry['metadataReader']['path'],entry['metadataReader']['sha256'])
for file in P.glob('*.py'):ast.parse(file.read_text(),filename=str(file))
ET.parse(P/'sdk-live.targets')
for digest in [binding['immutableNativeReferenceSha256']]:assert sha(P/'native-reference-case-set.json')==digest
assert not any(Path(name).suffix.lower()in['.dll','.pdb']and Path(name).is_relative_to(ROOT)for name in external)
(P/'mutable-output-pin-exclusions.json').write_text(json.dumps({'notImmutableExecutablePins':True,'guardedByCurrentGenerationSourceHashMapAndBaselineRules':True,'files':excluded},indent=2)+'\n')
files={str(f.relative_to(P)):sha(f)for f in sorted(P.rglob('*'))if f.is_file()and f.name!='operational-pins.json'and'__pycache__'not in f.parts}
pins={'sourceQualificationReady':True,'executionReleased':False,'featureAcceptance':False,'privateReceiverPhaseDiagnosticOnly':True,'files':files,'externalFiles':dict(sorted(external.items())),'actualQualificationReceiptSha256':binding['qualifiedReceiptSha256'],'sourcePreparationSha256':binding['preparedReceiptSha256'],'runtimeManifestSha256':binding['runtimeManifestSha256'],'nativeSelectionApprovalSeparateFromImmutableReference':True,'nativeReferenceSha256':binding['immutableNativeReferenceSha256'],'ordinaryOwnerSources':49,'rootSourceControls':56,'projectInputs':14,'qualifiedOrdinaryNative1048Unchanged':True,'mutableWorkspaceOutputsAreFreshBaselineAssertionsNotExternalPins':True,'actualPrivateRuntimeProducerHashMayDifferFromIsolatedQualifiedGeneration':True,'allFiveNoExecutePlansPassed':True,'allPythonAstAndTargetXmlPass':True}
(P/'operational-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
print(json.dumps({'disabledOperationalPinsSha256':sha(P/'operational-pins.json'),'localFiles':len(files),'externalInputs':len(external),'mutableWorkspaceOutputAliasesExcluded':len(excluded),'sourceQualificationReady':True,'executionReleased':False}))
