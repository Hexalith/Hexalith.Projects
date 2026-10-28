"""Create the final integrity inventory once; never replace an existing seal."""
from pathlib import Path
import hashlib
import json
p=Path(__file__).resolve().parent
seal=p/'SHA256SUMS.json'
if seal.exists():raise RuntimeError('Refuse to replace an existing integrity seal')
files={str(f.relative_to(p)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sorted(p.rglob('*')) if f.is_file() and f!=seal}
seal.write_text(json.dumps(files,indent=2)+'\n')
print(json.dumps({'sealed_files':len(files),'manifest':str(seal)}))
