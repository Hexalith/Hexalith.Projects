from pathlib import Path
import ast,difflib,hashlib,json,sys,xml.etree.ElementTree as ET
sys.dont_write_bytecode=True
from ops_common import ROOT,PREP,SOURCE_PREP,sha,sources
old=Path('/tmp/story65-advisory-wave-ops-prep-20261107')
files=['ops_common.py','build_restore.py','native_qualification.py','launch_managed.py','supervise_managed.py','run_and_restore.py']
diff=[]
for name in files:
 diff+=list(difflib.unified_diff((old/name).read_text().splitlines(keepends=True),(PREP/name).read_text().splitlines(keepends=True),fromfile='qualified-old/'+name,tofile='current/'+name))
diff+=list(difflib.unified_diff((old/'advisory-wave.targets').read_text().splitlines(keepends=True),(PREP/'advisory-terminal.targets').read_text().splitlines(keepends=True),fromfile='qualified-old/advisory-wave.targets',tofile='current/advisory-terminal.targets'))
for name in ['advisory-compiler-observer.targets','validate_closed_terminal.py','writer_fd_proof.py']:
 diff+=list(difflib.unified_diff([],(PREP/name).read_text().splitlines(keepends=True),fromfile='/dev/null',tofile='current/'+name))
(PREP/'adaptation.diff').write_text(''.join(diff))
for p in PREP.glob('*.py'):ast.parse(p.read_text())
for p in PREP.glob('*.targets'):ET.parse(p)
source=sources();binding=json.loads((PREP/'source-qualification-binding.json').read_text());q=json.loads(Path(binding['qualificationPath']).read_text())
inv=json.loads((PREP/'affected-inventory.json').read_text());external={}
def add(p,h=None):
 p=Path(p);assert p.suffix.lower()not in ['.dll','.pdb'],'Mutable binary pins are scoped inside baseline/namedguards only'
 actual=sha(p)
 if h is not None:assert actual==h
 external[str(p)]=actual
add(binding['qualificationPath'],binding['qualificationSha256'])
for p,h in q['bindingsSha256'].items():add(p,h)
prep=json.loads((SOURCE_PREP/'preparation-receipt.json').read_text())
for p,h in prep['filesSha256'].items():add(SOURCE_PREP/p,h)
controls=json.loads(Path(binding['currentSourceControls']).read_text())['sourceFilesSha256']
add(binding['currentSourceControls'],binding['currentSourceControlsSha256'])
for p,h in controls.items():add(ROOT/p,h)
for p,h in inv['sourceProjectSha256'].items():add(ROOT/p,h)
reference=json.loads((PREP/'native-reference-case-set.json').read_text());add(reference['referenceXml'],reference['referenceXmlSha256'])
for p,h in reference['sourceFilesSha256'].items():add(ROOT/p,h)
mapping=json.loads((PREP/'mapping-pins.json').read_text());add(mapping['mappingLog'],mapping['mappingLogSha256'])
for p,h in mapping['files'].items():add(p,h)
for p in ['tools/qualification/run_supported_project_reads.py','tools/qualification/supported_browser_reads.py','tests/e2e/package.json','.gitmodules']:
 add(ROOT/p)
for p in ['/tmp/story65-canonical-hash-acceptance-prep-20261108/audit.py','/tmp/story65-canonical-hash-native-audit-20261108/independent-native-audit.json']:
 add(p)
spec=ROOT/'_bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md';add(spec,binding['sourceSpecSha256'])
# All current binary bytes are checked here before sealing. They remain scoped
# baseline data, not immutable external pins that would block our own restoration.
rows=inv['knownServerCopies']+inv['sdk137']+[r for group in inv['independentModules'].values()for r in group['rows']]
for row in rows:assert sha(ROOT/row['path'])==row['sha256']and not row['privateMarker']
for mapping in [inv['core8Sha256'],inv['app147Sha256'],inv['additional2Sha256']]:
 for p,h in mapping.items():assert sha(ROOT/p if not Path(p).is_absolute()else p)==h
local={str(p.relative_to(PREP)):sha(p)for p in sorted(PREP.iterdir())if p.is_file()and p.name!='operational-pins.json'}
pins={'executionReleased':False,'sourceQualificationReady':True,'qualificationSha256':binding['qualificationSha256'],'source65':True,'ordinaryNative1076Retained':True,'actualPrivateNative139NotAdditionalCases':True,'files':local,'externalFiles':external,'mutableBinaryExternalPins':False,'baselineInventoryFrozenDoNotRegenerateAfterPrivateBuild':True,'tenPrivatePrimaryBuilds':True,'mandatoryOrdinaryElevenMinimumPlusDynamicUnion':True,'noNewRuntimeEnvironmentConfigLoggerPrivacyOrDeadlineChanges':True,'onlyExistingAspireNoBuildSubstitution':True,'physicalWriterProofOnlyOneObservedPoint':True,'noFeatureAcceptanceClaim':True,'pythonAstAndTwoXmlParseOnly':True,'noApplicationBuildHostWindowOrSinkAccessDuringPreparation':True}
(PREP/'operational-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
assert all(sha(PREP/p)==h for p,h in local.items())and all(sha(p)==h for p,h in external.items())
print(json.dumps({'pinsSha256':sha(PREP/'operational-pins.json'),'local':len(local),'external':len(external),'diffSha256':sha(PREP/'adaptation.diff'),'allCurrent':True,'executionReleased':False}))
