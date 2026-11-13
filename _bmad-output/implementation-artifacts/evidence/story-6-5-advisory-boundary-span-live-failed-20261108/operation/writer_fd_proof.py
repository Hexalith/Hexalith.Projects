"""Prepared metadata-only proof. No file contents/argv/env/config/unknown link values persist."""
from pathlib import Path
import os,stat

PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')

def same(pid,start):
 try:
  fields=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()
  return fields[0]!='Z' and fields[19]==str(start)
 except(FileNotFoundError,ProcessLookupError):return False

def owned_projects_writer(identity,private_directory_fd):
 """Call ONLY after the reviewed fresh owned-source/wrapper/private-module guards.

 Match only this exact Projects entry's open writable FD to the fixed file inode.
 Missing the finite observation window means attribution unproven; it is never a
 reason to delay original managed cleanup, infer zero admissions, or retry a run.
 """
 pid=identity['pid'];start=identity['startTicks']
 assert identity['role']=='projects' and same(pid,start)
 directory=os.fstat(private_directory_fd)
 assert (directory.st_dev,directory.st_ino,directory.st_uid,stat.S_IMODE(directory.st_mode))==(2096,8752986,1000,0o700)
 try:filefd=os.open('projects.jsonl',os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=private_directory_fd)
 except FileNotFoundError:return {'observed':False,'reason':'fixed-output-not-yet-created'}
 try:
  target=os.fstat(filefd)
  assert stat.S_ISREG(target.st_mode) and target.st_uid==1000 and stat.S_IMODE(target.st_mode)==0o600 and target.st_size<=8*1024*1024
  descriptors=list(Path('/proc',str(pid),'fd').iterdir());assert len(descriptors)<=4096
  for descriptor in descriptors:
   if not descriptor.name.isdigit():continue
   try:
    # Unknown link targets are compared in RAM then discarded, never serialized.
    if os.readlink(descriptor)!=str(PRIVATE/'projects.jsonl'):continue
    info=Path('/proc',str(pid),'fdinfo',descriptor.name).read_bytes();assert len(info)<=4096
    flags=[line.split(b':',1)[1].strip() for line in info.splitlines() if line.startswith(b'flags:')]
    assert len(flags)==1;access=int(flags[0],8)&os.O_ACCMODE
    if access not in (os.O_WRONLY,os.O_RDWR):continue
    seen=descriptor.stat();current=os.fstat(filefd)
    if (seen.st_dev,seen.st_ino)!=(target.st_dev,target.st_ino):continue
    assert (current.st_dev,current.st_ino,current.st_uid,stat.S_IMODE(current.st_mode))==(target.st_dev,target.st_ino,1000,0o600)
    assert same(pid,start)
    return {'observed':True,'role':'projects','pid':pid,'startTicks':str(start),'descriptor':int(descriptor.name),
     'device':target.st_dev,'inode':target.st_ino,'mode':'0600','writable':True,'exactFixedOutputInodeMatched':True,
     'noRawLinkTargetOrFdinfoPersisted':True,'knownOwnedSourceAndPrivateModuleGuardIsExternalPrerequisite':True}
   except(FileNotFoundError,ProcessLookupError):continue
  assert same(pid,start)
  return {'observed':False,'reason':'no-owned-writable-descriptor-observed'}
 finally:os.close(filefd)
