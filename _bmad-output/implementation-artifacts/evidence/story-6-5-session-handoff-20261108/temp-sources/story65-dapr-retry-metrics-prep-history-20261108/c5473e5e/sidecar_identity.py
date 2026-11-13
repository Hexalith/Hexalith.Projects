"""No entry point. Exact ordinary-session observer supplies owned graph/configs.
Private coordinates, arguments, held descriptors and endpoint identity are RAM-only.
"""
from pathlib import Path
import hashlib,os,re,stat,time
from metrics_core import Refused,ROLES,process,listener,stable_process
DPR=Path('/home/administrator/.dapr/bin/daprd')

def stat_key(value):return(value.st_dev,value.st_ino,value.st_size,value.st_mtime_ns,value.st_ctime_ns)

def hash_fd(fd):
    os.lseek(fd,0,os.SEEK_SET);value=hashlib.sha256()
    while True:
        block=os.read(fd,1024*1024)
        if not block:return value.hexdigest()
        value.update(block)

def open_binary(expected_sha256):
    """Exactly once BEFORE eight-second HTTP window, never per sidecar/scrape."""
    if not re.fullmatch(r'[0-9a-f]{64}',expected_sha256):raise Refused('binary-pin-refused')
    fd=os.open(DPR,os.O_RDONLY|os.O_NOFOLLOW)
    try:
        before=os.fstat(fd)
        if not stat.S_ISREG(before.st_mode)or hash_fd(fd)!=expected_sha256 or stat_key(os.fstat(fd))!=stat_key(before)or stat_key(DPR.lstat())!=stat_key(before):raise Refused('sidecar-binary-hash-changed')
        return{'fd':fd,'statKey':stat_key(before),'sha256':expected_sha256}
    except BaseException:os.close(fd);raise

def finish_binary(proof):
    """Exactly once AFTER all HTTP. Any failure invalidates the whole window."""
    fd=proof['fd']
    try:
        if stat_key(os.fstat(fd))!=proof['statKey']or stat_key(DPR.lstat())!=proof['statKey']or hash_fd(fd)!=proof['sha256']or stat_key(os.fstat(fd))!=proof['statKey']or stat_key(DPR.lstat())!=proof['statKey']:raise Refused('sidecar-binary-hash-changed')
        return True
    finally:os.close(fd)

def file_proof(path,expected_sha256):
    """Original source-owned config hash before window; only metadata in window."""
    fd=os.open(path,os.O_RDONLY|os.O_NOFOLLOW)
    try:
        before=os.fstat(fd)
        if not stat.S_ISREG(before.st_mode)or hash_fd(fd)!=expected_sha256 or stat_key(os.fstat(fd))!=stat_key(before)or stat_key(Path(path).lstat())!=stat_key(before):raise Refused('sidecar-config-changed')
        return stat_key(before)
    finally:os.close(fd)

def same(pid,ticks):
    row=process(pid)
    return row is not None and row['state']!='Z'and row['start']==str(ticks)

def flag(arguments,name):
    values=[]
    for index,item in enumerate(arguments):
        if item==name:
            if index+1>=len(arguments)or arguments[index+1].startswith('--'):raise Refused('sidecar-flag-refused')
            values.append(arguments[index+1])
        elif item.startswith(name+'='):values.append(item[len(name)+1:])
    if len(values)!=1:raise Refused('sidecar-flag-refused')
    return values[0]

def metrics_enabled(arguments):
    values=[]
    for index,item in enumerate(arguments):
        if item=='--enable-metrics':
            following=arguments[index+1]if index+1<len(arguments)else None
            values.append(following if following in ['true','false']else'true')
        elif item.startswith('--enable-metrics='):values.append(item.split('=',1)[1])
    if not values:return None
    if len(values)!=1 or values[0]not in ['true','false']:raise Refused('metrics-enable-flag-refused')
    return values[0]=='true'

def proc_snapshot():
    """One bounded preflight PPID snapshot, including children of ANY Go thread."""
    result={};deadline=time.monotonic()+2
    for path in Path('/proc').iterdir():
        if not path.name.isdigit():continue
        if len(result)>=8192 or time.monotonic()>deadline:raise Refused('owned-ancestry-refused')
        row=process(int(path.name))
        if row is not None:result[int(path.name)]=row
    return result

def chain(pid,ancestor,rows=None):
    """Fresh or supplied PPID chain; no main-thread task/children assumption."""
    seen=set()
    for _ in range(128):
        if pid==ancestor:return True
        if pid<=1 or pid in seen:return False
        seen.add(pid);row=rows.get(pid)if rows is not None else process(pid)
        if row is None or row['state']=='Z':return False
        pid=row['parent']
    return False

