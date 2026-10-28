"""Read-only checks of retained evidence; --live additionally checks the isolated workspace."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode=True
if not __debug__: raise RuntimeError('Optimized Python cannot run evidence assertions')
p=Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('--live',action='store_true');args=parser.parse_args()
def read(name):return json.loads((p/name).read_text())
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def git(repo,*argv):return subprocess.check_output(['git',*argv],cwd=repo,env={**os.environ,'GIT_OPTIONAL_LOCKS':'0'})
if (p/'SHA256SUMS.json').exists():
 sealed=read('SHA256SUMS.json')
 actual_names={str(f.relative_to(p)) for f in p.rglob('*') if f.is_file() and f.name!='SHA256SUMS.json'}
 assert actual_names==set(sealed),'Retained file inventory differs from seal'
 for name,expected in sealed.items():assert sha(p/name)==expected,name
selected=read('selected-inputs.json');workspace=Path(selected['workspace'])
candidate=read('reviewed-candidate-hashes.json')
for name,expected in candidate.items():assert sha(p/'reviewed-candidate-source'/name)==expected,name
for name,expected in read('reviewed-generated-input-hashes.json').items():assert sha(p/'reviewed-generated-source-mapping'/name)==expected,name
prepared=read('reviewed-source-preparation.stdout');env=prepared['buildPreparation']['environment']
assert prepared['status']=='prepared' and env['Configuration']=='Debug' and len(env)==7
mapping=read('reviewed-generated-source-mapping/mapping.json')
assert mapping['ActiveModule']=='Hexalith.Projects'
commands=read('commands.json');records={r['label']:r for r in commands};assert len(records)==len(commands)
for label in ['closure-manifest-validation','reviewed-manifest-validation','reviewed-source-preparation','reviewed-projects-contracts-build','reviewed-projects-server-build','reviewed-projects-contracts-tests','reviewed-projects-server-tests','reviewed-gateway-store-tests']:
 assert records[label]['exit_code']==0,label
for label in ['reviewed-projects-contracts-build','reviewed-projects-server-build']:assert all(records[label]['environment'][key]==value for key,value in env.items()),label
assert records['reviewed-package-preparation']['exit_code']==2
assert {d['ruleId'] for d in read('reviewed-package-preparation.stdout')['diagnostics']}=={'HXW011','HXW009'}
assert records['preservation-check']['exit_code']==1
binding=read('catalog-binding.json');raw=(p/'committed-catalog.props').read_bytes()
assert hashlib.sha256(raw).hexdigest()==binding['raw_sha256']
assert raw.decode('utf-8-sig').encode()==(p/'reviewed-generated-source-mapping/pinned-tree/Props/Directory.Packages.props').read_bytes()
assert binding['commit']=='81ab049208678bd6616828824729c0ddac02f7d1'
total=0
for result in read('reviewed-test-results.json'):
 assembly=ET.parse(p/result['xml']).find('assembly');assert assembly is not None
 for key in ['failed','errors','skipped','not-run']:assert assembly.get(key)=='0'
 assert assembly.attrib==result['assembly']
 cases=[t.attrib for t in assembly.findall('.//test')];assert cases==result['cases']
 total+=int(assembly.get('passed'))
assert total==175
controls=read('metadata-controls.json');assert len(controls)==8 and all(r['pass'] and r['references']==5 for r in controls)
for control in controls:
 items=read('metadata-'+control['case']+'.stdout')['Items']['ProjectReference'];assert len(items)==5
 for item in items:assert item.get('GlobalPropertiesToRemove','')==('' if control['producer_owned'] else 'Version;PackageVersion')
 analyzer=next(i for i in items if 'SourceTools' in i['Identity'])
 assert analyzer['OutputItemType']=='Analyzer' and analyzer['ReferenceOutputAssembly']=='false' and analyzer['SetTargetFramework']=='TargetFramework=netstandard2.0'
runtime=read('reviewed-runtime-lane.json');assert runtime['outbound_ready'] and runtime['container_absent'] and runtime['sidecar_exit']==0
before=read('preservation-before.json');after=read('preservation-after.json')
diff=[{'repository':repo,'field':key,'before':before[repo][key],'after':after[repo][key]} for repo in before for key in before[repo] if before[repo][key]!=after[repo][key]]
assert diff==read('preservation-drift.json')['differences'] and diff
if args.live:
 assert git(workspace,'rev-parse','HEAD').decode().strip()==selected['head']
 for entry in selected['inputs']:
  repo=workspace/entry['path'];revision=entry['selected']
  assert git(repo,'rev-parse','HEAD').decode().strip()==revision
  assert git(repo,'rev-parse','--show-superproject-working-tree').decode().strip()==str(workspace)
  line=git(workspace,'ls-files','--stage','--',entry['path']).decode();assert line.startswith('160000 '+revision+' 0\t')
  assert not list((repo/'references').glob('*/.git'))
 for repo_name,data in read('reviewed-source-inventory.json').items():
  repo=workspace/repo_name
  assert git(repo,'status','--porcelain=v1','--untracked-files=all').decode()==data['status'],repo_name
  for relative,expected in data['tracked_hashes'].items():assert sha(repo/relative)==expected,relative
 for name,expected in candidate.items():assert sha(workspace/name)==expected,name
 for name,expected in read('reviewed-generated-input-hashes.json').items():assert sha(Path(prepared['buildPreparation']['workspace'])/'source-mapping'/name)==expected,name
 for layer,files in read('reviewed-executable-inventory.json').items():
  out=workspace/f'tests/Hexalith.Projects.{layer}.Tests/bin/Debug/net10.0'
  actual={str(f.relative_to(out)):sha(f) for f in out.rglob('*') if f.is_file()};assert actual==files,layer
print(json.dumps({'evidence_checks':'pass','live_isolated_checks':args.live,'existing_tests_passed':total,'original_workspace_unchanged':False,'original_workspace_result':'Recorded concurrent drift; strict preservation audit failed. No restoration attempted.','qualification':'Source candidate only; not release or Story completion'}))
