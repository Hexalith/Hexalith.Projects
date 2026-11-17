"""Materialize two closed owner proposals; verify trees without module execution."""
from pathlib import Path
import hashlib,importlib.util,json,os,subprocess,sys
OUT=Path(__file__).resolve().parent
state=json.loads((OUT/'prerequisite-source-workspace.json').read_text());WORK=Path(state['work'])
SPEC=importlib.util.spec_from_file_location('runtime',WORK/'tools/delivery_runtime.py');rt=importlib.util.module_from_spec(SPEC);sys.modules[SPEC.name]=rt;SPEC.loader.exec_module(rt)
SPEC=importlib.util.spec_from_file_location('publisher',WORK/'tools/publish_contracts_prerequisite.py');pub=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(pub)
source=json.loads((OUT/'publisher-owner-source-publication.json').read_text())['source']
DEST=OUT/'delivery-proposals-attempt-1';DEST.mkdir(exist_ok=False)
transport=rt.Transport()
for profile,repository in [('eventstore','Hexalith.EventStore'),('projects','Hexalith.Projects')]:
 name=profile+'-publisher';directory=DEST/name;directory.mkdir()
 target=rt.Git(Path(os.environ['TMPDIR'])/(profile+'.git'));target.init();base=transport.module(target,repository)
 reviewed=OUT/'publisher-workflows'/profile;assert (reviewed/'base.txt').read_text().strip()==base,'Target main advanced; regenerate workflow from its new exact source'
 original=target.blob(base,'.github/workflows/release.yml');assert original==(reviewed/'before.yml').read_bytes()
 revised=(reviewed/'release.yml').read_bytes();assert source.encode() in revised and b'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb' not in revised
 original_tree=target.resolve(base+'^{tree}');before=target.items(base);target.run('read-tree',original_tree)
 oid=rt.sha(target.run('hash-object','-w','--stdin',data=revised).decode().strip());target.run('update-index','--add','--cacheinfo','100644,'+oid+',.github/workflows/release.yml')
 tree=rt.sha(target.run('write-tree','--missing-ok').decode().strip());after=target.items(tree)
 changed={p for p in before.keys()|after.keys() if before.get(p)!=after.get(p)};assert changed=={'.github/workflows/release.yml'}
 patch=target.run('diff','--no-ext-diff','--no-textconv','--binary','--full-index',original_tree,tree)
 package,path,manifest_hash=pub.qualification(WORK,profile)
 logs={
  'scope-preservation':json.dumps({'status':'passed','base':base,'base_tree':original_tree,'result_tree':tree,'only_changed_path':'.github/workflows/release.yml','original_sha256':pub.digest(original),'result_sha256':pub.digest(revised),'normal_full_release_preserved':'generator exact round-trip assertion passed','publisher_source':source},indent=2).encode()+b'\n',
  'package-qualification':json.dumps({'status':'passed','publisher_source':source,'manifest_sha256':manifest_hash,'fixed_package':package,'qualified_receipts':'all exact archive/proof hashes and mandatory package-only gate values checked by immutable publisher qualification()'},indent=2).encode()+b'\n'
 }
 v=subprocess.run(['actionlint','-shellcheck=',str(reviewed/'release.yml')],capture_output=True,timeout=60);assert v.returncode==0
 logs['actionlint']=json.dumps({'status':'passed','command':['actionlint','-shellcheck=','release.yml'],'input_sha256':pub.digest(revised),'exit':v.returncode,'output':(v.stdout+v.stderr).decode()},indent=2).encode()+b'\n'
 logs['behavioral-controls']=(OUT/'publisher-private-workspace-tests.log').read_bytes()+b'\n'+(OUT/'owner-controller-final-tests.log').read_bytes()
 assert b'Ran 36 tests' in logs['behavioral-controls'] and b'Ran 42 tests' in logs['behavioral-controls'] and logs['behavioral-controls'].rstrip().endswith(b'OK')
 proof={'schema':'hexalith.contracts-prerequisites-validation.v1','status':'passed','base_commit':base,'base_tree':original_tree,'result_tree':tree,'patch_sha256':pub.digest(patch),'checks':{check:{'status':'passed','log_sha256':pub.digest(data)} for check,data in logs.items()}}
 proof_bytes=(json.dumps(proof,indent=2)+'\n').encode()
 value={'schema':'hexalith.contracts-prerequisites.v1','repository':repository,'base_commit':base,'base_tree':original_tree,'result_tree':tree,'patch_sha256':pub.digest(patch),'before':{'.github/workflows/release.yml':list(before['.github/workflows/release.yml'])},'after':{'.github/workflows/release.yml':list(after['.github/workflows/release.yml'])},'validation_sha256':pub.digest(proof_bytes),'pre_push_gate':'reviewed-tree-owner-authorized'}
 (directory/(name+'.json')).write_text(json.dumps(value,indent=2)+'\n');(directory/(name+'.patch')).write_bytes(patch);(directory/(name+'-validation.json')).write_bytes(proof_bytes)
 for check,data in logs.items():(directory/(name+'-'+check+'.log')).write_bytes(data)
 print(name,base,tree,flush=True)
