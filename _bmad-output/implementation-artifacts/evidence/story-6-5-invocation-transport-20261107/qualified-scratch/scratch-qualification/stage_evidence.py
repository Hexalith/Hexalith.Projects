from pathlib import Path
import datetime,gzip,hashlib,json,shutil
ROOT=Path('/home/administrator/projects/hexalith/projects');P=Path('/tmp/story65-invocation-transport-prep-20261107');Q=Path('/tmp/story65-invocation-transport-qualification-20261107');O=Path('/tmp/story65-invocation-transport-ops-prep-20261107');E=Path('/tmp/story65-invocation-transport-evidence-20261107');E.mkdir(mode=0o700,exist_ok=False)
sha=lambda b:hashlib.sha256(b).hexdigest();copied={};compressed={}
def copy(source,target,expected=None):
 assert source.is_file()and not source.is_symlink();data=source.read_bytes();digest=sha(data);assert expected is None or expected==digest
 target=E/target;target.parent.mkdir(mode=0o700,parents=True,exist_ok=True);target.write_bytes(data);target.chmod(0o600);assert sha(target.read_bytes())==digest;copied[str(target.relative_to(E))]={'source':str(source),'sha256':digest,'bytes':len(data)}
meta=json.loads((P/'preparation-receipt.json').read_text())
assert sha((P/'preparation-receipt.json').read_bytes())=='8d967b31560ce95a630751a2d2b37bc6a9070296d19306ad11785c4b01ca0d36'
for rel,d in meta['filesSha256'].items():copy(P/rel,Path('qualified-preparation')/rel,d)
copy(P/'preparation-receipt.json','qualified-preparation/preparation-receipt.json')
# Qualification logs/scripts/receipts include no application payload values or binaries.
for f in sorted(Q.rglob('*')):
 if f.is_file()and f.suffix in {'.json','.py','.log'}:copy(f,Path('scratch-qualification')/f.relative_to(Q))
for f in sorted((P/'results-initial').rglob('*')):
 if not f.is_file():continue
 rel=Path('scratch-results')/f.relative_to(P/'results-initial')
 if f.suffix=='.jsonl':
  data=f.read_bytes();rows=0
  for line in data.splitlines():
   d=json.loads(line);assert d['role']=='gateway'and d['stage']in{'Whole','ComponentFile','ComponentPolicy'};assert all(type(d[k])is int and d[k]==0 for k in ['allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded']);rows+=1
  zipped=gzip.compress(data,compresslevel=9,mtime=0);out=E/str(rel)+'.gz' if False else E/Path(str(rel)+'.gz');out.parent.mkdir(mode=0o700,parents=True,exist_ok=True);out.write_bytes(zipped);out.chmod(0o600);assert gzip.decompress(zipped)==data
  compressed[str(out.relative_to(E))]={'source':str(f),'originalSha256':sha(data),'originalBytes':len(data),'rows':rows,'gzipSha256':sha(zipped),'gzipBytes':len(zipped),'strictThreeStageSchemaValidated':True,'allCountAllocationZero':True}
 elif f.suffix=='.json':copy(f,rel)
 else:raise AssertionError('UnexpectedScratchResultFile')
assert sum(x['rows']for x in compressed.values())==83662
# Immutable producer metadata only. Isolated binaries, obj and any new live subtree are excluded.
allowed={'.json','.py','.log','.md','.props','.targets','.diff','.txt'}
for f in sorted(O.rglob('*')):
 rel=f.relative_to(O)
 if set(rel.parts)&{'isolated-sdk','isolated-oracle','__pycache__','live'}:continue
 if f.is_file()and f.suffix in allowed:copy(f,Path('isolated-producer')/rel)
old=Path('/tmp/story65-invocation-transport-prep-history-20261107/07aa')
for f in sorted(old.iterdir()):
 if f.is_file():copy(f,Path('unexecuted-preparation-07aa')/f.name)
