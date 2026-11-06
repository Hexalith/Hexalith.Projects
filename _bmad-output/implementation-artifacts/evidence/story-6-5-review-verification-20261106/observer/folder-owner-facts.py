#!/usr/bin/env python3
"""Read-only, finite closed-facts observer for the exact held Story 6.5 fixture.
No domain query, activation, write, payload capture, or credential output.
"""
import argparse, datetime as dt, json, os, re, subprocess, sys, urllib.parse, urllib.request
from pathlib import Path

UTC=dt.timezone.utc

def read_private(path):
    path=Path(path)
    s=path.lstat()
    if path.is_symlink() or s.st_uid!=os.getuid() or s.st_mode&0o077 or not path.is_file():
        raise ValueError('private-file-refused')
    return json.loads(path.read_text())

def get(value,name,default=None):
    if not isinstance(value,dict): return default
    rows=[v for k,v in value.items() if k.lower()==name.lower()]
    if len(rows)>1: raise ValueError('duplicate-metadata-property')
    return rows[0] if rows else default

def stamp(value):
    if not isinstance(value,str): return None
    try:
        x=dt.datetime.fromisoformat(value.replace('Z','+00:00'))
        return x.astimezone(UTC) if x.tzinfo else None
    except ValueError: return None

def age(value,at):
    x=stamp(value)
    return round((at-x).total_seconds(),3) if x else None

def span_seconds(value):
    m=re.fullmatch(r'(?:(\d+)\.)?(\d+):(\d+):(\d+(?:\.\d+)?)',value)
    if not m: raise ValueError('threshold-format-refused')
    days,hours,minutes,seconds=m.groups()
    return (int(days or 0)*24+int(hours))*3600+int(minutes)*60+float(seconds)

def folder_threshold(isolation):
    found=[]
    for proc in Path('/proc').iterdir():
        if not proc.name.isdigit(): continue
        try:
            env=dict(part.split(b'=',1) for part in (proc/'environ').read_bytes().split(b'\0') if b'=' in part)
            if env.get(b'NAMESPACE')!=isolation.encode() or env.get(b'EventStore__DomainService__AppId')!=b'folders':continue
            cmd=(proc/'cmdline').read_bytes()
            if (proc/'exe').resolve().name!='Hexalith.Folders.Server' and not any(part.endswith(b'Hexalith.Folders.Server.dll') for part in cmd.split(b'\0')):continue
            found.append(env)
        except (OSError,ValueError):continue
    if len(found)!=1:raise ValueError('exact-folder-process-refused')
    env=found[0]
    key=b'Folders__TenantAccess__MutationFreshnessBudget'
    diagnostic=b'Folders__TenantAccess__DiagnosticStalenessBudget'
    return {'strictReadFreshnessBudgetSeconds':span_seconds(env[key].decode()) if key in env else 300.0,
        'diagnosticStalenessBudgetSeconds':span_seconds(env[diagnostic].decode()) if diagnostic in env else 1800.0,
        'strictBudgetSource':'exact-process-environment' if key in env else 'source-default-no-environment-override',
        'strictReadUsesDiagnosticBudget':False,'ageOperand':'LastEventTimestamp',
        'strictFreshComparison':'age <= budget','negativeAgeMalformed':True}

