"""Bind clean selected sources, binaries and producer-pinned catalog bytes."""
from run import *
import xml.etree.ElementTree as ET

def capture(label, args, cwd):
    return run('capture-v3-'+label, args, cwd)

workspace = WORK/'projects'
selection = json.loads((EVIDENCE/'selected-inputs.json').read_text())
repos = [('projects',workspace,selection['head'])] + [(Path(i['path']).name,workspace/i['path'],i['selected']) for i in selection['inputs']]
sources = {}
for label, repo, expected in repos:
    assert capture(label+'-final-source-head',['git','rev-parse','HEAD'],repo).strip() == expected
    assert not capture(label+'-final-unstaged-source',['git','diff','--ignore-submodules=all','--name-only'],repo)
    if repo != workspace:
        assert not capture(label+'-final-staged-source',['git','diff','--cached','--name-only'],repo)
        assert not list((repo/'references').glob('*/.git'))
    files = git(repo,'ls-files','-z','--','src','tests','test','*.props','*.targets','global.json','.editorconfig','.gitattributes','.config/dotnet-tools.json','module/hexalith.module-manifest.v2.json','hexalith-platform.json').split(b'\0')
    for raw in files:
        if not raw:
            continue
        relative = Path(os.fsdecode(raw))
        p = repo/relative
        if relative.parts[0] in {'_bmad-output','docs','artifacts','.agents','.claude','.github','references'}:
            continue
        # Record source/configuration rather than checked-in native binaries or historical receipts.
        if p.is_file() and (p.suffix in {'.cs','.csproj','.props','.targets','.ruleset','.globalconfig'} or p.name in {'global.json','.editorconfig','.gitattributes','dotnet-tools.json','hexalith.module-manifest.v2.json','hexalith-platform.json','NuGet.Config','nuget.config'}):
            sources[str(p.relative_to(workspace))] = digest(p)
(EVIDENCE/'selected-source-hashes.json').write_text(json.dumps(sources,indent=2)+'\n')

catalog = capture('platform-owned-exact-catalog',['git','show','51a71cb1cfe860d5fc135eb3b8521fd80c413810:Props/Directory.Packages.props'],workspace/'references/Hexalith.Builds')
actual = (EVIDENCE/'platform-owned-exact-catalog.stdout').read_bytes()
assert hashlib.sha256(actual).hexdigest() == '95622edbc970beea37bf22c2fbd5bfe43cf6e69f2087a8a0f6fac0e0b3097ef6'
assert actual.startswith(b'\xef\xbb\xbf')
assert actual[3:] == (EVIDENCE/'generated-source-mapping/pinned-tree/Props/Directory.Packages.props').read_bytes()

results = {}
for label in ['contracts','server']:
    xml = ET.parse(EVIDENCE/('final-'+label+'-tests.xml')).getroot()
    tests = xml.findall('.//test')
    assert len(tests) == (10 if label == 'contracts' else 243)
    assert all(t.attrib['result']=='Pass' for t in tests)
    assert len({t.attrib['id'] for t in tests}) == len(tests)
    assemblies = xml.findall('assembly')
    results[label] = dict(total=len(tests),classes=sorted({t.attrib['type'] for t in tests}),assemblies=[a.attrib for a in assemblies])
    for a in assemblies:
        p=Path(a.attrib['name'])
        results[label]['binary_sha256']=digest(p)
(EVIDENCE/'test-results.json').write_text(json.dumps(results,indent=2)+'\n')
run('reviewed-original-preservation-audit',['python3',str(EVIDENCE/'run.py'),'audit'])
print('Bound',len(sources),'source/configuration files; 253 unique passing cases')
