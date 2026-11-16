"""Retain post-review source evidence, native results, exact hashes and cleanup."""
import hashlib,json,os,subprocess,xml.etree.ElementTree as ET,re
from pathlib import Path
root=Path.cwd();out=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.7/review-01'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
checks=[]
for cmd in [['python3','-B','tools/planning/validate_production_authority.py','--story-id','6.7'],['python3','-B','tools/planning/validate_production_authority.py','--validate-index'],['git','diff','--check'],['git','-C','references/Hexalith.EventStore','diff','--check'],['git','-C','references/Hexalith.Conversations','-c','core.whitespace=cr-at-eol','diff','--check']]:
 r=subprocess.run(cmd,capture_output=True,text=True);checks.append({'command':cmd,'exitCode':r.returncode,'output':r.stdout+r.stderr});assert r.returncode==0,r.stdout+r.stderr
names=['Server-full-2.xml','Client-full-1.xml','UI-full-1.xml','Contracts-full-1.xml','Domain-full-1.xml','Rollback-full-2.xml'];native=[]
for name in names:
 p=out/name;r=ET.parse(p);tests=r.findall('.//test');assert tests and all(t.get('result')=='Pass' for t in tests)
 for a in r.findall('.//assembly'):
  assert int(a.get('passed'))==int(a.get('total'))==len(a.findall('.//test'))
  assert all(a.get(k)=='0' for k in ('failed','skipped','not-run','errors'))
  assert Path(a.get('name')).is_relative_to('/tmp/hexalith-job.9baytNdd')
  native.append({'path':name,'sha256':sha(p),'assemblyPath':a.get('name'),'total':int(a.get('total')),'passed':int(a.get('passed')),'failed':0,'skipped':0,'notRun':0})
packet=json.loads((out/'read-cutover-final.json').read_text())
for receipt in packet['nativeReceipts']: assert sha(out/receipt['path'])==receipt['sha256']
matrix=json.loads((out/'Server-full-2-matrix.json').read_text());assert len(matrix['cases'])==77 and len({(c['operation'],c['scenario']) for c in matrix['cases']})==77
for c in matrix['cases']:
 assert c['passed'] and c['noDomainWrites'] and not c['unexpectedSdkWriteKeys'] and not c['comparisonDifferences']
 assert c['legacyAggregateIdentities']==c['supportedAggregateIdentities'] and c['rawLegacyKeys'] and c['rawSupportedKeys']
 if c['scenario']=='absent-denied': assert c['missingTargetEvidence']['executed'] and c['missingTargetProof'] and c['missingTargetEvidence']['passed'] and c['missingTargetEvidence']['noDomainWrites']
