#!/usr/bin/env python3
"""Assemble one closed metadata/source packet in fresh TEMP storage; never publish or mutate inputs."""
from pathlib import Path
import collections, gzip, hashlib, json, math, os, re, shutil, xml.etree.ElementTree as ET

DEST=Path('/tmp/story65-canonical-hash-memo-repair-evidence-20261108')
ROOT=Path('/home/administrator/projects/hexalith/projects')
SCRATCH=Path('/tmp/story65-canonical-hash-memo-prep-20261108')
FINAL=Path('/tmp/story65-canonical-hash-memo-final-compiled-prep-20261108')
SBUILD=Path('/tmp/story65-canonical-hash-memo-build-20261108')
FBUILD=Path('/tmp/story65-canonical-hash-memo-final-compiled-build-20261108')
NATIVE=Path('/tmp/story65-canonical-hash-native-20261108')
PROD=Path('/tmp/story65-canonical-hash-memo-production-edit-20261108')
AUDITS=[(Path('/tmp/story65-canonical-hash-memo-audit-20261108'),'scratch-independent'),
        (Path('/tmp/story65-canonical-hash-native-audit-20261108'),'native-independent'),
        (Path('/tmp/story65-canonical-hash-memo-final-compiled-audit-20261108'),'final-independent')]
ARTIFACTS={}
ARCHIVES=[]

def sha(data): return hashlib.sha256(data).hexdigest()
def unique_pairs(pairs):
 d={}
 for k,v in pairs:
  assert k not in d,'duplicate JSON key';d[k]=v
 return d
def read_json(path): return json.loads(path.read_bytes(),object_pairs_hook=unique_pairs,parse_constant=lambda _:(_ for _ in ()).throw(AssertionError('nonfinite JSON')))
def add(source,target):
 source=Path(source);assert source.is_file() and not source.is_symlink(),source
 assert source.suffix not in {'.dll','.pdb','.nettrace','.etlx','.log'},source
 data=source.read_bytes();out=DEST/target;assert not out.exists(),out
 out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(data)
 assert out.read_bytes()==data
 ARTIFACTS[str(out.relative_to(DEST))]={'sha256':sha(data),'bytes':len(data),'sourcePath':str(source),'sourceSha256':sha(data),'kind':'exact-byte-copy'}
 return out

def generated(target,value):
 out=DEST/target;assert not out.exists(),out;out.parent.mkdir(parents=True,exist_ok=True)
 data=(json.dumps(value,indent=2)+'\n').encode();out.write_bytes(data)
 ARTIFACTS[str(out.relative_to(DEST))]={'sha256':sha(data),'bytes':len(data),'kind':'closed-derived-metadata'}

def selected_tree(source,target,suffixes):
 for path in sorted(source.rglob('*')):
  if path.is_file() and path.suffix in suffixes and not any(part in {'bin','obj','__pycache__'} for part in path.relative_to(source).parts):
   add(path,str(Path(target)/path.relative_to(source)))

def archive_samples(source,target,final):
 data=source.read_bytes();rows=[];observations=0;seen=set()
 base={'Dataset','Workload','State','Variant','Readers','Round','Observations','WallTicks','CpuMilliseconds','AllocatedBytes','Gen0','Gen1','Gen2','Captures','OwnerDisposeTicks','Counters'}
 if final: base.add('OwnerInitializeTicks')
 counter_shapes={
  'simple':{'ByteMemoEntries','ByteMemoRetained'},
  'memo':{'HashEntries','RetainedCharge','Hits','Misses','ByteMemoEntries','ByteMemoRetained'},
  'actual':{'HashEntries','RetainedCharge','Hits','Misses','ByteMemoEntries','ByteMemoRetained','ActualAuthorityOwner','ActualSelectorOptionObservationsPerEntry','OriginalOwnerMemos','AdditionalCanonicalHashMemo','ScopeDelegateBoundOutsideCapture','NoPerCaptureReflection'}}
 for line in data.splitlines():
  d=json.loads(line,object_pairs_hook=unique_pairs);assert set(d)==base
  assert d['Dataset'] in {'public-catalog-fixture','compiled-sample-owner'} and d['Workload'] in {'inventory','identity'}
  assert d['State'] in {'memo-cold','warm'} and d['Variant'] in {'production','control','candidate'}
  assert type(d['Readers']) is int and d['Readers'] in {1,2,4} and type(d['Round']) is int and d['Round'] in {1,2,3}
  key=tuple(d[k] for k in ['Dataset','Workload','State','Variant','Readers','Round']);assert key not in seen;seen.add(key)
  for k in ['Observations','WallTicks','AllocatedBytes','Gen0','Gen1','Gen2','OwnerDisposeTicks']+(['OwnerInitializeTicks'] if final else []): assert type(d[k]) is int and d[k]>=0
  assert type(d['CpuMilliseconds']) in {int,float} and math.isfinite(d['CpuMilliseconds']) and d['CpuMilliseconds']>=0
  count=d['Readers']*(64 if d['State']=='warm' else 1);assert d['Observations']==count==len(d['Captures'])
  ids=set()
  for capture in d['Captures']:
   assert set(capture)=={'Worker','Ordinal','ElapsedTicks'} and all(type(v) is int for v in capture.values())
   assert 0<=capture['Worker']<d['Readers'] and 1<=capture['Ordinal']<=(64 if d['State']=='warm' else 1) and capture['ElapsedTicks']>=0
   pair=(capture['Worker'],capture['Ordinal']);assert pair not in ids;ids.add(pair)
  expected='actual' if final and d['Variant']=='production' else 'memo' if d['Variant']=='candidate' else 'simple'
  assert set(d['Counters'])==counter_shapes[expected]
  assert all(type(v) in {int,bool} for v in d['Counters'].values())
  observations+=count;rows.append(d)
 assert len(rows)==216 and observations==16380 and len(seen)==216
 out=DEST/target;out.parent.mkdir(parents=True,exist_ok=True)
 with out.open('wb') as sink:
  with gzip.GzipFile(filename='',mode='wb',fileobj=sink,mtime=0) as zipfile: zipfile.write(data)
 assert gzip.decompress(out.read_bytes())==data
 ARCHIVES.append({'path':target,'sourcePath':str(source),'originalSha256':sha(data),'originalBytes':len(data),'gzipSha256':sha(out.read_bytes()),'gzipBytes':out.stat().st_size,'strictBatches':216,'strictObservations':16380,'decompressionByteIdentical':True,'duplicateJsonKeysAndBatchCaptureIdentitiesRejected':True})
 ARTIFACTS[target]={'sha256':sha(out.read_bytes()),'bytes':out.stat().st_size,'sourcePath':str(source),'sourceSha256':sha(data),'kind':'gzip-exact-original-bytes'}

