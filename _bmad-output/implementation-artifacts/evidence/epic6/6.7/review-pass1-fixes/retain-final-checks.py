"""Validate and retain the final source/native/rollback evidence after owned scratch cleanup."""
import argparse, hashlib, json, os, re, subprocess, xml.etree.ElementTree as ET
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--scratch-root',required=True);args=parser.parse_args()
root=Path.cwd();out=Path(__file__).resolve().parent;scratch=Path(args.scratch_root)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
checks=[]
commands=[['python3','-B','tools/planning/validate_production_authority.py','--story-id','6.7'],
 ['python3','-B','tools/planning/validate_production_authority.py','--validate-index'],['git','diff','--check']]
for command in commands:
 result=subprocess.run(command,cwd=root,capture_output=True,text=True)
 checks.append({'command':command,'exitCode':result.returncode,'output':result.stdout+result.stderr})
 assert result.returncode==0,checks[-1]
manifest=json.loads((out/'source-manifest-2.json').read_text());files=0
for repo in manifest['repositories']:
 for entry in repo['files']:
  assert sha(root/repo['path']/entry['path'])==entry['sha256'],entry['path'];files+=1
 for name in repo['deletedFiles']:assert not (root/repo['path']/name).exists()
assert manifest['sourceCopyParity'] and files==22242
native=[]
for name in ['Domain-full-1','Contracts-full-1','Client-full-2','UI-full-2','Server-full-3','Rollback-full-3']:
 path=out/(name+'.xml');xml=ET.parse(path);tests=xml.findall('.//test');assert tests
 assert all(t.get('result')=='Pass' for t in tests)
 for assembly in xml.findall('.//assembly'):
  assert int(assembly.get('total'))==int(assembly.get('passed'))==len(assembly.findall('.//test'))>0
  assert all(assembly.get(key)=='0' for key in ['failed','skipped','not-run','errors'])
  assert Path(assembly.get('name')).is_relative_to(scratch)
  native.append({'path':path.name,'sha256':sha(path),'passed':int(assembly.get('passed')),'skipped':0,'notRun':0})
 log=(out/(name+'-build.log')).read_text()
 assert re.search(r'\b0 Warning\(s\)',log) and re.search(r'\b0 Error\(s\)',log)
packet=json.loads((out/'read-cutover-final-2.json').read_text())
assert packet['sourceManifest']['sha256']==sha(out/'source-manifest-2.json')
for receipt in packet['nativeReceipts']:assert receipt['sha256']==sha(out/receipt['path'])
matrix=json.loads((out/'Server-full-3-matrix.json').read_text());assert len(matrix['cases'])==77
assert len({(cell['operation'],cell['scenario']) for cell in matrix['cases']})==77
assert all(c['passed'] and c['noDomainWrites'] and not c['comparisonDifferences'] and not c['unexpectedSdkWriteKeys'] for c in matrix['cases'])
proof=json.loads((out/'rollback-final-proof-2.json').read_text())
assert proof['executed'] and proof['passed'] and proof['evidenceHash']==sha(out/'read-cutover-final-2.json')
assert proof['supportedFromRealReceipt']==proof['immutableSelectionAfterRegression']=='Supported'
assert proof['regressionRefusal']==proof['incompleteLegacySecurityRefusal']=='Unavailable'
assert proof['explicitRollbackFromRealReceipt']=='LegacyRollback' and len(proof['reads'])==7
assert all(r['passed'] and r['noDomainWrites'] and not r['unexpectedSdkWriteKeys']
 and r['domainBytesAndEtagsBefore']==r['domainBytesAndEtagsAfter'] for r in proof['reads'])
cleanup=json.loads((out/'sidecar-cleanup.json').read_text());assert cleanup['reaped']
try:os.kill(cleanup['ownedPid'],0)
except ProcessLookupError:cleanup['processAbsent']=True
else:raise AssertionError('Owned Dapr still exists')
assert not scratch.exists();cleanup['privateWorkspaceRemoved']=True
startup=json.loads((out/'ui-startup-proof.json').read_text());assert startup['started'] and startup['reaped'] and startup['matchingStartupLogs']==1
mapping=json.loads((out/'private-source-mapping.log').read_text());diagnostics=sorted({d['ruleId'] for d in mapping['diagnostics']})
spec=root/'_bmad-output/implementation-artifacts/spec-6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback.md'
text=spec.read_text();assert '- [ ] [Review][Patch]' not in text
assert "baseline_commit: 'c6541a305a420078b16dceaba47f36468022850b'" in text
frozen=text.split('<frozen-after-approval',1)[1].split('</frozen-after-approval>',1)[0]
assert hashlib.sha256(frozen.encode()).hexdigest()==manifest['approvedFrozenIntentSha256']
failed_original=json.loads((out/'final-session-source-mapping.log').read_text());assert failed_original['outcome']['ruleId']=='HXW003'
result={'checks':checks,'nativeResults':native,'fullProjectsPassed':sum(n['passed'] for n in native if not n['path'].startswith('Rollback')),
 'matrixPassed':77,'rollbackReadsPassed':7,'sourceFilesCompared':files,'sourceManifestSha256':sha(out/'source-manifest-2.json'),
 'sourceCopyParity':True,'startupLogOccurrences':1,'cleanup':cleanup,'mappingDiagnostics':diagnostics,
 'activationPacket':{'path':'read-cutover-final-2.json','sha256':sha(out/'read-cutover-final-2.json')},
 'rollbackProof':{'path':'rollback-final-proof-2.json','sha256':sha(out/'rollback-final-proof-2.json')},
 'broadGateBlocker':{'command':'dotnet Hexalith.Builds.Module.Cli.dll prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace <private>/prepared --output json',
 'exitCode':2,'ruleId':'HXW003','cause':'Pre-existing initialized Conversations nested Builds dependency; original checkout preserved.',
 'fallback':'Private source copy with nested dependencies excluded; supported source mapper then succeeded.'},
 'historicalAttempts':['Server-compile-1: test referenced owner-internal key helper; fixture now seeds the documented storage address.',
 'Server-full-1: incorrect new test audience configuration and two new assertion fixtures; corrected without weakening product behavior.',
 'Client-full-1: nullable assertion in the new test; corrected.',
 'Rollback-full-2: fixed domain computation clock was incorrectly reused for expiring protocol proofs. Fixture now supplies the real protocol clock to the existing SDK transport; deadlines and verifier unchanged.'],
 'qualification':'Debug source/project-reference verification; production binding, owner/package publication and external Chatbot adoption remain deferred.',
 'deploymentPublicationOrGitMutation':False}
assert result['fullProjectsPassed']==2877
path=out/'final-checks.json';assert not path.exists();path.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:result[k] for k in ['fullProjectsPassed','matrixPassed','rollbackReadsPassed','sourceFilesCompared','cleanup','mappingDiagnostics']}))
