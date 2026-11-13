"""Exactly one diagnostic child per fresh supervisor. No application lifecycle operation."""
from pathlib import Path
import argparse,hashlib,json,os,subprocess,time,stat,threading,re
from common import process,same

SANITIZED_ALLOWLIST = {
    'identity-checks.json', 'attached.json', 'collection-receipt.json',
    'cpu-samples.jsonl', 'cpu-summary.json', 'supervisor-receipt.json',
}


def file_hash(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def remove_private_derivatives(output):
    """Exact newly owned0700 directory only; hash/delete via pinned nofollow descriptors."""
    removed=[]
    if not output.exists()and not output.is_symlink():return {'removed':removed,'allNonAllowlistedPrivateFilesAbsent':True}
    rootfd=os.open(output,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
    try:
        root=os.fstat(rootfd)
        if not stat.S_ISDIR(root.st_mode)or root.st_uid!=os.geteuid()or stat.S_IMODE(root.st_mode)!=0o700:raise RuntimeError('PrivateOutputIdentityInvalid')
        for current,directories,files,descriptor in os.fwalk('.',topdown=False,follow_symlinks=False,dir_fd=rootfd):
            for name in files:
                before=os.stat(name,dir_fd=descriptor,follow_symlinks=False)
                if current=='.'and name in SANITIZED_ALLOWLIST and stat.S_ISREG(before.st_mode):continue
                entry={'ordinal':len(removed)+1,'kind':'symlink'if stat.S_ISLNK(before.st_mode)else'file'if stat.S_ISREG(before.st_mode)else'special','bytes':before.st_size}
                if name in['execution.nettrace','execution.etlx']:entry['knownName']=name
                if stat.S_ISREG(before.st_mode):
                    fd=os.open(name,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=descriptor)
                    with os.fdopen(fd,'rb')as stream:
                        actual=os.fstat(stream.fileno());assert(actual.st_dev,actual.st_ino)==(before.st_dev,before.st_ino);entry['sha256']=hashlib.file_digest(stream,'sha256').hexdigest()
                after=os.stat(name,dir_fd=descriptor,follow_symlinks=False);assert(after.st_dev,after.st_ino)==(before.st_dev,before.st_ino);os.unlink(name,dir_fd=descriptor);entry['absenceVerified']=True;removed.append(entry)
            for name in directories:
                info=os.stat(name,dir_fd=descriptor,follow_symlinks=False)
                if stat.S_ISLNK(info.st_mode):os.unlink(name,dir_fd=descriptor)
                else:os.rmdir(name,dir_fd=descriptor)
        remaining=os.listdir(rootfd);valid=all(name in SANITIZED_ALLOWLIST and stat.S_ISREG(os.stat(name,dir_fd=rootfd,follow_symlinks=False).st_mode)for name in remaining)
        current=output.lstat();assert(current.st_dev,current.st_ino)==(root.st_dev,root.st_ino)
        return {'removed':removed,'allNonAllowlistedPrivateFilesAbsent':valid,'allSubdirectoriesRemoved':valid,'symlinkTargetsFollowedOrHashed':False,'rootDevice':root.st_dev,'rootInode':root.st_ino,'ownerMode':'0700'}
    finally:os.close(rootfd)

def write(path, value):
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, indent=2) + '\n')
    temporary.chmod(0o600)
    temporary.replace(path)


