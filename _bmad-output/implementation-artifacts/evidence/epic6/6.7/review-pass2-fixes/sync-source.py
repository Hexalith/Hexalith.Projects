"""Copy authorized source edits into the live private verification tree; retain exact changed hashes."""
import argparse,hashlib,json,shutil,subprocess
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--label',required=True);args=parser.parse_args()
root=Path.cwd();out=Path(__file__).resolve().parent;context=json.loads((out/'runner-context.json').read_text());target=Path(context['sourceRoot'])
assert target.is_dir()
paths=set(subprocess.check_output(['git','diff','--name-only'],text=True).splitlines())|set(subprocess.check_output(['git','ls-files','--others','--exclude-standard','src','tests','docs'],text=True).splitlines());changed=[]
for name in sorted(paths):
 if not name.startswith(('src/','tests/','docs/')):continue
 source=root/name;destination=target/name
 if not source.exists():
  if destination.exists():destination.unlink();changed.append({'path':name,'deleted':True})
 elif source.is_file() and (not destination.exists() or source.read_bytes()!=destination.read_bytes()):
  destination.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,destination)
  changed.append({'path':name,'sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
path=out/(args.label+'.json');assert not path.exists();path.write_text(json.dumps(changed,indent=2)+'\n');print(json.dumps({'changedFiles':len(changed),'sourceRoot':str(target)}))