def tenant_facts(p,tenant,actor,at,budget):
    principals=get(p,'principals',{})
    member=principals.get(actor) if isinstance(principals,dict) else None
    entries=list(principals.items()) if isinstance(principals,dict) else []
    membership_shape=all(isinstance(v,dict) and get(v,'principalId')==k and isinstance(get(v,'role'),str) and bool(get(v,'role').strip()) for k,v in entries)
    valid_role=isinstance(get(member,'role'),str) and bool(get(member,'role').strip())
    a=age(get(p,'lastEventTimestamp'),at)
    return {'present':p is not None,'identityMatches':get(p,'tenantId')==tenant,
        'enabled':get(p,'enabled'),'replayConflict':get(p,'replayConflict'),'malformedEvidence':get(p,'malformedEvidence'),
        'membershipPresent':isinstance(member,dict),'membershipIdentityMatches':get(member,'principalId')==actor,
        'membershipRolePresent':valid_role,'membershipTenantOwner':get(member,'role')=='TenantOwner',
        'membershipActive':get(p,'enabled') is True and isinstance(member,dict) and get(member,'principalId')==actor and valid_role,
        'membershipActiveDefinition':'enabled Tenant plus present exact principal and nonblank role; source has no member Active field',
        'allMembershipEntriesWellFormed':membership_shape,'watermark':get(p,'watermark'),
        'projectionWatermarkMatches':get(p,'projectionWatermark')==tenant+':'+str(get(p,'watermark')),
        'lastEventTimestamp':get(p,'lastEventTimestamp') if stamp(get(p,'lastEventTimestamp')) else None,
        'lastEventAgeSeconds':a,'strictFreshnessBudgetSeconds':budget,
        'withinStrictFreshnessBudget':a is not None and 0<=a<=budget,
        'staleBySourceAgeRule':a is not None and a>budget,'futureBySourceAgeRule':a is not None and a<0,
        'hasSeparateLastVerifiedAt':get(p,'lastVerifiedAt') is not None,
        'lastVerificationAgeSeconds':age(get(p,'lastVerifiedAt'),at),
        'verificationInterpretation':'Tenant authorizer uses LastEventTimestamp, not a separate verification timestamp'}

def native_facts(meta,identity,projected):
    proof=get(meta,'sourceProof');head=get(meta,'currentSequence');floor=get(meta,'retainedFloor',1)
    proofhead=get(proof,'headSequence');inc=get(proof,'sourceIncarnation');digest=get(proof,'cumulativeDigest')
    identitymatch=all(get(proof,k)==v for k,v in zip(('tenantId','domain','aggregateId'),identity))
    headmatch=isinstance(head,int) and head>=0 and proofhead==head
    floormatch=isinstance(floor,int) and floor>=1 and isinstance(head,int) and floor-1<=head and get(proof,'retainedFloor')==floor
    incarnation=isinstance(inc,str) and re.fullmatch(r'[0-7][0-9A-HJKMNP-TV-Z]{25}',inc,re.I) is not None
    digestvalid=isinstance(digest,str) and re.fullmatch('[0-9a-f]{64}',digest) is not None
    message= get(proof,'headMessageId')
    headid=(message is None) if head==0 else isinstance(message,str) and bool(message.strip())
    return {'metadataPresent':meta is not None,'currentSequence':head,'projectionSequence':projected,
        'nativeHeadEqualsProjectionSequence':isinstance(head,int) and head==projected,
        'lastModified':get(meta,'lastModified') if stamp(get(meta,'lastModified')) else None,
        'sourceErasurePending':get(meta,'sourceErasurePending',False),'proofPresent':isinstance(proof,dict),
        'proofCodecSupported':get(proof,'codecVersion')==1,'proofIdentityMatches':identitymatch,
        'proofHeadEqualsMetadata':headmatch,'proofRetainedFloorEqualsMetadata':floormatch,
        'proofIncarnationShapeValid':incarnation,'proofDigestShapeValid':digestvalid,'proofHeadMessagePresenceValid':headid,
        'proofErased':get(proof,'erased'),'nativeInspectionExecuted':False,
        'interpretation':'Provider metadata comparison only; no actor inspection or owner authorization acceptance'}

