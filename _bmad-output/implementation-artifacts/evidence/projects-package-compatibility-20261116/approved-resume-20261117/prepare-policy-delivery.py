from pathlib import Path
import hashlib,importlib.util,json,os,subprocess,sys
OUT=Path(__file__).resolve().parent;state=json.loads((OUT/'active-owner-source-workspace.json').read_text());WORK=Path(state['work'])
SPEC=importlib.util.spec_from_file_location('runtime',WORK/'tools/delivery_runtime.py');rt=importlib.util.module_from_spec(SPEC);sys.modules[SPEC.name]=rt;SPEC.loader.exec_module(rt)
SPEC=importlib.util.spec_from_file_location('publisher',WORK/'tools/publish_contracts_prerequisite.py');pub=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(pub)
for p in ['tools/deliver_contracts_prerequisites.py','tools/tests/test_deliver_contracts_prerequisites.py','.github/workflows/deliver-contracts-prerequisites.yml']:assert (WORK/p).read_bytes()==(OUT/'owner-implementation'/p).read_bytes()
DEST=OUT/'delivery-proposals-policy-attempt-1';DEST.mkdir(exist_ok=False);name='projects-prerequisite-policy';proof=json.loads((OUT/'projects-policy-qualification.json').read_text());transport=rt.Transport();target=rt.Git(Path(os.environ['TMPDIR'])/'projects.git');target.init();base=transport.module(target,'Hexalith.Projects');assert base==proof['base']
base_tree=target.resolve(base+'^{tree}');assert base_tree==proof['base_tree'];before=target.items(base);target.run('read-tree',base_tree);patch=(OUT/'projects-policy.patch').read_bytes();assert pub.digest(patch)==proof['patch_sha256'];target.run('apply','--cached','--whitespace=nowarn',data=patch);tree=rt.sha(target.run('write-tree','--missing-ok').decode().strip());assert tree==proof['result_tree'];after=target.items(tree)
changed={p for p in before.keys()|after.keys() if before.get(p)!=after.get(p)};assert changed==set(proof['paths']);assert all(before[p]==after[p] for p in before if before[p][0]=='160000')
package_receipts=[]
for profile in ['eventstore','projects']:
 package,path,manifest=pub.qualification(WORK,profile);package_receipts.append({'profile':profile,'source':package['source'],'archive_sha256':package['sha256'],'manifest_sha256':manifest})
workflow=target.blob(base,'.github/workflows/release.yml');test_workflow=Path(os.environ['TMPDIR'])/'release.yml';test_workflow.write_bytes(workflow)
logs={'scope-preservation':(json.dumps(dict(proof,complete_tree_and_gitlinks_verified=True),indent=2)+'\n').encode(),'package-qualification':(json.dumps({'status':'passed','scope':'package-only prerequisite qualification; no pin changes or public availability claim','fixed_publisher_source':'264e5de34341187a0ad4724dd6a39248b9cb9b58','qualified_packages':package_receipts},indent=2)+'\n').encode()}
raw=(OUT/'projects-policy-qualification.log').read_bytes();assert b'Ran 22 tests' in raw and raw.rstrip().endswith(b'OK');other=(OUT/'owner-implementation/logs/delivery-policy-regression.log').read_bytes();assert b'Ran 44 tests' in other and other.rstrip().endswith(b'OK');logs['behavioral-controls']=raw+b'\n'+other+b'\n'+(OUT/'projects-policy-gate.log').read_bytes()
action_results=[]
for file in [test_workflow,WORK/'.github/workflows/deliver-contracts-prerequisites.yml']:
 v=subprocess.run(['actionlint','-shellcheck=',str(file)],capture_output=True,timeout=60);assert v.returncode==0;action_results.append({'path':file.name,'sha256':pub.digest(file.read_bytes()),'exit':v.returncode,'output':(v.stdout+v.stderr).decode()})
logs['actionlint']=(json.dumps({'status':'passed','results':action_results},indent=2)+'\n').encode()
validation={'schema':'hexalith.contracts-prerequisites-validation.v1','status':'passed','base_commit':base,'base_tree':base_tree,'result_tree':tree,'patch_sha256':pub.digest(patch),'checks':{check:{'status':'passed','log_sha256':pub.digest(raw)} for check,raw in logs.items()}}
validation_raw=(json.dumps(validation,indent=2)+'\n').encode();manifest={'schema':'hexalith.contracts-prerequisites.v1','repository':'Hexalith.Projects','base_commit':base,'base_tree':base_tree,'result_tree':tree,'patch_sha256':pub.digest(patch),'before':{p:list(before[p]) for p in sorted(changed)},'after':{p:list(after[p]) for p in sorted(changed)},'validation_sha256':pub.digest(validation_raw),'pre_push_gate':'reviewed-tree-owner-authorized'}
(DEST/(name+'.json')).write_text(json.dumps(manifest,indent=2)+'\n');(DEST/(name+'.patch')).write_bytes(patch);(DEST/(name+'-validation.json')).write_bytes(validation_raw)
for check,data in logs.items():(DEST/(name+'-'+check+'.log')).write_bytes(data)
print(json.dumps({'profile':name,'base':base,'tree':tree,'paths':sorted(changed)}))