def main():
 assert not DEST.exists(),'fresh destination required'
 DEST.mkdir(mode=0o755)
 # Exact complete production edit, including before/after source and independent review.
 selected_tree(PROD,'production',{'.cs','.json','.diff'})
 # Qualified source only: no framework/dependency/binary copies or redundant source-tree snapshot.
 for source,label in [(SCRATCH,'scratch/source'),(FINAL,'final/source')]:
  for folder in ['original','control','candidate','harness']:
   for p in sorted((source/folder).iterdir()):
    if p.is_file() and (p.suffix in {'.cs','.csproj'} or p.name=='NuGet.Config'):
     add(p,str(Path(label)/folder/p.name))
  for name in ['PLAN.json','commands.json','preparation-receipt.json']:
   add(source/name,str(Path(label)/name))
 for name in ['candidate.patch','source-provenance.json','reference-inputs.json']:
  add(SCRATCH/name,'scratch/source/'+name)
 for name in ['clone-provenance.json','prototype-receipt.json','qualified-candidate.patch','qualified-source-provenance.json','transfer.diff']:
  add(FINAL/name,'final/source/'+name)
 # Keep both failed alias source artifacts and their actual closed command receipts.
 for name in ['receipt.json','disabled-preparation.json','root-build-release.json']:
  add(SBUILD/name,'scratch/build/'+name)
 for name in ['Parity.cs','write_harness.py','compile-only-preparation-before.json','receipt.json','root-release.json']:
  add(SBUILD/'alias-correction'/name,'scratch/build/alias-correction/'+name)
 initial_log=SBUILD/'build.log';raw=initial_log.read_bytes();text=raw.decode('utf-8')
 diagnostics=[]
 for line in text.splitlines():
  match=re.search(r'(?P<file>[^/\\]+\.cs)\((?P<line>\d+),(?P<column>\d+)\): error (?P<code>CS\d+):',line)
  if match:
   value={k:(int(v) if k in {'line','column'} else v) for k,v in match.groupdict().items()}
   if value not in diagnostics:diagnostics.append(value)
 generated('scratch/build/alias-correction/initial-failure-closed-metadata.json',{'classification':'scratch-only-alias-shadowing','initialBuildExitCode':1,'runtimeInvocations':0,'originalCompilerLogPath':str(initial_log),'originalCompilerLogSha256':sha(raw),'originalCompilerLogBytes':len(raw),'rawStdoutNotCopied':True,'uniqueClosedDiagnostics':diagnostics,'correction':'Only standalone Control/Candidate type aliases renamed ControlCodec/CandidateCodec in Parity and its historical generator; product/source algorithms unchanged.'})
 for name in ['compile-only-preparation.json','root-release.json','run-receipt.json']:
  add(SBUILD/'runtime-release'/name,'scratch/build/runtime-release/'+name)
 for name in ['receipt.json','native-handoff.json','root-build-release.json']:
  add(FBUILD/name,'final/build/'+name)
 for name in ['compile-only-preparation.json','root-release.json','run-receipt.json']:
  add(FBUILD/'runtime-release'/name,'final/build/runtime-release/'+name)
 for root,label,is_final in [(Path('/tmp/story65-canonical-hash-memo-results-20261108'),'scratch/results',False),(Path('/tmp/story65-canonical-hash-memo-final-compiled-results-20261108'),'final/results',True)]:
  for name in ['closed.json','dataset-facts.json','parity.json','runtime-facts.json','summary.json']:
   add(root/name,label+'/'+name)
  if (root/'root-implementation-decision.json').exists():add(root/'root-implementation-decision.json',label+'/root-implementation-decision.json')
  archive_samples(root/'raw-batches.jsonl',label+'/raw-batches.jsonl.gz',is_final)
 # Public fixture bytes and compiled-owner source; compiled data DLL remains hash-only in refs.
 add(ROOT/'references/Hexalith.EventStore/deploy/dapr/eventstore-routing-catalog.json','datasets/public-catalog-fixture.json')
 add(ROOT/'references/Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Counter/SampleCatalog.cs','datasets/SampleCatalog.cs')
 # Exact actual native XML plus command/source/generation proofs; native stdout is excluded.
 add(NATIVE/'source-controls.json','native/source-controls.json')
 add(NATIVE/'initial-build-receipt.json','native/initial-build-receipt.json')
 for folder in ['final','smoke']:
  for p in sorted((NATIVE/folder).iterdir()):
   if p.is_file() and p.suffix in {'.json','.xml'}:add(p,str(Path('native')/folder/p.name))
 for source,label in AUDITS:
  selected_tree(source,'audits/'+label,{'.json','.py'})
 selected_tree(Path(__file__).parent/'history','packet/packaging-history',{'.json','.py','.md'})
 # Strict proof artifact bindings, read without assuming old mutable workspace generations.
 required={
 'audits/scratch-independent/independent-benchmark-audit.json':'f8a1b620421a7a42b7fbf2ba604566bde4175ded7162e703b4c5b9e546d5832a',
 'audits/native-independent/independent-native-audit.json':'857afbd14563603cf2b1f24f9785a5decf6687b0055b1c25034184eb499001c8',
 'audits/final-independent/independent-final-compiled-audit.json':'717fbecbd7a72196e1cb2ca2885c9aaaad263cc97780b9918bc9909f209c5a4f'}
 for rel,digest in required.items(): assert sha((DEST/rel).read_bytes())==digest
 native_total=0;native_xml=[]
 for name,count in [('owner450.xml',450),('contracts-tests.xml',46),('server-tests.xml',324),('ui-tests.xml',211),('integration-tests.xml',45)]:
  p=DEST/'native/final'/name;tree=ET.parse(p);cases=tree.findall('.//test');assert len(cases)==count and all(case.attrib['result']=='Pass' for case in cases)
  assert len(tree.findall('.//error'))==0
  native_xml.append({'path':str(p.relative_to(DEST)),'passExecutions':count,'sha256':sha(p.read_bytes())});native_total+=count
 assert native_total==1076
 add(Path(__file__),'packet/assemble_evidence.py')
 generated('packet/qualification-summary.json',{'scratch':{'parityChecks':178,'batches':216,'observations':16380},'finalCompiled':{'parityChecks':178,'batches':216,'observations':16380},'actualNativeExecutions':1076,'newCanonicalNativeCases':23,'additionalUnchangedHostByteMemoCases':5,'nativeXml':native_xml,'smokeExecutionsNotAddedToNativeTotal':28,'ordinaryManagedAcceptance':'pending','fullHostDeadlineBenefitClaimed':False,'binaryCopiesIncluded':0,'rawStdoutIncluded':False,'archives':ARCHIVES,'requiredIndependentAudits':required})
 readme=Path(__file__).with_name('README.md');add(readme,'README.md')
 manifest={'schemaVersion':1,'complete':True,'temporaryReviewCandidate':True,'productionAndSpecPublicationNotPerformedByPackager':True,'artifacts':ARTIFACTS,'artifactCount':len(ARTIFACTS),'compressedOriginals':ARCHIVES,'includedNoBinariesPrivateConfigRawTraceOrNativeStdout':True}
 (DEST/'artifact-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 sums=[]
 for p in sorted(DEST.rglob('*')):
  if p.is_file():sums.append(sha(p.read_bytes())+'  '+str(p.relative_to(DEST)))
 (DEST/'SHA256SUMS').write_text('\n'.join(sums)+'\n')
 for p in DEST.rglob('*'):
  if p.is_file():p.chmod(0o444)
 print(json.dumps({'path':str(DEST),'artifactsExcludingManifestAndSums':len(ARTIFACTS),'checksummedFiles':len(sums),'manifestSha256':sha((DEST/'artifact-manifest.json').read_bytes()),'sha256SumsSha256':sha((DEST/'SHA256SUMS').read_bytes()),'bytes':sum(p.stat().st_size for p in DEST.rglob('*') if p.is_file())}))
if __name__=='__main__':main()