def run(output,hold):
    access=read_private(Path(hold)/'access.json');matches=[]
    for candidate in Path('/tmp').glob('supported-project-reads-private-*/fixture.json'):
        try:
            cfg=read_private(candidate);fixture=cfg.get('Projects',{}).get('SupportedReads',{})
            if fixture.get('Tenant')==access.get('tenant'):matches.append((candidate,cfg,fixture))
        except (OSError,ValueError):continue
    if len(matches)!=1:raise ValueError('exact-fixture-refused')
    path,cfg,fixture=matches[0]
    endpoint_path=path.parent/'endpoints.json';es=endpoint_path.lstat();ps=path.parent.stat()
    if endpoint_path.is_symlink() or not endpoint_path.is_file() or es.st_uid!=os.getuid() or es.st_mode&0o022 or ps.st_uid!=os.getuid() or ps.st_mode&0o077:raise ValueError('private-endpoint-file-refused')
    endpoints=json.loads(endpoint_path.read_text())
    tenant=fixture['Tenant'];actor=fixture['Browser']['PrimaryActor'];isolation=fixture['IsolationId']
    threshold=folder_threshold(isolation)
    rid=endpoints['redis-container-id']
    if re.fullmatch('[0-9a-f]{64}',rid) is None:raise ValueError('owned-provider-refused')
    inspect=subprocess.run(['docker','inspect','--format','{{json .}}',rid],capture_output=True,timeout=15)
    if inspect.returncode:raise ValueError('owned-provider-inspect-failed')
    container=json.loads(inspect.stdout)
    if container.get('Id')!=rid or container.get('Config',{}).get('Labels',{}).get('hexalith.supported-reads')!=isolation:raise ValueError('provider-ownership-refused')
    def physical(key):
        r=subprocess.run(['docker','exec',rid,'redis-cli','-e','--raw','-D','','HGET',key,'data'],capture_output=True,timeout=15)
        if r.returncode:raise ValueError('provider-read-failed')
        if len(r.stdout)>1048576:raise ValueError('provider-read-size-refused')
        return json.loads(r.stdout) if r.stdout else None
    base=endpoints['eventstore-dapr-cli'].rstrip('/')
    url=urllib.parse.urlparse(base)
    if url.scheme!='http' or url.hostname not in ('localhost','127.0.0.1','::1') or not url.port:raise ValueError('state-endpoint-refused')
    def logical(key):
        req=urllib.request.Request(base+'/v1.0/state/statestore/'+urllib.parse.quote(key,safe=''))
        with urllib.request.urlopen(req,timeout=15) as r:
            data=r.read(1048577)
            if len(data)>1048576:raise ValueError('state-read-size-refused')
            p=json.loads(data) if data else None
        # Do not choose a batch candidate/previous value or emulate SDK authority visibility.
        if isinstance(p,dict) and '$hxrmb' in p:raise ValueError('logical-value-needs-sdk-visibility')
        return p
    observed=dt.datetime.now(UTC)
    tenant_p=logical('projection:folders:tenant-access:'+tenant)
    org=logical('projection:folders:organization-permissions:'+tenant)
    folder_models=[logical('projection:folders:folder-permissions:'+tenant+':'+f) for f in ('folder-project-a','folder-project-b')]
    native=[]
    scopes=[('tenant-source',('system','tenants',tenant),get(tenant_p,'watermark')),
        ('organization-source',(tenant,'folders',tenant),get(org,'lastSequence')),
        ('folder-a-source',(tenant,'folders','folder-project-a'),get(folder_models[0],'lastSequence')),
        ('folder-b-source',(tenant,'folders','folder-project-b'),get(folder_models[1],'lastSequence'))]
    for label,identity,seq in scopes:
        actorid=':'.join(identity);key='eventstore||AggregateActor||'+actorid+'||'+actorid+':metadata'
        row={'scope':label};row.update(native_facts(physical(key),identity,seq));native.append(row)
    finished=dt.datetime.now(UTC)
    rows=[]
    for label,p in zip(('folder-a','folder-b'),folder_models):
        rows.append({'scope':label,'present':p is not None,'identityMatches':get(p,'tenantId')==tenant and get(p,'organizationId')==tenant,
            'isCreated':get(p,'isCreated'),'isArchived':get(p,'isArchived'),'lastSequence':get(p,'lastSequence'),
            'projectedAt':get(p,'projectedAt') if stamp(get(p,'projectedAt')) else None,
            'projectedAgeSeconds':age(get(p,'projectedAt'),observed),
            'exactUserReadMetadataGrantPresent':any(get(x,'principalId')==actor and get(x,'principalKind') in (0,'User') and get(x,'action')=='read_metadata' and get(x,'isGranted') is True for x in get(p,'overrides',[]))})
    report={'kind':'Read-only closed provider and state facts; no owner authority accepted','observedAtUtc':observed.isoformat(),'finishedAtUtc':finished.isoformat(),
        'sourceStable':True,'rawDomainPayloadRetained':False,'nativeQueriesIssued':False,'stateWritesIssued':False,
        'threshold':threshold,'tenant':tenant_facts(tenant_p,tenant,actor,observed,threshold['strictReadFreshnessBudgetSeconds']),
        'organization':{'present':org is not None,'identityMatches':get(org,'tenantId')==tenant and get(org,'organizationId')==tenant,
            'isInitialized':get(org,'isInitialized'),'lastSequence':get(org,'lastSequence'),'projectedAgeSeconds':age(get(org,'projectedAt'),observed)},
        'folders':rows,'nativeSources':native,'productionReadiness':False}
    serialized=json.dumps(report,indent=2)+'\n'
    protected=[tenant,actor,isolation,access.get('password'),fixture.get('ForeignTenant'),fixture.get('Browser',{}).get('ForeignActor')]
    if any(v and v in serialized for v in protected) or re.search(r'eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}',serialized):raise ValueError('output-protection-refused')
    descriptor=os.open(output,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o600)
    with os.fdopen(descriptor,'w') as f:f.write(serialized)
    print(json.dumps({'result':'recorded','tenantStaleBySourceAgeRule':report['tenant']['staleBySourceAgeRule'],
        'lastEventAgeSeconds':report['tenant']['lastEventAgeSeconds'],'strictFreshnessBudgetSeconds':threshold['strictReadFreshnessBudgetSeconds'],
        'membershipActive':report['tenant']['membershipActive'],'nativeHeadMatches':[x['nativeHeadEqualsProjectionSequence'] for x in native]}))

