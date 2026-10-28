"""Bind retained candidates, selected committed sources, producer inputs and executed outputs."""
from run import *
import shutil
import xml.etree.ElementTree as ET

workspace=WORK/'projects'
files=['module/hexalith.module-manifest.v2.json','module/README.md','src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj']
assert not (EVIDENCE/'reviewed-candidate-source').exists()
for relative in files:
    target=EVIDENCE/'reviewed-candidate-source'/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(workspace/relative,target)
patch=git(workspace,'diff','--','src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj')
for relative in files[:2]:
    result=subprocess.run(['git','diff','--no-index','--','/dev/null',relative],cwd=workspace,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    assert result.returncode==1
    patch+=result.stdout
(EVIDENCE/'projects-root-enrollment-reviewed.patch').write_bytes(patch)
source={}
repos=[workspace]+[workspace/line.split(' ',1)[1] for line in git(workspace,'config','--file','.gitmodules','--get-regexp',r'^submodule\..*\.path$').decode().splitlines()]
for repo in repos:
    paths=git(repo,'ls-files','-z').split(b'\0')
    source[str(repo.relative_to(workspace))]={'head':git(repo,'rev-parse','HEAD').decode().strip(),'tracked_hashes':{os.fsdecode(name):digest(repo/os.fsdecode(name)) for name in paths if name and (repo/os.fsdecode(name)).is_file()},'status':git(repo,'status','--porcelain=v1','--untracked-files=all').decode()}
(EVIDENCE/'reviewed-source-inventory.json').write_text(json.dumps(source,indent=2)+'\n')
(EVIDENCE/'reviewed-candidate-hashes.json').write_text(json.dumps({name:digest(workspace/name) for name in files},indent=2)+'\n')
mapping=WORK/'source-reviewed/source-mapping'
(EVIDENCE/'reviewed-generated-input-hashes.json').write_text(json.dumps({str(p.relative_to(mapping)):digest(p) for p in sorted(mapping.rglob('*')) if p.is_file()},indent=2)+'\n')
catalog=json.loads((mapping/'catalog-versions.json').read_text())
raw=git(workspace/'references/Hexalith.Builds','show',catalog['CatalogCommit']+':Props/Directory.Packages.props')
(EVIDENCE/'committed-catalog.props').write_bytes(raw)
emitted=(mapping/'pinned-tree/Props/Directory.Packages.props').read_bytes()
assert raw.decode('utf-8-sig').encode()==emitted
(EVIDENCE/'catalog-binding.json').write_text(json.dumps({'commit':catalog['CatalogCommit'],'raw_sha256':hashlib.sha256(raw).hexdigest(),'materialized_sha256':hashlib.sha256(emitted).hexdigest(),'utf8_bom_removed':raw.startswith(b'\xef\xbb\xbf'),'otherwise_equal':True},indent=2)+'\n')
outputs={}
for layer in ['Contracts','Server']:
    out=workspace/f'tests/Hexalith.Projects.{layer}.Tests/bin/Debug/net10.0'
    outputs[layer]={str(p.relative_to(out)):digest(p) for p in sorted(out.rglob('*')) if p.is_file()}
(EVIDENCE/'reviewed-executable-inventory.json').write_text(json.dumps(outputs,indent=2)+'\n')
results=[]
for filename in ['reviewed-projects-contracts-tests.xml','reviewed-projects-server-tests.xml','reviewed-gateway-store-tests.xml']:
    xml=ET.parse(EVIDENCE/filename)
    assemblies=xml.findall('assembly')
    assert len(assemblies)==1
    assembly=assemblies[0]
    for k in ['failed','errors','skipped','not-run']: assert assembly.get(k)=='0'
    tests=assembly.findall('.//test');assert len(tests)==int(assembly.get('total'))>0
    results.append({'xml':filename,'assembly':assembly.attrib,'cases':[t.attrib for t in tests]})
(EVIDENCE/'reviewed-test-results.json').write_text(json.dumps(results,indent=2)+'\n')
run('reviewed-candidate-patch-check',['git','apply','--check',str(EVIDENCE/'projects-root-enrollment-reviewed.patch')],ROOT)
run('reviewed-isolated-diff-check',['git','diff','--check'],workspace)
# Original strict audit failure is retained; concurrent state is not reset or rebaselined.
