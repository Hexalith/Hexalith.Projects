from pathlib import Path
import ast,hashlib,json,xml.etree.ElementTree as ET
P=Path(__file__).resolve().parent
R=Path('/home/administrator/projects/hexalith/projects')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
write=lambda n,d:(P/n).write_text(json.dumps(d,indent=2)+'\n')
source_path=Path('/tmp/story65-canonical-hash-native-20261108/source-controls.json')
source=json.loads(source_path.read_text())['sourceFilesSha256']
assert len(source)==65
binding=json.loads(Path('/tmp/story65-canonical-current-gateway-cpu-prep-20261108/watcher/ordinary-binding.json').read_text())
app={k:sha(Path(k) if Path(k).is_absolute() else R/k) for k in binding['currentApp147Sha256']}
assert len(app)==147
inputs={}
def add(path,expected=None):
 p=Path(path);p=p if p.is_absolute() else R/p
 assert p.is_file() and not p.is_symlink() and all(not a.is_symlink() for a in p.parents)
 h=sha(p)
 if expected is not None:assert h==expected,str(p)
 inputs[str(p)]=h
for rel,h in source.items():add(rel,h)
for rel,h in app.items():add(rel,h)
add(source_path)
for rel,h in binding['producerFilesSha256'].items():add(rel,h)
project=ET.fromstring((P/'differential/Differential.csproj').read_text())
assert not project.findall('.//PackageReference') and not project.findall('.//ProjectReference')
refs=[]
for item in project.findall('.//Reference'):
 p=Path(item.findtext('HintPath'));add(p);refs.append(str(p))
 assert p.name!='Hexalith.Projects.Server.Tests.dll'
for tag,attr in [('Compile','Include'),('Analyzer','Include')]:
 for item in project.findall('.//'+tag):
  p=Path(item.attrib[attr])
  if p.is_absolute():add(p)
# Exact ordinary compiled oracle, with its symbols, not a historical test-copy fallback.
oracle=R/'src/Hexalith.Projects.Server/bin/Debug/net10.0/Hexalith.Projects.Server.dll'
assert str(oracle) in refs
add(oracle.with_suffix('.pdb'))
assert sha(P/'CaptureCounters.cs')=='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
assert sha(P/'AdvisoryWaveBootstrap.cs')=='8f5836d1ff4dabb4c5fe819ae762dfff0748b5d792f984ae1adf45af28f45375'
assert sha(P/'ProjectAdvisoryQueryExecutor.original.cs')=='ac34b0c5c86c8a85ddf8591f73ddb02d32dc3a63db47a206b42688f2c696df8d'
for p in P.glob('*.py'):ast.parse(p.read_text())
normal=json.loads((P/'normalization.json').read_text());reverse=(P/'ProjectAdvisoryQueryExecutor.private.cs').read_bytes().decode()
for edit in reversed(normal['edits']):
 assert reverse.count(edit['new'])==1
 reverse=reverse.replace(edit['new'],edit['old'],1)
assert reverse.encode()==(P/'ProjectAdvisoryQueryExecutor.original.cs').read_bytes()
write('source-proof.json',{'originalExecutorSha256':sha(P/'ProjectAdvisoryQueryExecutor.original.cs'),'privateExecutorSha256':sha(P/'ProjectAdvisoryQueryExecutor.private.cs'),'differentialCopySha256':sha(P/'InstrumentedProjectAdvisoryQueryExecutor.cs'),'reverse24EditsRestoresOriginalBytes':True,'helperSha256':sha(P/'CaptureCounters.cs'),'bootstrapSha256':sha(P/'AdvisoryWaveBootstrap.cs'),'currentOraclePath':str(oracle),'currentOracleSha256':sha(oracle),'source65Unchanged':True,'application147SnapshotOnly':True,'noSourceBuildLoadRuntimeHostOrWindowExecuted':True,'testOnlyDifference':'class-declaration rename plus primitive-terminal-reference property and assignment, absent from application candidate','retainedDriverSource':str(Path('/tmp/story65-advisory-wave-prep-20261107/differential')),'newProofsNotPassedYet':True})
write('input-baseline.json',{'source65Sha256':source,'application147Sha256':app,'ordinaryCore8Sha256':binding['producerFilesSha256'],'oraclePath':str(oracle),'oracleSha256':sha(oracle),'rawReferences':refs,'noOldSdk6947Fallback':True})
files={str(p.relative_to(P)):sha(p) for p in sorted(P.rglob('*')) if p.is_file() and p.name!='preparation-receipt.json' and not any(x in p.parts for x in ['bin','obj','__pycache__'])}
receipt={'compileReleased':False,'executionReleased':False,'sourceQualificationReady':False,'preparedFiles':len(files),'filesSha256':files,'inputsSha256':inputs,'sourceSha256':source,'app147Sha256':app,'compiledInputsSha256':{},'compiledOutputsRequireSeparateRootBinding':True,'oraclePath':str(oracle),'oracleSha256':sha(oracle),'rawReferenceCount':len(refs),'plannedBehavioralCases':49,'plannedBehavioralComparisons':196,'plannedFreshFlowCanaries':3,'plannedProtocolCases':4,'plannedControlledProcesses':23,'actualSyntheticRuns':0,'applicationBuilds':0,'hostsStarted':0,'nativeAcceptanceClaim':False,'featureAcceptance':False}
write('preparation-receipt.json',receipt)
for path,h in inputs.items():assert sha(Path(path))==h
for path,h in files.items():assert sha(P/path)==h
print(json.dumps({'preparationSha256':sha(P/'preparation-receipt.json'),'files':len(files),'externalInputs':len(inputs),'source':len(source),'app':len(app),'rawReferences':len(refs),'allFlagsFalse':True}))