proof=json.loads((out/'rollback-final-proof.json').read_text());assert proof['passed'] and proof['evidenceHash']==sha(out/'read-cutover-final.json')
assert proof['supportedFromRealReceipt']=='Supported' and proof['explicitRollbackFromRealReceipt']=='LegacyRollback'
assert proof['regressionRefusal']==proof['incompleteLegacySecurityRefusal']=='Unavailable'
assert len(proof['reads'])==7
for r in proof['reads']: assert r['passed'] and r['noDomainWrites'] and not r['unexpectedSdkWriteKeys'] and r['domainBytesAndEtagsBefore']==r['domainBytesAndEtagsAfter']
cleanup=json.loads((out/'review-session-sidecar-cleanup.json').read_text());assert cleanup['reaped']
try:os.kill(cleanup['ownedPid'],0)
except ProcessLookupError:cleanup['processAbsent']=True
else:raise AssertionError('Owned sidecar remains')
assert not Path('/tmp/hexalith-job.9baytNdd').exists();cleanup['privateTMPDIRRemoved']=True
manifest={'repositories':[],'ownership':'Snapshot includes pre-existing/concurrent changes; no blanket ownership claim or Git mutation.','qualification':'Reviewed Debug source/project-reference evidence; no deployed or package-mode acceptance.'}
untracked_cs=0
for rel in ['.','references/Hexalith.EventStore','references/Hexalith.Conversations']:
 repo=root/rel
 git=lambda *a:subprocess.check_output(['git',*a],cwd=repo,text=True)
 paths=set(git('diff','--name-only','--no-ext-diff').splitlines())|set(git('ls-files','--others','--exclude-standard').splitlines())
 paths.update(x for x in ['AGENTS.md','.editorconfig','.gitattributes','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json','.gitmodules','module/hexalith.module-manifest.v2.json'] if (repo/x).is_file())
 if rel=='.':paths.update(['_bmad-output/implementation-artifacts/epic-6-context.md','_bmad-output/planning-artifacts/story-6-7-approved-read-cutover-amendments-2026-11-16.md'])
 entries=[]
 for name in sorted(paths):
  p=repo/name
  if not p.is_file() or 'evidence/' in name or name.startswith('artifacts/') or '/artifacts/' in name:continue
  data=p.read_bytes()
  if name.endswith('.cs'):
   assert not any(line.endswith((b' ',b'\t')) for line in data.splitlines()),str(p);untracked_cs+=1
  entries.append({'path':name,'sha256':sha(p),'bytes':len(data)})
 manifest['repositories'].append({'path':rel,'revision':git('rev-parse','HEAD').strip(),'branch':git('branch','--show-current').strip(),'files':entries})
spec=root/'_bmad-output/implementation-artifacts/spec-6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback.md'
text=spec.read_text();assert "status: 'done'" in text and "baseline_commit: 'c6541a305a420078b16dceaba47f36468022850b'" in text
frozen=text.split('<frozen-after-approval',1)[1].split('</frozen-after-approval>',1)[0]
manifest['approvedFrozenIntentSha256']=hashlib.sha256(frozen.encode()).hexdigest()
p=out/'source-manifest.json';encoded=json.dumps(manifest,indent=2)+'\n'
if p.exists(): assert p.read_text()==encoded
else:p.write_text(encoded)
assert sha(out.parent.parent/'6.7-read-cutover.json')==sha(out.parent/'before-approved-continuation/6.7-read-cutover.json')
for name in names:
 log=(out/name.replace('.xml','-build.log')).read_text()
 assert re.search(r'\b0 Warning\(s\)',log) and re.search(r'\b0 Error\(s\)',log)
result={'checks':checks,'nativeResults':native,'fullProjectsPassed':sum(r['passed'] for r in native if r['path']!='Rollback-full-2.xml'),'matrixCasesPassed':77,'additionalAbsenceOrValidEmptyProofs':7,'retainedRollbackReadsPassed':7,'sourceManifestSha256':sha(p),'activationPacket':{'path':'read-cutover-final.json','sha256':sha(out/'read-cutover-final.json')},'rollbackProof':{'path':'rollback-final-proof.json','sha256':sha(out/'rollback-final-proof.json')},'cleanup':cleanup,'sourceCSharpWhitespaceChecked':untracked_cs,'postBuildChanges':'One trailing whitespace correction and workflow/docs/tracking updates only; application behavior unchanged.','historicalAttempts':'Server-full-1 had an order-only dictionary assertion failure, corrected and fully rerun. Rollback-full-1 lacked routing input and is not rollback evidence.','sourceQualificationDiagnostics':['HXW011','HXW009','HXW010'],'productionAcceptance':False,'deploymentPublicationOrGitMutation':False,'specStatus':'done','sprintStatus':'review','deferredFollowups':4}
p=out/'final-checks.json';assert not p.exists();p.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'passed':result['fullProjectsPassed'],'matrix':77,'rollbackReads':7,'sourceFiles':sum(len(r['files']) for r in manifest['repositories']),'cleanup':cleanup,'sourceManifestSha256':result['sourceManifestSha256']}))
