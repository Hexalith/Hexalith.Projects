from pathlib import Path
import hashlib,json
p=Path(__file__).resolve().parent
source=(p/'QueriesController.private.cs').read_text()
for line in json.loads((p/'normalization-inserted-lines.json').read_text()):
 assert source.count(line+'\n')==1;source=source.replace(line+'\n','',1)
assert source.encode()==(p/'QueriesController.original.cs').read_bytes()
proof=json.loads((p/'source-proof.json').read_text());assert hashlib.sha256(source.encode()).hexdigest()==proof['originalSha256']
print(json.dumps({'byteExact':True,'onlyInsertedLines':23,'originalSha256':proof['originalSha256'],'methodIdentityUnchanged':True}))
