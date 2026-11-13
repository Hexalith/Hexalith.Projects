from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch
import importlib.util,tempfile,json,hashlib,shutil,datetime
base=Path(__file__).parent;spec=importlib.util.spec_from_file_location('generation_smoke',base/'watch_live.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
root=Path(tempfile.mkdtemp(prefix='story65-generation-smoke-'));root.chmod(0o700);owner=root/'owner';owner.mkdir();evidence=root/'evidence';evidence.mkdir();watch=root/'watch';watch.mkdir();api=owner/'src/Hexalith.EventStore/bin/Debug/net10.0/Hexalith.EventStore.Gateway.dll';producer=owner/'src/Hexalith.EventStore.Gateway/bin/Debug/net10.0/Hexalith.EventStore.Gateway.dll';api.parent.mkdir(parents=True);producer.parent.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest();sources={};manifest={};image=b'no target image: synthetic guard fixture'
for i in range(41):
 relative=f'src/source-{i}.cs';path=owner/relative;path.parent.mkdir(parents=True,exist_ok=True);path.write_text(f'// guard-only source {i}\n');sources[relative]=h(path.read_bytes());manifest[str(path.relative_to(root))]=sources[relative]
audit=root/'owner.json';audit.write_text(json.dumps({'sourceSha256':sources}));(evidence/'source-manifest.json').write_text(json.dumps(manifest));api.write_bytes(image);producer.write_bytes(image)
a=SimpleNamespace(owner_receipt=audit,owner_receipt_sha256=h(audit.read_bytes()),evidence=evidence,watch_output=watch,gateway_sha256='0'*64)
results=[]
try:
 with patch.object(m,'ROOT',root),patch.object(m,'OWNER',owner),patch.object(m,'GATEWAY',api),patch.object(m,'mapped',return_value=True):
  proof=m.generation(a,(99999,'1'));assert proof['sourceFileCount']==41 and proof['allSourceInputsUnchanged'] and proof['freshGenerationGatewaySha256']==h(image) and proof['generationChanged'];results.append({'case':'automatic-fresh-hash-both-matching-copies-and-all41-source-hashes','passed':True})
  api.write_bytes(b'different')
  try:m.generation(a,(99999,'1'));raise AssertionError('accepted mismatched copies')
  except m.Guard as error:assert str(error)=='producer-api-gateway-copy-mismatch';results.append({'case':'producer-api-copy-mismatch-rejected','passed':True})
  api.write_bytes(image);source=owner/'src/source-0.cs';old=source.read_bytes();source.write_bytes(b'changed')
  try:m.generation(a,(99999,'1'));raise AssertionError('accepted changed source')
  except m.Guard as error:assert str(error)=='owned-source-freeze-mismatch';results.append({'case':'changed-one-of41-source-files-rejected','passed':True})
  source.write_bytes(old);api.write_bytes(b'PrivateFocusedTiming');producer.write_bytes(b'PrivateFocusedTiming')
  try:m.generation(a,(99999,'1'));raise AssertionError('accepted private timing')
  except m.Guard as error:assert str(error)=='private-timing-marker-present';results.append({'case':'forbidden-timing-marker-rejected','passed':True})
 failed=lambda n:{'multiple':True,'attempt':n,'result':None,'incomplete':True,'candidateCount':0,'positiveCandidatesVerified':False}
 positive={'multiple':True,'attempt':2,'result':'MultipleCandidates','incomplete':False,'candidateCount':2,'positiveCandidatesVerified':True}
 assert m.decision([failed(1)])=='failed-attach' and m.decision([failed(27)])=='failed-attach' and m.decision([failed(28)])=='insufficient-windows-skip' and m.decision([failed(1),positive])=='positive-skip';results.append({'case':'late-ready-failed-history-safe-if-three-windows-remain-and-no-positive','passed':True})
 # Full control flow with absent end report and late readiness; every OS/graph/profile action mocked.
 runout=root/'late-ready';args=SimpleNamespace(watch_output=runout,evidence=evidence,timeout=2,not_before_epoch=datetime.datetime.now(datetime.timezone.utc).timestamp(),wrapper=base/'run_probe.py',tool=base/'bin/Debug/net10.0/AllocationProbe.dll',owner_receipt=audit)
 owned={'appHostPid':111,'appHostStartTicks':'123'};resource={'wrapperPid':222};phases=[];original_write=m.write
 def record(path,value):
  if path.name=='watcher-receipt.json':phases.append(value['phase'])
  original_write(path,value)
 identity={'Files':[{}]*45}
 with patch.object(m,'observations',return_value=[failed(1)]),patch.object(m,'owned_session',return_value=(root/'ownership',owned)),patch.object(m,'source_resource',return_value=resource),patch.object(m,'runtime',return_value=(333,'456',222,m.ENTRY)),patch.object(m,'generation',return_value=proof),patch.object(m,'identity',return_value=identity),patch.object(m,'write',side_effect=record),patch.object(m.subprocess,'run',return_value=SimpleNamespace(returncode=0))as execute:
  m.main(args);receipt=json.loads((runout/'watcher-receipt.json').read_text());assert receipt['lateObserverReadiness'] and receipt['phase']=='profile-finished' and execute.call_count==1 and phases.index('ready-waiting-first-multiple')<phases.index('failed-multiple-profile-requested') and receipt['privateIdentityRemoved'];results.append({'case':'late-identity-ready-published-before-profile-request-no-end-report-no-real-subprocess','passed':True})
finally:shutil.rmtree(root)
receipt={'diagnosticRegressionOnly':True,'watcherSha256':h((base/'watch_live.py').read_bytes()),'allPassed':all(r['passed']for r in results),'cases':results,'noTargetOrRealSubprocess':True,'featureAcceptance':False};(base/'generation-and-late-ready-smoke.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt,indent=2))
