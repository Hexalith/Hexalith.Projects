from pathlib import Path
from xml.etree import ElementTree as ET
import hashlib,json,re,subprocess,sys
ROOT=Path('/home/administrator/projects/hexalith/projects');OUT=Path('/tmp/story65-memo-rw-native-20261107')
def sha(b):return hashlib.sha256(b).hexdigest()
commands=json.loads((OUT/'native-commands.json').read_text());assert commands['projectsPassingCases']==626
paths=json.loads((ROOT/'_bmad-output/implementation-artifacts/evidence/story-6-5-activation-eligibility-20261107/six-host-sdk-copies.json').read_text())['sourceHostCopies']
dirs=[(ROOT/x['serviceDefaultsPath']).parent for x in paths]+[ROOT/'tests/Hexalith.Projects.Integration.Tests/bin/Debug/net10.0',ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0']
forbidden=[b'Story65Timing',b'Story65PrivateDiagnostic',b'Hexalith.PrivateOwnerTiming',b'PrivateFocusedTiming',b'TimingHexalith',b'Hexalith.PrivateCaptureCounter',b'InvocationTransportBootstrap',b'GatewayControllerBootstrap',b'CaptureCounters.cs']
files=[]
for directory in dirs:
 for p in sorted(directory.glob('Hexalith.*.dll')):
  b=p.read_bytes();bad=[x.decode() for x in forbidden if x in b];assert not bad,(p,bad)
  files.append({'path':str(p.relative_to(ROOT)),'sha256':sha(b),'privateDiagnosticMarkersAbsent':True})
sdk=[x for x in files if x['path'].endswith('/Hexalith.EventStore.ServiceDefaults.dll')];assert len(sdk)==8 and len({x['sha256'] for x in sdk})==1
for item in sdk:
 b=(ROOT/item['path']).read_bytes()
 for marker in (b'RouteCatalogImageDigestMemo',b'RouteCatalogAdmissionGate',b'RouteCatalogFreshStreamImageDigest',b'RouteCatalogImageBuffers',b'BeginPrivateReceiverImageScope',b'AttachPrivateImageOwner',b'TechnicalHttpParsedTemplate',b'ReaderWriterLockSlim'):assert marker in b,(item['path'],marker)
owner_proof=json.loads(Path(sys.argv[1]).read_text());source=owner_proof['sourceSha256']
for p,h in source.items():assert sha((ROOT/'references/Hexalith.EventStore'/p).read_bytes())==h,p
(OUT/'binary-source-audit.json').write_text(json.dumps({'diagnosticOverridePresent':False,'featureAcceptance':False,'checkedAssemblyCopies':len(files),'matchingSdkCopies':len(sdk),'ownerSourceHashCount':len(source),'ownerSourceHashes':source,'files':files},indent=2)+'\n')
xml=OUT/'ui-tests.xml';tests=list(ET.parse(xml).getroot().iter('test'));matrix=json.loads(Path('/tmp/story65-owner-repair-native-20261107/matrix-audit.json').read_text())
for row in matrix['rows']:
 for t in row['coveringTests']:
  matches=[x for x in tests if all(x.get(k)==t[k] for k in ('type','method','name'))];assert len(matches)==1 and matches[0].get('result')=='Pass',t
matrix.update(scope='Current ordinary source matrix audit. Separate managed live acceptance remains required.',xml='ui-tests.xml',xmlSha256=sha(xml.read_bytes()),featureAcceptance=False)
(OUT/'matrix-audit.json').write_text(json.dumps(matrix,indent=2)+'\n')
baseline=json.loads(Path('/tmp/story-6-5-parent-baseline-ubp3ntoh.json').read_text());spec=ROOT/'_bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md'
frozen=sha(re.search(rb'<frozen-after-approval.*?</frozen-after-approval>',spec.read_bytes(),re.S).group());assert frozen==baseline['frozen_sha256']
sprint=ROOT/'_bmad-output/implementation-artifacts/sprint-status.yaml';assert sha(sprint.read_bytes())==baseline['sprint_sha256']
index=sha(subprocess.check_output(['git','diff','--cached','--binary'],cwd=ROOT));assert index==baseline['index_sha256']
(OUT/'parent-guards.json').write_text(json.dumps({'frozenSha256':frozen,'sprintSha256':sha(sprint.read_bytes()),'indexSha256':index,'baseline':baseline},indent=2)+'\n')
owner=ROOT/'references/Hexalith.EventStore';diff=subprocess.check_output(['git','diff','--binary'],cwd=owner)
for p in subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z'],cwd=owner).decode().split('\0'):
 if not p:continue
 result=subprocess.run(['git','diff','--no-index','--binary','/dev/null',p],cwd=owner,stdout=subprocess.PIPE);assert result.returncode==1;diff+=result.stdout
p=Path('/tmp/story65-memo-rw-current-owner-20261107.diff');p.write_bytes(diff)
print(json.dumps({'nativePassed':626+owner_proof['uniquePassingCases'],'binaryCopiesClean':len(files),'matchingSdkCopies':len(sdk),'sourceHashesMatch':len(source),'matrixRows':len(matrix['rows']),'matrixCases':matrix['executedCoveringCases'],'ownerDiff':str(p),'ownerDiffSha256':sha(diff),'guardsUnchanged':True}))
