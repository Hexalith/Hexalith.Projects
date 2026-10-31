#!/usr/bin/env python3
"""Independently check retained raw stream hashes and immutable archive identities."""
from pathlib import Path
import hashlib,json,sys
root=Path(__file__).resolve().parent.parent
failures=[];receipts=[]
for directory in [root/'analyzer',root/'qualification']:
 for p in sorted(directory.glob('*.command.json')):
  r=json.loads(p.read_text());receipts.append({'file':str(p.relative_to(root)),'exitCode':r['exitCode']})
  for stream in ['stdout','stderr']:
   rec=r[stream];q=p.parent/rec['file'];b=q.read_bytes()
   if hashlib.sha256(b).hexdigest()!=rec['sha256'] or len(b)!=rec['sizeBytes']:failures.append(str(q))
a=json.loads((root/'analyzer/analysis-result.json').read_text())
source=json.loads((root/'source-inputs.json').read_text())
assert a['selectedRevision']==source['selectedSource']=='2186b9a4bc507a116a457c590c80945c2e232cbd'
assert a['selectedTree']==source['tree']
assert a['packageJsonSha256']==source['inputs']['package.json']['sha256']
assert a['packageLockSha256']==source['inputs']['package-lock.json']['sha256']
assert a['proposedVersion']=='4.39.0' and a['releaseType']=='minor' and len(a['commits'])==95
assert a['executedLifecycle'] is False
archives=json.loads((root/'qualification/local-archive-observations.json').read_text())
for rec in archives['archives']:
 p=root/'qualification/local-packages-4.39.0'/rec['file'];b=p.read_bytes()
 if hashlib.sha256(b).hexdigest()!=rec['sha256'] or len(b)!=rec['sizeBytes']:failures.append(str(p))
 assert rec['repository']['commit']==a['selectedRevision'] and rec['version']==a['proposedVersion']
 assert rec['published'] is False and rec['signed'] is False and rec['qualificationPassed'] is False
assert len(archives['archives'])==4 and archives['releaseEligible'] is False
assert not (root/'qualification/local-packages-4.39.0/g4-tool-package-inventory.json').exists()
result={'checkedCommandReceipts':receipts,'checkedArchiveCount':4,'failures':failures,'consistent':not failures,'qualificationPassed':False}
(root/'qualification/receipt-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'checkedCommandReceipts':len(receipts),'checkedArchives':4,'consistent':not failures}))
sys.exit(bool(failures))