def run_owned(command, timeout, cpu_seconds, rss_bytes):
    import resource, signal
    def limits():resource.setrlimit(resource.RLIMIT_CPU,(cpu_seconds,cpu_seconds+1))
    child=None;pidfd=None;child_ticks=None;readers=[];captured=[];started=time.monotonic();peak=0;reason=None;error_type=None
    result={'childSpawned':False,'ownedChildExited':True,'onlyDirectlyOwnedDiagnosticChildSignaled':False,'unrecognizedStdoutStderrRetained':False}
    def stop_exact():
        if child is None or child.poll()is not None:return
        if pidfd is not None:signal.pidfd_send_signal(pidfd,signal.SIGTERM)
        elif child_ticks is not None and same(child.pid,child_ticks):child.terminate()
        else:return
        result['onlyDirectlyOwnedDiagnosticChildSignaled']=True
        try:child.wait(timeout=3)
        except subprocess.TimeoutExpired:
            if pidfd is not None:signal.pidfd_send_signal(pidfd,signal.SIGKILL)
            elif child_ticks is not None and same(child.pid,child_ticks):child.kill()
            child.wait(timeout=3)
    try:
        # This fresh supervisor has no threads before this one spawn. Reader threads are created afterward only.
        child=subprocess.Popen(command,stdout=subprocess.PIPE,stderr=subprocess.PIPE,preexec_fn=limits)
        result.update(childSpawned=True,ownedChildExited=False,childPid=child.pid,childStartTicks=None,childGroup=os.getpgrp())
        pidfd=os.pidfd_open(child.pid)
        initial=process(child.pid);child_ticks=initial['startTicks']if initial else None;result['childStartTicks']=child_ticks;result['pidfdPinsExactOwnedChild']=True
        def drain(stream):
            prefix=bytearray()
            while True:
                block=stream.read(4096)
                if not block:break
                if len(prefix)<8192:prefix.extend(block[:8192-len(prefix)])
            captured.append(bytes(prefix))
        readers=[threading.Thread(target=drain,args=(stream,),daemon=True)for stream in(child.stdout,child.stderr)]
        for reader in readers:reader.start()
        while child.poll()is None:
            try:
                values=dict(line.split(':',1)for line in Path('/proc',str(child.pid),'status').read_text().splitlines()if':'in line);rss=int(values.get('VmRSS','0 kB').split()[0])*1024;peak=max(peak,rss)
                if rss>rss_bytes:reason='diagnostic-rss-guard'
            except FileNotFoundError:pass
            if time.monotonic()-started>timeout:reason='diagnostic-wall-guard'
            if reason:break
            time.sleep(.1)
    except Exception as failure:error_type=type(failure).__name__;reason=reason or'diagnostic-supervisor-error'
    finally:
        if child is not None:
            if child.poll()is None:
                # Any postspawn exception still retains a kernel pidfd, or reacquires the same un-reaped direct child.
                if pidfd is None:
                    try:pidfd=os.pidfd_open(child.pid)
                    except(Exception):
                        try:
                            current=process(child.pid)
                            if current is not None:child_ticks=current['startTicks']
                        except(Exception):pass
                stop_exact()
            try:result['exitCode']=child.wait(timeout=5)
            except subprocess.TimeoutExpired:result['exitCode']=None;result['cleanupIncomplete']=True
            result['ownedChildExited']=child.poll()is not None
        for reader in readers:
            if reader.ident is not None:reader.join(timeout=2)
        if pidfd is not None:os.close(pidfd)
    failures=[];allowed={'target-already-exited','identity-contract-invalid','target-identity-changed','unsafe-or-unexpected-owned-gateway-arguments-skip-attach','input-label-invalid','required-identity-input-missing','input-hash-mismatch','owned-gateway-map-proof-unavailable-skip-attach','owned-gateway-assembly-not-mapped-skip-attach','owned-executable-path-unavailable','required-target-assembly-path-mismatch','collector-error'}
    for prefix in captured:
        for line in prefix.splitlines():
            try:value=json.loads(line)
            except(ValueError,UnicodeDecodeError):continue
            if isinstance(value,dict)and set(value)=={'diagnosticOnly','errorType','fixedReason'}and value['diagnosticOnly']is True and value['fixedReason']in allowed and re.fullmatch('[A-Za-z0-9]{1,80}',value['errorType']):failures.append(value)
    result.update(wallSeconds=time.monotonic()-started,peakObservedRssBytes=peak,guardReason=reason,supervisorErrorType=error_type,cpuHardLimitSeconds=cpu_seconds+1,rssGuardBytes=rss_bytes,fixedFailures=failures)
    return result

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--stage',choices=['collect','analyze'],required=True);parser.add_argument('--tool',type=Path,required=True);parser.add_argument('--identity',type=Path);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    os.umask(0o077)
    if args.stage=='collect' and (args.output.exists() or args.output.is_symlink()):raise SystemExit('fresh-output-required')
    receipt={'diagnosticOnly':True,'featureAcceptance':False,'stage':args.stage,'toolSha256':file_hash(args.tool),'supervisorPid':os.getpid(),'supervisorStartTicks':process(os.getpid())['startTicks'],'supervisorGroup':os.getpgrp(),'noTargetSignalsOrGcRequests':True,'singleChildInFreshSupervisorNoThreadedFork':True,'incomplete':True}
    path=args.output.with_suffix('.'+args.stage+'-supervisor.json')
    try:
        if args.stage=='collect':
            assert args.identity is not None
            receipt['identitySha256']=file_hash(args.identity)
            receipt['child']=run_owned(['dotnet',str(args.tool),'collect',str(args.identity),str(args.output)],55,40,768*1024*1024)
        else:
            assert args.output.is_dir()and not args.output.is_symlink()
            receipt['child']=run_owned(['dotnet',str(args.tool),'analyze',str(args.output)],60,45,1024*1024*1024)
        receipt['incomplete']=receipt['child']['exitCode']!=0 or receipt['child']['guardReason']is not None
    except Exception as failure:receipt['errorType']=type(failure).__name__
    finally:
        # Coordinator alone performs final derivative removal after the owned collect/parse processes are absent.
        write(path,receipt)
        if args.output.exists()and not args.output.is_symlink():
            previous={}
            target=args.output/'supervisor-receipt.json'
            if target.exists():previous=json.loads(target.read_bytes())
            previous[args.stage]=receipt;write(target,previous)
    print(json.dumps({'diagnosticOnly':True,'stage':args.stage,'incomplete':receipt['incomplete']}))
    return 2 if receipt['incomplete']else 0

if __name__=='__main__':raise SystemExit(main())