def point_binary(proof,pid):
    """Observed inode/metadata point only, NOT continuous byte-integrity proof."""
    if stat_key(os.fstat(proof['fd']))!=proof['statKey']or stat_key(DPR.lstat())!=proof['statKey']:raise Refused('sidecar-binary-hash-changed')
    exe=Path('/proc',str(pid),'exe')
    if exe.readlink()!=DPR:raise Refused('sidecar-executable-changed')
    target=exe.stat()
    if(target.st_dev,target.st_ino)!=proof['statKey'][:2]:raise Refused('sidecar-executable-changed')

def classify(owned,graph,role,configuration,binary,session_guard,rows):
    """BEFORE window. rows is one fresh shared proc_snapshot after guarded graph.
    session_guard proves launcher/AppHost/CLI starts, exact evidence binding and
    pinned CLI. Original source/config/core validation is outside HTTP clock.
    configuration is exact path/hash from the original owned receiver proof.
    """
    session_guard()
    if role not in ROLES:raise Refused('role-or-binary-pin-refused')
    app=owned['appHostPid'];app_start=owned['appHostStartTicks']
    if not same(app,app_start):raise Refused('owned-apphost-start-changed')
    matches=[r for r in graph.get('resources',[])if r.get('displayName')==role+'-dapr-cli']
    if len(matches)!=1:raise Refused('owned-sidecar-resource-unobservable')
    wrapper=matches[0].get('properties',{}).get('executable.pid');wrapper_row=rows.get(wrapper)if type(wrapper)is int else None
    if wrapper_row is None or wrapper_row['state']=='Z'or not chain(wrapper,app,rows)or not same(wrapper,wrapper_row['start']):raise Refused('owned-sidecar-wrapper-unobservable')
    found=[]
    for pid,current in rows.items():
        if current['state']=='Z'or not chain(pid,wrapper,rows):continue
        try:exe=Path('/proc',str(pid),'exe').readlink()
        except(FileNotFoundError,ProcessLookupError):continue
        if exe!=DPR:continue
        point_binary(binary,pid)
        raw=Path('/proc',str(pid),'cmdline').read_bytes()
        if len(raw)>65536:raise Refused('sidecar-arguments-cap')
        try:arguments=[item.decode('utf-8')for item in raw.split(b'\0')if item]
        except UnicodeError:raise Refused('sidecar-arguments-refused')from None
        if flag(arguments,'--app-id')!=role or flag(arguments,'--config')!=configuration['path']:raise Refused('sidecar-app-or-config-refused')
        port=flag(arguments,'--metrics-port')
        if not re.fullmatch(r'[0-9]{1,5}',port)or not 1<=int(port)<=65535:raise Refused('metrics-port-not-explicit')
        if metrics_enabled(arguments)is False:raise Refused('metrics-disabled-or-flag-refused')
        environment=Path('/proc',str(pid),'environ').read_bytes().split(b'\0')
        if environment.count(('NAMESPACE='+owned['_isolation']).encode())!=1:raise Refused('sidecar-namespace-refused')
        config_key=file_proof(configuration['path'],configuration['sha256'])
        if not same(pid,current['start']):raise Refused('sidecar-start-changed')
        family,address,inode=listener(pid,current['start'],int(port))
        found.append({'role':role,'pid':pid,'processIdentity':stable_process(current),'start':current['start'],'wrapperPid':wrapper,'wrapperIdentity':stable_process(wrapper_row),'wrapperStart':wrapper_row['start'],'appHostPid':app,'appHostStart':app_start,'port':int(port),'family':family,'address':address,'socketInode':inode,'configuration':configuration,'configStatKey':config_key})
    if len(found)!=1:raise Refused('owned-sidecar-unobservable-or-ambiguous')
    recheck(found[0],owned,binary,session_guard);return found[0]

def recheck(identity,owned,binary,session_guard):
    """Cheap finite observed point per scrape; no whole binary/source rehash."""
    session_guard();pid=identity['pid'];wrapper=identity['wrapperPid'];app=identity['appHostPid']
    row=process(pid);wrapped=process(wrapper)
    if row is None or wrapped is None or row['state']=='Z'or wrapped['state']=='Z'or stable_process(row)!=identity['processIdentity']or stable_process(wrapped)!=identity['wrapperIdentity']or not same(app,identity['appHostStart'])or not chain(pid,wrapper)or not chain(wrapper,app):raise Refused('sidecar-or-listener-changed')
    point_binary(binary,pid)
    if stat_key(Path(identity['configuration']['path']).lstat())!=identity['configStatKey']:raise Refused('sidecar-config-changed')
    family,address,inode=listener(pid,identity['start'],identity['port'])
    if(family,address,inode)!=(identity['family'],identity['address'],identity['socketInode']):raise Refused('sidecar-or-listener-changed')
    after=process(pid);wrapped_after=process(wrapper)
    if after is None or wrapped_after is None or after['state']=='Z'or wrapped_after['state']=='Z'or stable_process(after)!=identity['processIdentity']or stable_process(wrapped_after)!=identity['wrapperIdentity']:raise Refused('sidecar-start-changed')
    session_guard();return True
