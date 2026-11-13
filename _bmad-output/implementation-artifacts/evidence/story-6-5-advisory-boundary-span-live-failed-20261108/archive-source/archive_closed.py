"""ROOT REVIEW REQUIRED. Closed primitive-only copy and optional exact-owned-file removal."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,re,stat,subprocess,sys
sys.dont_write_bytecode=True
PREP=Path('/tmp/story65-advisory-boundary-span-live-prep-20261108')
EVIDENCE=Path('/tmp/story65-advisory-boundary-span-diagnostic-20261108')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
OUT=EVIDENCE/'counter'
sys.path.insert(0,str(PREP))
from ops_common import ROOT,sha,sources,ordinary_sdk_proof,independent_dependencies,app_marker_proof,group_members,operational_pins
from validate_closed_terminal import validate

def absent(pid,ticks):
 try:return Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()[19]!=str(ticks)
 except(FileNotFoundError,ProcessLookupError):return True

def run_metadata(command):
 # Only finite exact-owned Docker IDs/namespace selectors; never persist command/stdout/stderr.
 result=subprocess.run(command,capture_output=True,timeout=10)
 assert len(result.stdout)<=1024*1024 and len(result.stderr)<=16384
 return result

def guards(ordinary_path,ordinary_sha):
 status=json.loads((PREP/'latest-operational-result.json').read_text())
 assert status['ordinaryRestoreAttempted']is True and status['ordinaryExitCode']==0
 assert ordinary_path.parent.parent==PREP and ordinary_path.parent.name.startswith('operational-ordinary-')and ordinary_path.name=='receipt.json'
 assert sha(ordinary_path)==ordinary_sha
 ordinary=json.loads(ordinary_path.read_text())
 assert ordinary['phase']=='ordinary'and ordinary['completed']is True and ordinary['source65Match']is True and ordinary['allPrivateMarkersAbsent']is True
 supplemental=json.loads((EVIDENCE/'supplemental-cleanup-qualification.json').read_text())
 assert all(supplemental[k]is True for k in ['safeForOrdinaryRestoration','allRecordedPidStartIdentitiesAbsentIncludingZombies','allNativeSeedGroupsFreshlyAbsentIncludingZombies'])
 audit=json.loads((EVIDENCE/'independent-cleanup-source-audit.json').read_text())
 assert all(audit[k]is True for k in ['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent','ownedSidecarsAbsent'])
 assert audit['fullLiveManifestHashMismatches']==0
 identities=audit['identities'][:]
 early=json.loads((EVIDENCE/'early-runtime-identities.json').read_text())['identities'];assert len(early)==6
 for row in early:
  identities.extend([{'pid':row['pid'],'startTicks':row['startTicks']},{'pid':row['wrapperPid'],'startTicks':row['wrapperStartTicks']}])
 assert all(absent(row['pid'],row['startTicks'])for row in identities)
 native_path=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory']);assert native_path.parent==PREP
 native=json.loads((native_path/'receipt.json').read_text());identity=native['ownedNativeIdentity']
 assert native['nativeProcessAndDescendantsAbsent']is True and not group_members(identity['processGroup'])
 groups=[identity['processGroup']]
 for row in audit['nativeTestProcessCleanup']:
  assert row['descendantsAbsent']is True and type(row['groupId'])is int and not group_members(row['groupId']);groups.append(row['groupId'])
 for row in audit['containers']:
  identifier=row['containerId'];assert re.fullmatch(r'[0-9a-f]{12,64}',identifier)
  result=run_metadata(['docker','inspect','--format','{{.Id}}',identifier])
  assert result.returncode!=0 and b'no such object'in result.stderr.lower()
 run=json.loads((EVIDENCE/'managed/run.json').read_text())
 oidc=next(row['resource']for row in run['cleanup']if row['resource'].endswith('-oidc'))
 namespace=oidc[:-5];assert re.fullmatch(r'reads-[0-9a-f]{24}',namespace)
 result=run_metadata(['docker','ps','--all','--filter','label=hexalith.supported-reads='+namespace,'--format','{{.ID}}']);assert result.returncode==0 and not result.stdout.strip()
 manifest=json.loads((EVIDENCE/'managed/source-manifest.json').read_text())
 for name,digest in manifest.items():
  p=(ROOT/name).resolve();assert p.is_relative_to(ROOT)and sha(p)==digest
 sources();sdk=ordinary_sdk_proof();dependencies=independent_dependencies();apps=app_marker_proof(False)
 return {'allRecordedAndEarlyIdentitiesAbsentIncludingZombies':True,'recordedIdentityRows':len(identities),'nativeGroupsAbsentIncludingZombies':len(groups),'recordedContainersFreshlyAbsent':len(audit['containers']),'ownedNamespaceFreshlyAbsent':True,'fullLiveSourceManifestCount':len(manifest),'fullLiveSourceMismatches':0,'source65Match':True,'ordinaryReceiptSha256':ordinary_sha,'sdk':sdk,'dependencies':dependencies,'appMarkerProof':apps}

def read_file(fd,name,limit):
 f=os.open(name,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=fd)
 try:
  st=os.fstat(f);assert stat.S_ISREG(st.st_mode)and st.st_uid==1000 and stat.S_IMODE(st.st_mode)==0o600 and st.st_size<=limit
  chunks=[];n=0
  while True:
   data=os.read(f,min(65536,limit+1-n))
   if not data:break
   chunks.append(data);n+=len(data);assert n<=limit
  again=os.fstat(f);identity=(st.st_dev,st.st_ino,st.st_size,st.st_mtime_ns)
  assert identity==(again.st_dev,again.st_ino,again.st_size,again.st_mtime_ns)
  return b''.join(chunks),identity
 finally:os.close(f)

def copy_new(directory,name,data):
 f=os.open(directory/name,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600)
 try:
  with os.fdopen(f,'wb',closefd=False)as stream:stream.write(data);stream.flush();os.fsync(f)
 finally:os.close(f)
 assert sha(directory/name)==hashlib.sha256(data).hexdigest()

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--reviewed-script-sha256');parser.add_argument('--operational-pins-sha256');parser.add_argument('--ordinary-receipt',type=Path,required=True);parser.add_argument('--ordinary-receipt-sha256',required=True);parser.add_argument('--remove-reviewed-owned',action='store_true');args=parser.parse_args()
 assert args.execute and sha(Path(__file__))==args.reviewed_script_sha256
 operational_pins(args.operational_pins_sha256)
 proof=guards(args.ordinary_receipt,args.ordinary_receipt_sha256)
 supervisor=json.loads((EVIDENCE/'supervisor.json').read_text());assert supervisor['controlPublished']is True
 fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  st=os.fstat(fd);assert(st.st_dev,st.st_ino,st.st_uid,stat.S_IMODE(st.st_mode))==(2096,8752986,1000,0o700)
  assert set(os.listdir(fd))=={'window.control','projects.jsonl','projects.closed.json'}
  validated=validate(supervisor) # Exact23closedkeys/14rowfields/enums/pairs/control bytes and inode.
  assert validated['complete']is True
  contents={};identities={}
  for name in sorted(os.listdir(fd)):
   data,identity=read_file(fd,name,8*1024*1024 if name.endswith('.jsonl')else 4096)
   expected=validated['artifacts'][name];assert identity[:3]==(expected['device'],expected['inode'],expected['bytes'])and hashlib.sha256(data).hexdigest()==expected['sha256']
   contents[name]=data;identities[name]=identity
  # Recheck fresh process/group/source/module guards before any copy or deletion.
  after=guards(args.ordinary_receipt,args.ordinary_receipt_sha256);assert after==proof
  OUT.mkdir(mode=0o700,exist_ok=False)
  for name in ['projects.jsonl','projects.closed.json']:copy_new(OUT,name,contents[name])
  report={'diagnosticOnly':True,'featureAcceptance':False,'guards':proof,'validated':validated,'copiedOnly':['projects.jsonl','projects.closed.json'],'controlContentsCopied':False,'controlSha256':hashlib.sha256(contents['window.control']).hexdigest(),'controlBytes':len(contents['window.control']),'writerProofIsOneObservedPointNotAllRowExclusivity':True,'noSerializedResultTimerCauseDependencyCauseOrChildQuiescenceInference':True,'exactOwnedFilesRemoved':False,'fixedDirectoryEmpty':False}
  copy_new(OUT,'archive-before-removal.json',(json.dumps(report,indent=2)+'\n').encode())
  if args.remove_reviewed_owned:
   assert set(os.listdir(fd))==set(contents)
   # Hold original directory FD; no wildcard paths, replacement files or unknown inodes.
   for name in sorted(contents):
    data,identity=read_file(fd,name,8*1024*1024);assert identity==identities[name]and data==contents[name]
   for name in sorted(contents):
    s=os.stat(name,dir_fd=fd,follow_symlinks=False);assert(s.st_dev,s.st_ino,s.st_size,s.st_mtime_ns)==identities[name];os.unlink(name,dir_fd=fd)
   report['exactOwnedFilesRemoved']=True;report['fixedDirectoryEmpty']=os.listdir(fd)==[];assert report['fixedDirectoryEmpty']
  copy_new(OUT,'archive-cleanup.json',(json.dumps(report,indent=2)+'\n').encode())
  print(json.dumps({'rows':validated['rows'],'captures':validated['captures'],'copiedFiles':2,'removed':report['exactOwnedFilesRemoved'],'directoryEmpty':report['fixedDirectoryEmpty']}))
 finally:os.close(fd)
if __name__=='__main__':main()
