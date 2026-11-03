from pathlib import Path
import sqlite3,json,hashlib,collections,re,urllib.parse
base=Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-45');db=Path('/home/administrator/.aspire/dashboard/runs/20261103T105703707Z/dashboard.db');c=sqlite3.connect('file:'+str(db)+'?mode=ro',uri=True)
h=lambda x:hashlib.sha256(x.encode()).hexdigest()
failed=c.execute("select s.trace_id,s.span_id from telemetry_spans s join telemetry_resources r using(resource_id) join telemetry_span_attributes a on a.trace_id=s.trace_id and a.span_id=s.span_id where r.resource_name='Hexalith.Folders' and s.name='POST /query' and a.attribute_key='http.response.status_code' and a.attribute_value='499'").fetchall();assert len(failed)==2
trace,folder=failed[1];rows=c.execute('select r.resource_name,s.span_id,s.parent_span_id,s.name,s.start_time_ticks,s.end_time_ticks from telemetry_spans s join telemetry_resources r using(resource_id) where trace_id=? order by start_time_ticks',(trace,)).fetchall();start=min(x[4] for x in rows);by={r[1]:r for r in rows}
attrs={r[1]:dict(c.execute('select attribute_key,attribute_value from telemetry_span_attributes where trace_id=? and span_id=?',(trace,r[1]))) for r in rows}
def descendants(root):
 ids={root}
 while True:
  n=ids|{r[1] for r in rows if r[2] in ids}
  if n==ids:return ids
  ids=n
def classify(r):
 a=attrs[r[1]];u=a.get('url.full','');n=r[3]
 if '/v1.0/state/' in u:return 'catalog-state-'+a.get('http.request.method','unknown')
 if '/actors/' in u:return 'native-atomic-pair' if 'InspectSourceMetadataPairAsync' in u else 'native-actor-state' if '/state/' in u else 'native-actor-method'
 if 'streams/read/workload' in u or 'streams/read/workload' in n:return 'metadata-transport'
 if 'owner-authority/workload' in u or 'owner-authority/workload' in n:return 'owner-query-transport'
 if n=='POST /query':return 'domain-query'
 return 'operation-sha256:'+h(n)
spans=[];events=[]
for r in rows:
 resource,sp,pa,n,a,b=r;item={'resource':resource,'spanSha256':h(sp),'parentSpanSha256':h(pa),'kind':classify(r),'startMs':round((a-start)/10000,4),'durationMs':round((b-a)/10000,4),'serverOrClientRecordedHttpStatus':attrs[sp].get('http.response.status_code')};spans.append(item)
 for eid,ev,t in c.execute("select event_id,event_name,event_time_ticks from telemetry_span_events where trace_id=? and span_id=? and event_name like 'private.route_catalog.%'",(trace,sp)):
  values=dict(c.execute('select attribute_key,attribute_value from telemetry_span_event_attributes where event_id=?',(eid,)));assert set(values)<={'enabled','completed','request_canceled','capture_count','cache_hits','document_count','retained_bytes','elapsed_ms','phase','host_memo_enabled'};events.append({'resource':resource,'spanSha256':h(sp),'event':ev,'startMs':round((t-start)/10000,4),'attributes':values})
metadata=[]
for r in rows:
 if r[0]!='Hexalith.Folders' or classify(r)!='metadata-transport':continue
 ids=descendants(r[1]);scope=[x for x in spans if any(x['spanSha256']==h(s) for s in ids)];native=[x for x in scope if x['kind']=='native-atomic-pair' and x['resource']=='eventstore'];state=collections.Counter((x['resource'],x['serverOrClientRecordedHttpStatus']) for x in scope if x['kind']=='catalog-state-POST');metadata.append({'clientStartMs':round((r[4]-start)/10000,4),'clientDurationMs':round((r[5]-r[4])/10000,4),'clientReceivedHttpStatus':attrs[r[1]].get('http.response.status_code'),'nativeRpcMs':[x['durationMs'] for x in native],'statePostStatuses':[{'resource':k[0],'status':k[1],'count':n} for k,n in sorted(state.items(),key=str)]})
raw=Path('/tmp/story61-owned-diagnostics-live45-d4rkkme4/folders.json').read_bytes();logs=json.loads(raw)['logs'];heldExceptions=0
for x in logs:
 content=x.get('content','');i=content.find('{')
 try:v=json.loads(content[i:])
 except ValueError:continue
 heldExceptions+=int('RouteCatalogHeldMetadataDispatch.InvokeAsync' in v.get('Exception',''))
facts={'nativeDatabase':str(db),'nativeDatabaseSha256':hashlib.sha256(db.read_bytes()).hexdigest(),'traceSha256':h(trace),'failedFolderScopeSha256':h(folder),'actualHeldInvokeCancellationStackObserved':heldExceptions>0,'heldInvokeCancellationStackCount':heldExceptions,'heldStackCountIsAcrossRetainedFolderResourceLog':True,'rawFolderLogSha256':hashlib.sha256(raw).hexdigest(),'rawDiagnosticsRemainOwnerPrivate':True,'callbackBudgetMilliseconds':2000,'matrixRowsCompleted':0,'metadataRequests':metadata,'spans':spans,'catalogPhaseEvents':events,'cancellationInterpretation':'Recorded499 is server cancellation. Final Folder client received no HTTP status; late native completion is not accepted authority.','scopeQualificationEvidence':'Fresh validation phase sets without per-send Folder state acquisition/CAS or decode-scope event, plus actual held InvokeAsync cancellation stack; two original inbound scopes still acquire their own fences.'};(base/'open-1-transport-facts.json').write_text(json.dumps(facts,indent=2)+'\n');print('Metadata requests',metadata);print('Folder state HTTP totals',collections.Counter((classify(r),attrs[r[1]].get('http.response.status_code')) for r in rows if r[0]=='Hexalith.Folders' and classify(r).startswith('catalog-state')));print('Held invocation cancellation stack count',heldExceptions)