def self_test():
    at=dt.datetime(2026,1,1,0,10,tzinfo=UTC)
    p={'tenantId':'private-tenant','enabled':True,'principals':{'private-actor':{'principalId':'private-actor','role':'TenantOwner'}},'lastEventTimestamp':'2026-01-01T00:00:00Z','watermark':2,'projectionWatermark':'private-tenant:2','replayConflict':False,'malformedEvidence':False}
    a=tenant_facts(p,'private-tenant','private-actor',at,300)
    assert a['staleBySourceAgeRule'] and a['membershipActive'] and a['lastEventAgeSeconds']==600 and a['projectionWatermarkMatches']
    assert not tenant_facts(p,'private-tenant','absent',at,300)['membershipPresent']
    p['lastEventTimestamp']='2026-01-01T00:05:00Z';assert tenant_facts(p,'private-tenant','private-actor',at,300)['withinStrictFreshnessBudget']
    assert span_seconds('7.00:00:00')==604800 and span_seconds('00:05:00')==300
    print('Temp observer offline controls passed; no live access performed.')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('output',nargs='?');parser.add_argument('--hold',default='/tmp/story65-a11y-live-hold');parser.add_argument('--self-test',action='store_true');args=parser.parse_args()
    try:
        if args.self_test:self_test()
        elif args.output:run(args.output,args.hold)
        else:parser.error('output is required')
    except Exception as failure:
        print(json.dumps({'result':'observer-failed','failureType':type(failure).__name__,'failureCode':str(failure) if str(failure) in {'private-file-refused','exact-fixture-refused','private-endpoint-file-refused','exact-folder-process-refused','owned-provider-refused','owned-provider-inspect-failed','provider-ownership-refused','provider-read-failed','provider-read-size-refused','state-endpoint-refused','state-read-size-refused','logical-value-needs-sdk-visibility','output-protection-refused','threshold-format-refused'} else 'unclassified','detailRetained':False}),file=sys.stderr);sys.exit(1)
