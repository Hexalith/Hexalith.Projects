from pathlib import Path
import datetime,gzip,hashlib,json,shutil,subprocess
root=Path('/home/administrator/projects/hexalith/projects'); raw=Path('/tmp/story64-ci-repair-20261027'); target=root/'_bmad-output/implementation-artifacts/evidence/story-6-4-ci-build-mapping-repair-20261027'; target.mkdir(parents=True,exist_ok=True)
entry=json.loads(Path('/tmp/story64-ci-parent-20261027/entry.json').read_text())
rows=[]
for row in entry['fixture']:
 data=(root/'references/Hexalith.Conversations'/row['path']).read_bytes(); digest=hashlib.sha256(data).hexdigest(); assert digest==row['sha256'],row['path']; rows.append(dict(**row,current_sha256=digest,unchanged=True))
(target/'frozen-fixture-hashes.json').write_text(json.dumps(rows,indent=2)+'\n')
observations={}; sources=[]
for owner in ('Hexalith.Builds','Hexalith.Conversations'):
 cwd=root/'references'/owner
 def git(*args): return subprocess.check_output(['git',*args],cwd=cwd).decode()
 observations[owner]=dict(head=git('rev-parse','HEAD').strip(),status=git('status','--short'),staged=git('diff','--cached','--name-only'))
 paths=set(git('diff','--name-only').splitlines())|set(git('ls-files','--others','--exclude-standard').splitlines())
 for name in sorted(paths):
  source=cwd/name
  if not source.is_file(): continue
  payload=source.read_bytes(); dest=target/'candidate-sources'/owner/name; dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes(payload)
  sources.append(dict(owner=owner,path=name,sha256=hashlib.sha256(payload).hexdigest(),bytes=len(payload)))
 (target/(owner+'.patch.gz')).write_bytes(gzip.compress(git('diff','--binary').encode(),mtime=0))
(target/'candidate-source-hashes.json').write_text(json.dumps(sources,indent=2)+'\n')
(target/'owner-state.json').write_text(json.dumps(observations,indent=2)+'\n')
for source in sorted(raw.iterdir()):
 if not source.is_file(): continue
 if source.suffix=='.log': dest=target/'observations'/(source.name+'.gz'); payload=gzip.compress(source.read_bytes(),mtime=0)
 elif source.suffix in ('.json','.xml','.py'): dest=target/'observations'/source.name; payload=source.read_bytes()
 else: continue
 dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes(payload)
map_source=raw/'verified-source/source-mapping'; dest=target/'generated-source-mapping'; dest.mkdir(exist_ok=True)
for source in map_source.iterdir():
 if source.is_file(): shutil.copy2(source,dest/source.name)
summary=dict(recorded_at_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),scope='Candidate preparation and focused tests only; no delivery, publication, CI dispatch or runtime acceptance.',source_preparation=json.loads((raw/'verified-source-prepare.json').read_text()),original_source_refusal=json.loads((raw/'conversations-source-prepare.json').read_text()),current_package_refusal=json.loads((raw/'current-package-prepare.json').read_text()),frozen_fixture_files=len(rows),frozen_fixture_unchanged=True,windows_execution='not performed',independent_parent_audit='pending',producer_release_qualification='pending')
(target/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(target)
