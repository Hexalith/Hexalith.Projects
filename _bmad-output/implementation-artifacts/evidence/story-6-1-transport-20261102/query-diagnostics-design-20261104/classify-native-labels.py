from pathlib import Path
import base64,hashlib,json,re,urllib.parse

canonical=Path('/home/administrator/projects/hexalith/projects')
packet=canonical/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/query-diagnostics-design-20261104'
receipt=json.loads((packet/'native-metric-exports.json').read_text())
run=canonical/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-63/main/run.json'
provider=json.loads(run.read_text())
redis=[row['resource'] for row in provider['cleanup'] if row['resource'].endswith('-redishost')]
assert len(redis)==1
isolation=redis[0].removesuffix('-redishost')
assert hashlib.sha256(isolation.encode()).hexdigest().upper()==provider['providerIsolationSha256']
tenants=[isolation+'-tenant',isolation+'-foreign']
projects=['project-a','project-b'];folders=['folder-project-a','folder-project-b']
targets=tenants+projects+folders
for tenant in tenants:
 targets += ['projects:reads:v1:'+str(len(tenant))+':'+tenant+':index']
 for project in projects:
  targets += ['projects:reads:v1:'+str(len(tenant))+':'+tenant+':'+str(len(project))+':'+project,
              tenant+':projects:'+project,tenant+':folders:folder-'+project]
forms=set(targets)
for value in targets:
 raw=value.encode();forms.update([urllib.parse.quote(value,safe=''),urllib.parse.quote(urllib.parse.quote(value,safe=''),safe=''),
  base64.b64encode(raw).decode(),base64.b64encode(raw).decode().rstrip('='),base64.urlsafe_b64encode(raw).decode().rstrip('='),
  raw.hex(),raw.hex().upper(),hashlib.sha256(raw).hexdigest(),hashlib.sha256(raw).hexdigest().upper()])
def decoded_values(value):
 values={value}
 for _ in range(3):
  extra=set()
  for item in values:
   extra.add(urllib.parse.unquote(item))
   if len(item)>=8 and re.fullmatch(r'[A-Za-z0-9_+/=-]+',item):
    try:extra.add(base64.urlsafe_b64decode(item+'='*((-len(item))%4)).decode())
    except (ValueError,UnicodeDecodeError):pass
  values.update(extra)
 return values
def matches(value):return any(form in item for item in decoded_values(value) for form in forms)
controls=[targets[0],urllib.parse.quote(targets[-1],safe=''),base64.urlsafe_b64encode(targets[-1].encode()).decode().rstrip('='),
 urllib.parse.quote(base64.urlsafe_b64encode(targets[-1].encode()).decode(),safe=''),hashlib.sha256(targets[-1].encode()).hexdigest()]
assert all(matches(value) for value in controls)
observations=[]
for row in receipt['observations']:
 raw=(Path(receipt['rawPrivateDirectory'])/(row['appId']+'.prom')).read_bytes()
 assert hashlib.sha256(raw).hexdigest()==row['rawPrivateFileSha256']
 labels=set();matching_labels=set();values=0;shapes=set();actor_coordinates=0;state_keys=0
 for line in raw.decode().splitlines():
  if line.startswith('#'):continue
  for label,encoded in re.findall(r'([a-zA-Z_][a-zA-Z0-9_]*)="((?:[^"\\]|\\.)*)"',line):
   value=json.loads('"'+encoded+'"');labels.add(label);values+=1
   if matches(value):matching_labels.add(label)
   if label=='path':
    path=urllib.parse.unquote(value);pieces=path.strip('/').split('/')
    if len(pieces)>=5 and pieces[:2]==['v1.0','actors']:
     if pieces[3]!='{id}':actor_coordinates+=1
     if pieces[3]=='{id}' and re.fullmatch(r'[A-Za-z][A-Za-z0-9]{1,64}',pieces[2]):shapes.add('/'+'/'.join(pieces))
    if len(pieces)>=4 and pieces[:2]==['v1.0','state'] and pieces[3] not in {'bulk','query'}:state_keys+=1
 observations.append({'appId':row['appId'],'labelNamesExamined':sorted(labels),'labelValuesExamined':values,
  'knownCoordinateFormMatchLabelNames':sorted(matching_labels),'actualActorIdentityPathSeries':actor_coordinates,
  'actualStateKeyPathSeries':state_keys,'exactActorRouteShapes':sorted(shapes),'rawPrivateFileSha256':row['rawPrivateFileSha256']})
target=packet/'native-metric-all-label-classification.json';assert not target.exists()
target.write_text(json.dumps({'providerReportSha256':hashlib.sha256(run.read_bytes()).hexdigest(),
 'metricReceiptSha256':hashlib.sha256((packet/'native-metric-exports.json').read_bytes()).hexdigest(),
 'providerIdentityValidated':True,'coordinateDerivation':'Launcher-owned immutable Redis receipt -> verified provider isolation -> existing Tenant/ForeignTenant seeding contract and exact supported read/aggregate key shapes.',
 'encodingControlsPassed':len(controls),'knownCoordinateFormsChecked':len(forms),'allLabelFieldsExamined':True,'observations':observations,
 'limits':['One retained six-sidecar scrape before final-source privacy changes.','Known seed identities/read keys and encoding/hash transforms; not a proof for every unobserved future key.',
 'Prometheus-format exemplar absence does not establish OTLP exemplar absence.','Aggregate activity counts remain observable by approved policy.']},indent=2)+'\n')
(packet/'classify-native-labels.py').write_bytes(Path(__file__).read_bytes())
print(json.dumps({'apps':len(observations),'allLabelFieldsExamined':True,'matches':sum(bool(row['knownCoordinateFormMatchLabelNames']) for row in observations),
 'actualActorCoordinates':sum(row['actualActorIdentityPathSeries'] for row in observations),'actualStateKeys':sum(row['actualStateKeyPathSeries'] for row in observations),'encodingControls':len(controls)}))
