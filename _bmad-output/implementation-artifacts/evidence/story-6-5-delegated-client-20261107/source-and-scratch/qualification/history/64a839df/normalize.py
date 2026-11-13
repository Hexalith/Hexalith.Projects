from pathlib import Path
import json,hashlib
p=Path(__file__).resolve().parent
proof=json.loads((p/'source-proof.json').read_text())
s=(p/'DelegatedOwnerQueryClient.private.cs').read_text()
for change in proof['signatureAndCallEdits']:
 assert s.count(change['new'])==1;s=s.replace(change['new'],change['old'],1)
for line in json.loads((p/'normalization-inserted-lines.json').read_text()):
 assert s.count(line+'\n')==1;s=s.replace(line+'\n','',1)
assert s.encode()==(p/'DelegatedOwnerQueryClient.original.cs').read_bytes()
assert hashlib.sha256(s.encode()).hexdigest()==proof['originalSha256']
print(json.dumps({'normalizedByteExact':True,'originalSha256':proof['originalSha256'],'removedInsertedLines':proof['insertedLines'],'reversedPrivateSignatureCallEdits':2}))