# Preserve exact controlled source bodies, never application binaries.
for rel,d in meta['sourceSha256'].items():copy(ROOT/rel,Path('source-controls-55')/rel,d)
actual=json.loads((O/'producer-attempts/attempt-_ktcrvhx/actual-compiler-inputs.json').read_text())
compile_inputs=[x for x in actual['inputs']if x['kind']=='Compile'];assert len(compile_inputs)==145
for i,item in enumerate(compile_inputs):
 f=Path(item['path']);assert f.suffix=='.cs';copy(f,Path('producer-compile-sources')/f'{i:03d}-{f.name}',item['sha256'])
for f in [Path('/tmp/story65-invocation-transport-parent-scratch-audit-20261107.py')]:copy(f,Path('independent-audit')/f.name)
readme='''This TEMP candidate records only source preparation, isolated SDK compilation and scratch differential qualification. It is not final Story6.5 feature acceptance. Native33, application/live diagnostic, ordinary restoration and MULTIPLE/browser acceptance remain pending. The historical ordinary1038 total is not increased or requalified by this packet.\n\nAll80 eligible comparison cells (84 actual SendAsync entries per variant because redirect reenters),3 first-business canaries and one genuine excluded entry process passed. The excluded process separately compared20 cells/21 entries, lived through the8-second window plus4-second closure opportunity, and observed no recorder artifacts without helper reflection. This does not prove no helper cctor/worker execution or execution of all five real excluded hosts. Six real entry identities are metadata/source proof only.\n\nThe83,662 closed numeric rows use only Whole/ComponentFile/ComponentPolicy, with all allocation/count columns zero. Original JSONL bytes/hash/row counts and deterministic gzip hashes are in the staging manifest. No window.control content, DLL/PDB/cache, request/response payload, proof/header/context values or dependency trace values are copied.\n\nWhole excludes eligibility/admission; transport excludes outer ResponseContentRead buffering. Missing children do not establish kind/result/outcome. Recursive Whole intervals can overlap. Residual wall time is unattributed; there is no join to previous probes or logical requests. The controlled synthetic catalog is not an installed Gateway inventory proof.\n\nUnexecuted07aa source preparation and disabled9848/468/62e producer generations are preserved as history. The07aa controller union-preservation gap was corrected before any build/run; it is not a failed runtime attempt. All three scratch builds and both facts executions passed on their first attempt, with zero warnings/errors and no source/fixture fix after execution began.\n'''
(E/'README.md').write_text(readme);copied['README.md']={'source':'generated staging scope/limits','sha256':sha((E/'README.md').read_bytes()),'bytes':(E/'README.md').stat().st_size}
for f in E.rglob('*'):
 if f.is_file():assert f.suffix not in {'.dll','.pdb','.cache','.pyc','.control','.jsonl'}
manifest={'temporaryCandidateOnly':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'qualifiedPreparationSha256':'8d967b31560ce95a630751a2d2b37bc6a9070296d19306ad11785c4b01ca0d36','finalScratchQualificationSha256':'4676214b7425a92c9d0ed5b801b4afd0e0ea8c2973c952bc8c3c9477562bb2e5','copiedFiles':copied,'compressedSyntheticRows':compressed,'strictRowTotal':83662,'native33Executed':False,'applicationBuilds':0,'hostsStarted':0,'pending':['native33','application/live diagnostic','ordinary restoration','MULTIPLE and browser acceptance'],'noBinariesRawControlOrRuntimePayloadValues':True}
(E/'staging-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
files={str(f.relative_to(E)):sha(f.read_bytes())for f in sorted(E.rglob('*'))if f.is_file()};(E/'SHA256SUMS').write_text(''.join(d+'  '+n+'\n'for n,d in files.items()))
print(json.dumps({'path':str(E),'stagingManifestSha256':sha((E/'staging-manifest.json').read_bytes()),'checksumManifestSha256':sha((E/'SHA256SUMS').read_bytes()),'checksumEntries':len(files),'copiedFiles':len(copied),'gzipFiles':len(compressed),'strictRows':sum(x['rows']for x in compressed.values()),'totalBytes':sum(f.stat().st_size for f in E.rglob('*')if f.is_file())}))
