"""FILE-only prototype. Raw metrics/labels and endpoint coordinates stay in RAM."""
from pathlib import Path
import decimal,errno,ipaddress,os,re,selectors,socket,time

FAMILIES={'dapr_resiliency_count':'retry_execution','dapr_resiliency_activations_total':'retry_activation'}
LABELS={'app_id','name','policy','namespace','flow_direction','target','status'}
ROLES={'eventstore','tenants','projects','projects-workers','folders','folders-workers'}
RESOURCE_BUCKETS={'projects-local-resiliency':'baseline','supported-reads-owner-deadline':'owner','supported-reads-project-state':'project_state'}
ROLE_RESOURCES={'eventstore':{'supported-reads-owner-deadline'},'projects':{'supported-reads-project-state'},'tenants':{'projects-local-resiliency'},'projects-workers':{'projects-local-resiliency'},'folders':set(),'folders-workers':set()}
BODY_CAP=2*1024*1024; HEADER_CAP=16384; LINE_CAP=16384; SERIES_CAP=20000; WALL_SECONDS=2

class Refused(Exception):
    """Only a fixed synthetic category may cross the collector boundary."""
    pass

def labels(text):
    result={};position=0
    while position<len(text):
        m=re.match(r'([A-Za-z_][A-Za-z0-9_]*)="((?:[^"\\\r\n]|\\[\\n"])*)"',text[position:])
        if not m:raise Refused('malformed-selected-metric')
        key,raw=m.groups()
        if key in result:raise Refused('duplicate-selected-label')
        result[key]=re.sub(r'\\([\\n"])',lambda v:'\n'if v[1]=='n'else v[1],raw)
        position+=m.end()
        if position<len(text):
            if text[position]!=',':raise Refused('malformed-selected-metric')
            position+=1
            if position==len(text):raise Refused('malformed-selected-metric')
    return result

def parse(body,role):
    """One exact view label set, no aliases or summed duplicate series."""
    if role not in ROLES or len(body)>BODY_CAP:raise Refused('body-or-role-refused')
    try:text=body.decode('utf-8')
    except UnicodeError:raise Refused('metrics-encoding-refused')from None
    families={value:{'familyDeclared':False,'samplesSeen':0,'discardedSeries':0,'series':{}}for value in FAMILIES.values()}
    lines=text.splitlines()
    if len(lines)>SERIES_CAP:raise Refused('metrics-line-cap')
    for line in lines:
        if len(line.encode('utf-8'))>LINE_CAP:raise Refused('metrics-line-cap')
        if not line:continue
        if line.startswith('# TYPE '):
            pieces=line.split()
            if len(pieces)==4 and pieces[2]in FAMILIES:
                if pieces[3]!='counter':raise Refused('selected-family-type-refused')
                families[FAMILIES[pieces[2]]]['familyDeclared']=True
            continue
        name=line.split('{',1)[0].split(' ',1)[0]
        if name not in FAMILIES:continue
        family=families[FAMILIES[name]];family['samplesSeen']+=1
        match=re.fullmatch(re.escape(name)+r'\{(.*)\} ([^ ]+)',line)
        if not match:raise Refused('malformed-selected-metric')
        values=labels(match[1])
        if set(values)!=LABELS or values['app_id']!=role or values['name']not in ROLE_RESOURCES[role] or values['policy']!='retry' or values['flow_direction']!='outbound' or values['target']!='app_eventstore' or values['namespace']!='' or values['status']!='':
            family['discardedSeries']+=1;continue
        try:number=decimal.Decimal(match[2])
        except decimal.InvalidOperation:raise Refused('selected-counter-number-refused')from None
        if not number.is_finite()or number<0 or number!=number.to_integral_value()or number>2**63-1:raise Refused('selected-counter-number-refused')
        bucket=RESOURCE_BUCKETS[values['name']]+'/empty/unspecified'
        if bucket in family['series']:raise Refused('duplicate-selected-series')
        family['series'][bucket]=int(number)
    return families

def delta(before,after,same_start):
    result={}
    for name in FAMILIES.values():
        left=before[name]['series'];right=after[name]['series'];rows=[]
        for bucket in sorted(left.keys()|right.keys()):
            value=None
            if not same_start:category='identity-changed'
            elif bucket not in left or bucket not in right:category='newly-observed-positive'if bucket not in left and right.get(bucket,0)>0 else'no-series'
            elif right[bucket]<left[bucket]:category='counter-decreased'
            elif left[bucket]<=0 or right[bucket]<=0:category='nonpositive-count-unobservable'
            else:category='positive-delta'if right[bucket]>left[bucket]else'observed-unchanged';value=right[bucket]-left[bucket]
            rows.append({'bucket':bucket,'category':category,'delta':value})
        result[name]={'beforeFamilyDeclared':before[name]['familyDeclared'],'afterFamilyDeclared':after[name]['familyDeclared'],'beforeSelectedSeries':len(left),'afterSelectedSeries':len(right),'deltas':rows,'missingIsZero':False,'deltaCountsFirstNotifyIncrementsNotRetryAttempts':True}
    return result

def process(pid):
    try:
        parts=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()
        return {'state':parts[0],'parent':int(parts[1]),'group':int(parts[2]),'session':int(parts[3]),'start':parts[19]}
    except(FileNotFoundError,ProcessLookupError):return None

def stable_process(row):return tuple(row[k]for k in ['start','parent','group','session'])

