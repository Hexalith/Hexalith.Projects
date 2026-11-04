from pathlib import Path
import datetime,hashlib,importlib.util,json,os,re,sys,tempfile,urllib.parse,urllib.request

root=Path('/tmp/story61-isolated-20261103-r5tqvlm3/projects')
private=Path('/tmp/supported-project-reads-private-koex5lio')
destination=Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/query-diagnostics-design-20261104')
spec=importlib.util.spec_from_file_location('story61_native_metric_observation',root/'tools/qualification/run_supported_project_reads.py')
m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
session=json.loads((private/'apphost-session.json').read_text())
assert m.owns_current_apphost(session)
assert hashlib.sha256((private/'fixture.json').read_bytes()).hexdigest()==session['configurationSha256']
fixture=json.loads((private/'fixture.json').read_text())['Projects']['SupportedReads']
identities=[fixture['Tenant'],fixture['ForeignTenant']]
configs,receipts=m.receiver_configurations(private/'discovery.json',private/'discovery.sqlite',session['_isolation'])
raw_directory=Path(tempfile.mkdtemp(prefix='story61-owned-native-metrics-live63-main-'));raw_directory.chmod(0o700)
observations=[]
for parts in m.sidecar_processes():
 if '--config' not in parts or parts[parts.index('--config')+1] not in {str(p) for p in configs.values()}:continue
 pid=int(parts[0]);started=m.process_start(pid)
 app=parts[parts.index('--app-id')+1]
 environment=dict(item.split(b'=',1) for item in Path('/proc',str(pid),'environ').read_bytes().split(b'\0') if b'=' in item)
 assert environment[b'NAMESPACE']==session['_isolation'].encode()
 assert '--metrics-port' in parts
 port=int(parts[parts.index('--metrics-port')+1]);assert m.sidecar_listens_at(pid,port,'127.0.0.1:'+str(port))
 with urllib.request.urlopen('http://127.0.0.1:'+str(port)+'/metrics',timeout=5) as response:
  assert response.status==200
  raw=response.read(8*1024*1024+1);assert len(raw)<=8*1024*1024
 output=raw_directory/(app+'.prom');output.write_bytes(raw);output.chmod(0o600)
 families=set();identity_labels=set();literal_resource_path_labels=set();exemplars=0;series=0
 for line in raw.decode().splitlines():
  if line.startswith('#') or not line.strip():continue
  series+=1
  if ' # {' in line:exemplars+=1
  match=re.match(r'([a-zA-Z_:][a-zA-Z0-9_:]*)\{(.*)\}\s',line)
  if not match:continue
  family,labels=match.groups()
  for label,value in re.findall(r'([a-zA-Z_][a-zA-Z0-9_]*)="((?:[^"\\]|\\.)*)"',labels):
   decoded=urllib.parse.unquote(value)
   if any(identity in decoded for identity in identities):identity_labels.add(label);families.add(family)
   if '/v1.0/actors/' in decoded and '{' not in decoded or '/v1.0/state/' in decoded and '{' not in decoded:
    literal_resource_path_labels.add(label);families.add(family)
 assert started is not None and m.process_start(pid)==started
 observations.append({'appId':app,'pid':pid,'startTicks':started,'nativeBinarySha256':hashlib.sha256(Path(parts[1]).read_bytes()).hexdigest(),
  'configurationSha256':hashlib.sha256(configs[app].read_bytes()).hexdigest(),'observedHttpStatus':200,'metricSeriesObserved':series,
  'managedTenantIdentityBearingLabelNames':sorted(identity_labels),'literalActorOrStatePathLabelNames':sorted(literal_resource_path_labels),
  'affectedMetricFamilies':sorted(families),'openMetricsExemplarsObserved':exemplars,'rawPrivateFileSha256':hashlib.sha256(raw).hexdigest()})
assert len(observations)==len(m.READ_APPS) and {r['appId'] for r in observations}==m.READ_APPS
assert m.owns_current_apphost(session)
report={'kind':'bounded-read-only-six-owned-native-metrics-exports','observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'provider':'live-63/main','sourceManifestSha256':'1f4e55a334e7949abdfb78b266d9cca57513641dc2efd83e2443d594baa291aa',
 'observations':sorted(observations,key=lambda r:r['appId']),'rawPrivateDirectory':str(raw_directory),
 'sourceChanged':False,'runtimeConfigurationChanged':False,'newRuntimeOrInstrumentationStarted':False,'wholeChainPrivacyAccepted':False,
 'limits':['One finite metrics scrape, not continuous monitoring.','No absence claim for metrics emitted after this observation.','Exemplar count covers this Prometheus response format only.']}
target=destination/'native-metric-exports.json';assert not target.exists();target.write_text(json.dumps(report,indent=2)+'\n')
(destination/'observe-native-metrics.py').write_bytes(Path(__file__).read_bytes())
print(json.dumps({'observedApps':len(observations),'managedTenantLabelsFound':any(r['managedTenantIdentityBearingLabelNames'] for r in observations),
 'literalActorOrStatePathLabelsFound':any(r['literalActorOrStatePathLabelNames'] for r in observations),'rawObservationsRetainedPrivately':True}))
