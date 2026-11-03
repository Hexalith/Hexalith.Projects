from pathlib import Path
import sqlite3,json,hashlib,urllib.parse,sys,collections
base=Path(sys.argv[1]);database=Path(sys.argv[2]);run=json.loads((base/'run.json').read_text());owned=next(x['resource'] for x in run['cleanup'] if x.get('resource','').endswith('-redishost'));tenant=owned.removesuffix('-redishost')+'-foreign';expected=tenant+':folders:'+tenant
c=sqlite3.connect('file:'+str(database)+'?mode=ro',uri=True);targets=collections.defaultdict(set)
for trace,span,url in c.execute("select trace_id,span_id,attribute_value from telemetry_span_attributes where attribute_key='url.full' and attribute_value like '%InspectSource%'"):
 p=urllib.parse.urlsplit(url).path.split('/')
 if 'AggregateActor' in p and urllib.parse.unquote(p[p.index('AggregateActor')+1])==expected:targets[trace].add(span)
assert targets
results=[]
def h(v):return hashlib.sha256(v.encode()).hexdigest()
for trace,native in targets.items():
 rows=c.execute('select r.resource_name,s.span_id,s.parent_span_id,s.name,s.start_time_ticks,s.end_time_ticks from telemetry_spans s join telemetry_resources r using(resource_id) where s.trace_id=? order by s.start_time_ticks',(trace,)).fetchall();by={x[1]:x for x in rows}
 for owner,root,parent,name,start,end in rows:
  if owner!='eventstore' or not(name.startswith('POST') and 'streams/read/workload' in name):continue
  descendants={root}
  while True:
   n=descendants|{x[1] for x in rows if x[2] in descendants}
   if n==descendants:break
   descendants=n
  if not descendants&native:continue
  spans=[];events=[]
  for resource,span,parent,name,a,b in rows:
   if span not in descendants:continue
   attrs=dict(c.execute('select attribute_key,attribute_value from telemetry_span_attributes where trace_id=? and span_id=?',(trace,span)));url=attrs.get('url.full',attrs.get('http.url',attrs.get('url.path','')))
   if '/v1.0/state/' in url:kind='catalog-state-'+attrs.get('http.request.method','unknown')
   elif '/actors/' in url:kind='native-atomic-metadata-pair' if 'InspectSourceMetadataPairAsync' in url else 'native-source-inspection' if 'InspectSourceProofAsync' in url else 'native-actor-state-read' if '/state/' in url else 'native-actor-method'
   elif '/streams/read/workload' in name:kind='gateway-workload-metadata'
   else:kind='operation-sha256:'+h(name)
   spans.append({'resource':resource,'spanSha256':h(span),'parentSpanSha256':h(parent),'operation':kind,'startMillisecondsRelativeToGateway':round((a-start)/10000,4),'durationMilliseconds':round((b-a)/10000,4),'recordedHttpStatus':attrs.get('http.response.status_code')})
   for eid,event,when in c.execute('select event_id,event_name,event_time_ticks from telemetry_span_events where trace_id=? and span_id=? and event_name like ?', (trace,span,'private.route_catalog.%')):
    values=dict(c.execute('select attribute_key,attribute_value from telemetry_span_event_attributes where event_id=?',(eid,)))
    allowed={'enabled','completed','request_canceled','capture_count','cache_hits','document_count','retained_bytes','elapsed_ms','phase','host_memo_enabled'}
    assert set(values)<=allowed
    events.append({'resource':resource,'spanSha256':h(span),'event':event,'startMillisecondsRelativeToGateway':round((when-start)/10000,4),'attributes':values})
  rootSpan=next(x for x in spans if x['spanSha256']==h(root));results.append({'traceSha256':h(trace),'rootSpanSha256':h(root),'gatewayDurationMilliseconds':rootSpan['durationMilliseconds'],'gatewayRecordedHttpStatus':rootSpan['recordedHttpStatus'],'spans':spans,'admissionEvents':events})
f={'nativeDatabase':str(database),'nativeDatabaseSha256':hashlib.sha256(database.read_bytes()).hexdigest(),'exactOwnedTenantOrganizationActorUrlMatched':True,'boundTenantHash':h(tenant).upper(),'requests':results,'gateway499Meaning':'Server-recorded cancellation; canceled client received no HTTP status.'};(base/'all-native-metadata-trace-facts.json').write_text(json.dumps(f,indent=2)+'\n')
for r in results:
 print({'gatewayMilliseconds':r['gatewayDurationMilliseconds'],'gatewayRecordedHttpStatus':r['gatewayRecordedHttpStatus'],'nativeRpcMilliseconds':[s['durationMilliseconds'] for s in r['spans'] if s['operation']=='native-atomic-metadata-pair'],'catalogStateStatuses':dict(collections.Counter(s['recordedHttpStatus'] for s in r['spans'] if s['operation'].startswith('catalog-state'))),'admissionEvents':r['admissionEvents']})