def listener(pid,start,port):
    """Return one numeric loopback destination after same-PID socket-FD/inode proof."""
    current=process(pid)
    if current is None or current['state']=='Z'or current['start']!=str(start):raise Refused('sidecar-start-changed')
    inodes=set()
    for fd in Path('/proc',str(pid),'fd').iterdir():
        try:value=os.readlink(fd)
        except FileNotFoundError:continue
        match=re.fullmatch(r'socket:\[(\d+)\]',value)
        if match:inodes.add(match[1])
    found=set()
    for family in ['tcp','tcp6']:
        for line in Path('/proc',str(pid),'net',family).read_text().splitlines()[1:]:
            fields=line.split();address,raw_port=fields[1].split(':')
            if fields[3]!='0A'or int(raw_port,16)!=port or fields[9]not in inodes:continue
            raw=bytes.fromhex(address);packed=raw[::-1]if family=='tcp'else b''.join(raw[i:i+4][::-1]for i in range(0,16,4));bound=ipaddress.ip_address(packed)
            if not(bound.is_loopback or bound.is_unspecified):continue
            destination='127.0.0.1'if family=='tcp'else'::1'
            if bound.is_loopback:destination=str(bound)
            found.add((socket.AF_INET if family=='tcp'else socket.AF_INET6,destination,fields[9]))
    after=process(pid)
    if after is None or after['state']=='Z'or stable_process(after)!=stable_process(current):raise Refused('sidecar-start-changed')
    if not found:raise Refused('metrics-listener-unobservable')
    # Fixed preference within proven listeners; never probe guessed alternate ports.
    return sorted(found,key=lambda row:row[0])[0]

def decode_response(raw):
    header,separator,body=raw.partition(b'\r\n\r\n')
    if not separator or len(header)>HEADER_CAP:raise Refused('http-header-refused')
    lines=header.split(b'\r\n')
    if not re.fullmatch(rb'HTTP/1\.[01] 200(?: [\x20-\x7e]*)?',lines[0]):raise Refused('http-status-not-200')
    headers={}
    for line in lines[1:]:
        key,separator,value=line.partition(b':')
        if not separator:raise Refused('http-header-refused')
        key=key.lower();value=value.strip()
        if key in headers:raise Refused('http-header-refused')
        headers[key]=value
    if b'content-encoding'in headers and headers[b'content-encoding'].lower()!=b'identity':raise Refused('http-content-encoding-refused')
    if b'transfer-encoding'in headers:
        if headers[b'transfer-encoding'].lower()!=b'chunked'or b'content-length'in headers:raise Refused('http-framing-refused')
        output=bytearray();remaining=body
        while True:
            line,separator,remaining=remaining.partition(b'\r\n')
            if not separator or not re.fullmatch(rb'[0-9A-Fa-f]{1,8}',line):raise Refused('http-framing-refused')
            size=int(line,16)
            if size==0:
                if remaining!=b'\r\n':raise Refused('http-framing-refused')
                body=bytes(output);break
            if size>BODY_CAP-len(output)or len(remaining)<size+2 or remaining[size:size+2]!=b'\r\n':raise Refused('http-body-cap-or-framing-refused')
            output.extend(remaining[:size]);remaining=remaining[size+2:]
    elif b'content-length'in headers:
        count=headers[b'content-length']
        if not re.fullmatch(rb'\d{1,9}',count)or int(count)!=len(body):raise Refused('http-framing-refused')
    if len(body)>BODY_CAP:raise Refused('http-body-cap')
    return body

def get_metrics(family,address,port,overall_deadline=None):
    """Numeric loopback only. No URL, proxy, redirect, DNS, raw log or body persistence."""
    try:parsed_address=ipaddress.ip_address(address);loopback=parsed_address.is_loopback
    except ValueError:raise Refused('endpoint-refused')from None
    if not loopback or type(port)is not int or not 1<=port<=65535 or family not in [socket.AF_INET,socket.AF_INET6]or(family==socket.AF_INET)!=(parsed_address.version==4):raise Refused('endpoint-refused')
    started=time.monotonic();deadline=min(started+WALL_SECONDS,overall_deadline)if overall_deadline is not None else started+WALL_SECONDS
    if deadline<=started:raise Refused('metrics-read-deadline')
    with socket.socket(family,socket.SOCK_STREAM)as stream,selectors.DefaultSelector()as selector:
        stream.setblocking(False);code=stream.connect_ex((address,port))
        if code not in [0,errno.EINPROGRESS,errno.EWOULDBLOCK,errno.EALREADY]:raise Refused('metrics-connect-unobservable')
        selector.register(stream,selectors.EVENT_WRITE);pending=b'GET /metrics HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\nAccept: text/plain\r\nAccept-Encoding: identity\r\n\r\n';raw=bytearray()
        while True:
            remaining=deadline-time.monotonic()
            if remaining<=0:raise Refused('metrics-read-deadline')
            if not selector.select(min(remaining,.25)):continue
            if stream.getsockopt(socket.SOL_SOCKET,socket.SO_ERROR):raise Refused('metrics-connect-unobservable')
            try:
                if pending:
                    pending=pending[stream.send(pending):]
                    if not pending:selector.modify(stream,selectors.EVENT_READ)
                else:
                    block=stream.recv(16384)
                    if not block:break
                    raw.extend(block)
                    if len(raw)>BODY_CAP+HEADER_CAP+262144:raise Refused('http-wire-cap')
                    position=raw.find(b'\r\n\r\n')
                    if position<0 and len(raw)>HEADER_CAP:raise Refused('http-header-cap')
            except BlockingIOError:continue
    return decode_response(bytes(raw))
