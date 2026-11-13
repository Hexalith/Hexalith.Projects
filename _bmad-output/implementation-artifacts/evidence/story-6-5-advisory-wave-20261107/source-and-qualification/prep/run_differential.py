#!/usr/bin/env python3
"""PREPARED ONLY. Exact synthetic differential controller; never starts an application topology."""
from pathlib import Path
import argparse, hashlib, json, os, selectors, stat, subprocess, time

PREP=Path('/tmp/story65-advisory-wave-prep-20261107')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
DLL=PREP/'differential/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll'
DOTNET='/home/administrator/.dotnet/dotnet'
NAMES={'window.control','projects.jsonl','projects.closed.json'}
ZERO={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
FIELDS=ZERO|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
STAGES={'Whole','ComponentFile','ComponentPolicy','AclFile','SettingsEnumerationSortDictionary','AuthenticationSerialization','ActorBoundary','ResultTail'}
SLOW=['shared-deadline','index-uncooperative','filter-uncooperative','final-anchor-uncooperative','caller-uncooperative-child']
CANARIES=['canary-positive-control','canary-current','canary-suppressed']


def sha(data): return hashlib.sha256(data).hexdigest()

def check_dir(fd):
    s=os.fstat(fd)
    assert (s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)
    assert stat.S_ISDIR(s.st_mode) and os.geteuid()==1000

def write_new(fd,name,data):
    out=os.open(name,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600,dir_fd=fd)
    try:
        s=os.fstat(out); assert stat.S_ISREG(s.st_mode) and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600
        with os.fdopen(out,'wb',closefd=False) as f: f.write(data); f.flush()
    finally: os.close(out)

def read_owned(fd,name,maximum):
    f=os.open(name,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=fd)
    try:
        s=os.fstat(f); assert stat.S_ISREG(s.st_mode) and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600 and s.st_size<=maximum
        chunks=[]; total=0
        while True:
            part=os.read(f,min(65536,maximum+1-total))
            if not part: break
            chunks.append(part); total+=len(part); assert total<=maximum
        return b''.join(chunks)
    finally: os.close(f)

def ready_line(proc,deadline):
    selector=selectors.DefaultSelector(); selector.register(proc.stdout,selectors.EVENT_READ)
    try:
        while time.monotonic()<deadline:
            if not selector.select(0.1):
                if proc.poll() is not None: raise RuntimeError('ChildExitedBeforeProtocol')
                continue
            line=proc.stdout.readline()
            if line: return line.decode('ascii').strip()
        raise TimeoutError('ChildProtocolTimeout')
    finally: selector.close()

def wait_closed(fd):
    end=time.monotonic()+8
    while time.monotonic()<end:
        try:
            raw=read_owned(fd,'projects.closed.json',4096)
            return json.loads(raw),raw
        except (FileNotFoundError,json.JSONDecodeError): time.sleep(.01)
    raise TimeoutError('NoCompleteClosedReceipt')

def one(mode,group,case,ordinal,result_root=None):
    result_dir=(result_root or PREP/'differential-results')/mode/(case or group); result_dir.mkdir(parents=True,exist_ok=False)
    fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
    proc=None; command=[]; artifacts={}; error='none'; closed=None; row_count=0; stage_counts={}
    try:
        check_dir(fd); assert os.listdir(fd)==[]
        if mode=='failing-sink': write_new(fd,'projects.jsonl',b'')
        command=[DOTNET,str(DLL),mode,group,str(result_dir/'business-parity.json')]
        if case: command.append(case)
        env=os.environ.copy(); env.pop('DOTNET_PROCESSOR_COUNT',None)
        for key in list(env):
            if key.startswith('Hexalith') and key.endswith('Root') or key=='CustomBeforeMicrosoftCommonTargets': env.pop(key,None)
        proc=subprocess.Popen(command,cwd=PREP/'differential',env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL)
        line=ready_line(proc,time.monotonic()+20)
        fields=line.split(':'); assert len(fields)==3 and fields[0]=='READY'
        frequency,start=map(int,fields[1:]); assert frequency>0 and start>0
        recording=mode not in ('disabled','canary-positive-control')
        if recording: write_new(fd,'window.control',f'{ordinal}\n{frequency}\n{start}\n{start+8*frequency}\n'.encode('ascii'))
        terminal=ready_line(proc,time.monotonic()+60)
        if recording:
            assert terminal=='CASES_DONE'
            closed,raw=wait_closed(fd)
            assert type(closed) is dict and type(closed.get('role')) is str
            assert all(type(value) is int for key,value in closed.items() if key!='role')
            assert closed['runOrdinal']==ordinal and closed['role']=='projects'
            assert closed['admitted']==closed['disposedOrUnwound'] and closed['activeAtDrainEnd']==0
            assert closed['countersFinal']==1 and closed['incompleteHardDrain']==0 and closed['queuedAtDrainEnd']==0
            assert closed['writtenRows']<=65536 and closed['writtenBytes']<=8*1024*1024
            if mode=='capped': assert closed['capRowsDropped']>0 and closed['ioFailures']==0
            elif mode=='failing-sink': assert closed['ioFailures']>0 and closed['writtenRows']==0
            else: assert closed['capRowsDropped']==0 and closed['ioFailures']==0 and closed['queuedRowsDropped']==0 and closed['closedRowsDropped']==0
            proc.stdin.write(b'EXIT\n'); proc.stdin.flush()
            terminal=ready_line(proc,time.monotonic()+10)
        status=json.loads(terminal); assert set(status)=={'cases','passed','errorType'}
        assert status['cases']==status['passed'] and status['errorType']=='none'
        code=proc.wait(timeout=10); assert code==0
        check_dir(fd)
        for name in sorted(os.listdir(fd)):
            assert name in NAMES
            data=read_owned(fd,name,8*1024*1024 if name.endswith('.jsonl') else 4096)
            (result_dir/name).write_bytes(data); artifacts[name]={'sha256':sha(data),'bytes':len(data)}
            if name=='projects.jsonl':
                for line in data.splitlines():
                    row=json.loads(line); assert set(row)==FIELDS and all(row[key]==0 for key in ZERO)
                    assert type(row['role']) is str and type(row['stage']) is str
                    assert all(type(row[key]) is int for key in FIELDS-{'role','stage'})
                    assert row['role']=='projects' and row['runOrdinal']==ordinal and row['captureOrdinal']>0 and row['stage'] in STAGES
                    assert row['frequency']==frequency and row['elapsedTicks']>=0
                    row_count+=1; stage_counts[row['stage']]=stage_counts.get(row['stage'],0)+1
        if closed:
            assert row_count==closed['writtenRows'] and artifacts.get('projects.jsonl',{'bytes':0})['bytes']==closed['writtenBytes']
        if (mode=='active' and group=='fast' and case is None) or mode in ('canary-current','canary-suppressed'): assert set(stage_counts)==STAGES
        # Only controller/helper-created fixed files, after process exit and completed drain, are removed.
        for name in sorted(os.listdir(fd)):
            assert name in NAMES; read_owned(fd,name,8*1024*1024); os.unlink(name,dir_fd=fd)
        check_dir(fd); assert os.listdir(fd)==[]
    except Exception as ex:
        error=type(ex).__name__
        # Preserve any incomplete synthetic files. Do not clean a running producer or claim directory release.
        if proc is not None and proc.poll() is None: proc.terminate(); proc.wait(timeout=10)
        raise
    finally:
        receipt={'preparationOnly':False,'mode':mode,'group':group,'case':case,'command':command,'cwd':str(PREP/'differential'),'runtimeProcessorOverride':None,'errorType':error,'artifacts':artifacts,'rows':row_count,'stageCounts':stage_counts,'closed':closed,'rawDependencyTracePersisted':False}
        (result_dir/'controller-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
        os.close(fd)


def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--execute',action='store_true'); ap.add_argument('--mode',choices=['disabled','active','capped','failing-sink','all']+CANARIES,default='all'); ap.add_argument('--attempt',choices=['initial','qualified'],default='initial'); args=ap.parse_args()
    plan=[(mode,'canary',None) for mode in CANARIES] if args.mode=='all' else []
    if args.mode in CANARIES: plan=[(args.mode,'canary',None)]
    modes=['disabled','active','capped','failing-sink'] if args.mode=='all' else [args.mode]
    for mode in modes:
        if mode in CANARIES: continue
        if mode=='disabled': plan.append((mode,'all',None))
        else:
            plan.append((mode,'fast',None))
            plan.extend((mode,'slow',name) for name in SLOW)
    if not args.execute:
        print(json.dumps({'preparationOnly':True,'executed':False,'plannedProcesses':[{'mode':m,'group':g,'case':c} for m,g,c in plan]},indent=2)); return
    pins=json.loads((PREP/'differential/compiled-input-pins.json').read_text())['inputsSha256']
    for name,wanted in pins.items(): assert sha(Path(name).read_bytes())==wanted
    owner=Path('/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore')
    for name,wanted in json.loads(Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json').read_text())['sourceSha256'].items():
        assert sha((owner/name).read_bytes())==wanted
    source=json.loads((PREP/'source-proof.json').read_text())
    root=Path('/home/administrator/projects/hexalith/projects')
    assert sha((root/'src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs').read_bytes())==source['originalExecutorSha256']
    assert sha((owner/'src/Hexalith.EventStore.ServiceDefaults/Routing/DevelopmentRouteCatalogInputs.cs').read_bytes())==source['ordinaryCaptureSourceSha256']
    assert sha((PREP/'CaptureCounters.cs').read_bytes())==source['helperSha256']
    assert sha((PREP/'AdvisoryWaveBootstrap.cs').read_bytes())==source['bootstrapSha256']
    assert DLL.exists()
    result_root=PREP/('differential-results' if args.attempt=='initial' else 'differential-results-qualified')
    for n,(mode,group,case) in enumerate(plan,9101): one(mode,group,case,n,result_root)

if __name__=='__main__': main()
