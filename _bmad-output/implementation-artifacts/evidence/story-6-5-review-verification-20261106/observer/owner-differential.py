from pathlib import Path
import json,urllib.request,urllib.parse,ssl,subprocess,hashlib,time,datetime,sys
root=Path.cwd();hold=Path('/tmp/story65-a11y-live-hold');access=json.loads((hold/'access.json').read_text());candidates=[]
for f in Path('/tmp').glob('supported-project-reads-private-*/fixture.json'):
 if f.stat().st_uid!=__import__('os').getuid() or f.stat().st_mode&0o077:continue
 cfg=json.loads(f.read_text());data=cfg.get('Projects',{}).get('SupportedReads',{})
 if data.get('Tenant')==access['tenant']:candidates.append((f,cfg,data))
assert len(candidates)==1
file,cfg,fixture=candidates[0];graph=json.loads(subprocess.check_output(['aspire','describe','--apphost',str(root/'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'),'--format','Json','--non-interactive'],text=True))
def endpoint(name):
 urls=[u['url'] for r in graph['resources'] if r['displayName']==name for u in r['urls'] if u['name']=='http'];assert len(urls)==1;return urls[0]
context=ssl.create_default_context(cafile=fixture['TrustBundle']);body=urllib.parse.urlencode({'grant_type':'password','client_id':'supported-read-reader','username':'actor-1','password':access['password'],'scope':'openid'}).encode()
request=urllib.request.Request(fixture['Browser']['Authority']+'/protocol/openid-connect/token',body,{'Content-Type':'application/x-www-form-urlencoded'})
with urllib.request.urlopen(request,context=context,timeout=15) as response:credential=json.load(response)['access_token']
observation_url=endpoint('browser-read-observer')+'/api/v1/live-fixtures/graphs/'+fixture['IsolationId']+'/persisted-observation'
def observe():
 with urllib.request.urlopen(observation_url,timeout=15) as r:return json.load(r)
before=observe();results=[]
for name,target,kind,payload in [('empty-scope-1','projects','ResolveProjectFromAttachments',{'attachments':[],'includeArchived':False}),('open-1','project-a','GetProject',{'inspectDescriptiveMetadata':False,'inspectQuarantined':False}),('single-folder','projects','ResolveProjectFromAttachments',{'attachments':[{'referenceKind':'folder','referenceId':'folder-project-a'}],'includeArchived':False}),('empty-scope-2','projects','ResolveProjectFromAttachments',{'attachments':[],'includeArchived':False}),('open-2','project-a','GetProject',{'inspectDescriptiveMetadata':False,'inspectQuarantined':False}),('multiple-folder','projects','ResolveProjectFromAttachments',{'attachments':[{'referenceKind':'folder','referenceId':'folder-project-a'},{'referenceKind':'folder','referenceId':'folder-project-b'}],'includeArchived':False})]:
 query={'tenant':fixture['Tenant'],'domain':'projects','aggregateId':target,'queryType':'Hexalith.Projects.Queries.'+kind+'.v1','payload':payload}
 req=urllib.request.Request(endpoint('eventstore')+'/api/v1/queries',json.dumps(query).encode(),{'Content-Type':'application/json','Authorization':'Bearer '+credential});start=time.monotonic();item={'case':name,'atUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
 try:
  with urllib.request.urlopen(req,timeout=20) as r:status=r.status;delivered=json.load(r)
  item.update(httpStatus=status,wireElapsedMilliseconds=(time.monotonic()-start)*1000,success=delivered.get('success'));p=delivered.get('payload') or {}
  if kind=='GetProject':
   s=p.get('snapshot') or {};item.update(responseState=s.get('responseState'),components=[{k:c.get(k) for k in ['name','included','freshness','reason']} for c in s.get('components',[])])
  else:item.update(result=p.get('result'),incomplete=p.get('incomplete'),candidateCount=len(p.get('candidates',[])))
 except urllib.error.HTTPError as error:item.update(httpStatus=error.code,wireElapsedMilliseconds=(time.monotonic()-start)*1000)
 results.append(item)
after=observe();out={'kind':'Actual public authenticated differential; no privacy suppression disabled','cases':results,'domainUnchanged':before['domain']==after['domain'],'auditUnchanged':before.get('audit')==after.get('audit'),'productionReadiness':False}
output=Path(sys.argv[1]);output.write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
