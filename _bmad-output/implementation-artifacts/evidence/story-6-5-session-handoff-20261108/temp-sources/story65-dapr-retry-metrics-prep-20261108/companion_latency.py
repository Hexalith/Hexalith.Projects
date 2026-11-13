"""Optional same-body peer-latency summaries. No bucket metrics or extra HTTP."""
import decimal,re
from metrics_core import BODY_CAP,LINE_CAP,SERIES_CAP,ROLES,Refused,labels
BASE='dapr_runtime_service_invocation_res_recv_latency_ms'
NAMES={BASE+'_sum':'sumMilliseconds',BASE+'_count':'count'}

def parse_latency(body,role):
    if role not in ROLES or len(body)>BODY_CAP:raise Refused('companion-body-or-role-refused')
    try:lines=body.decode('utf-8').splitlines()
    except UnicodeError:raise Refused('companion-encoding-refused')from None
    if len(lines)>SERIES_CAP:raise Refused('metrics-line-cap')
    output={'familyDeclared':False,'samplesSeen':0,'discardedSeries':0,'series':{}}
    for line in lines:
        if len(line.encode('utf-8'))>LINE_CAP:raise Refused('metrics-line-cap')
        if line==f'# TYPE {BASE} histogram':output['familyDeclared']=True;continue
        name=line.split('{',1)[0].split(' ',1)[0]
        if name not in NAMES:continue
        output['samplesSeen']+=1;match=re.fullmatch(re.escape(name)+r'\{(.*)\} ([^ ]+)',line)
        if not match:raise Refused('malformed-companion-sample')
        values=labels(match[1])
        if set(values)!={'app_id','src_app_id','status'}or values['app_id']!=role or values['src_app_id']not in ROLES or not re.fullmatch(r'0|[1-9][0-9]{0,2}',values['status'])or not(0<=int(values['status'])<=16 or 100<=int(values['status'])<=599):
            output['discardedSeries']+=1;continue
        try:value=decimal.Decimal(match[2])
        except decimal.InvalidOperation:raise Refused('companion-number-refused')from None
        if not value.is_finite()or value<0 or value>10**18:raise Refused('companion-number-refused')
        field=NAMES[name]
        if field=='count'and value!=value.to_integral_value():raise Refused('companion-count-refused')
        key=values['src_app_id']+'/'+values['status'];row=output['series'].setdefault(key,{})
        if field in row:raise Refused('duplicate-companion-series')
        row[field]=int(value)if field=='count'else float(value)
    return output

def delta_latency(before,after,same_start):
    rows=[]
    for bucket in sorted(before['series'].keys()|after['series'].keys()):
        left=before['series'].get(bucket,{});right=after['series'].get(bucket,{});mean=None;count=None
        if not same_start:category='identity-changed'
        elif set(left)!={'sumMilliseconds','count'}or set(right)!={'sumMilliseconds','count'}:category='series-unobserved'
        elif left['sumMilliseconds']<=0 or left['count']<=0 or right['sumMilliseconds']<=0 or right['count']<=0:category='positive-baseline-or-endpoint-unobserved'
        elif right['sumMilliseconds']<left['sumMilliseconds']or right['count']<left['count']:category='counter-decreased'
        else:
            count=right['count']-left['count'];total=right['sumMilliseconds']-left['sumMilliseconds']
            if count>0:mean=total/count;category='observed-positive-count-delta'
            elif total==0:category='observed-unchanged'
            else:category='inconsistent-sum-count';count=None
        rows.append({'peerStatusBucket':bucket,'category':category,'deltaCount':count,'deltaMeanMilliseconds':mean})
    return {'beforeFamilyDeclared':before['familyDeclared'],'afterFamilyDeclared':after['familyDeclared'],'rows':rows,'missingIsZero':False,'aggregatePeerObservationsNotRequestsOrCrossServiceJoins':True}
