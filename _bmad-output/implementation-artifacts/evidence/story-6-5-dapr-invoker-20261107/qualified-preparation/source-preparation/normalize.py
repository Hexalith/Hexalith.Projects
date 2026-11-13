from pathlib import Path
import json, hashlib
base=Path(__file__).parent
text=(base/'DaprDomainQueryInvoker.private.cs').read_text()
for line in reversed(json.loads((base/'normalization-inserted-lines.json').read_text())):
 assert text.count(line)==1
 text=text.replace(line,'',1)
assert text.encode()==(base/'DaprDomainQueryInvoker.original.cs').read_bytes()
print(json.dumps({'byteExact':True,'sha256':hashlib.sha256(text.encode()).hexdigest()}))
