"""Ordinary acceptance only; invoke only after explicit parent release.
No profiler/overlay/cutoff/topology signals. Existing managed lane owns cleanup.
"""
from pathlib import Path
import argparse,datetime,hashlib,json,os,subprocess
ROOT=Path('/home/administrator/projects/hexalith/projects');OWNER=ROOT/'references/Hexalith.EventStore'
def sha(path):
 with path.open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(path,value):
 temporary=Path(str(path)+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');temporary.chmod(0o600);temporary.replace(path)
def start(pid):
 try:
  fields=Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split();return None if fields[0]=='Z'else fields[19]
 except FileNotFoundError:return None
def source_proof(receipt_path,expected):
 if sha(receipt_path)!=expected:raise RuntimeError('new-owner-receipt-hash-mismatch')
 receipt=json.loads(receipt_path.read_text());sources=receipt.get('sourceSha256')
 if not isinstance(sources,dict)or not sources:raise RuntimeError('new-owner-source-manifest-missing')
 rows=[]
 for relative,digest in sorted(sources.items()):
  path=OWNER/relative
  if Path(relative).is_absolute()or '..'in Path(relative).parts or not path.resolve().is_relative_to(OWNER):raise RuntimeError('owner-source-coordinate-invalid')
  actual=sha(path)
  if actual!=digest:raise RuntimeError('owner-source-freeze-mismatch')
  rows.append({'source':'references/Hexalith.EventStore/'+relative,'sha256':actual})
 return {'receiptSha256':expected,'sourceCount':len(rows),'allMatched':True,'sources':rows,'ownerHead':receipt.get('ownerHead'),'uniquePassingCases':receipt.get('uniquePassingCases')}
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--owner-receipt',type=Path,required=True);parser.add_argument('--owner-receipt-sha256',required=True);parser.add_argument('--native-audit',type=Path,required=True);parser.add_argument('--native-audit-sha256',required=True);parser.add_argument('--evidence',type=Path,default=Path('/tmp/story65-receiver-image-acceptance-live-20261107'));args=parser.parse_args();os.umask(0o077)
 if args.evidence.exists():raise RuntimeError('fresh-evidence-path-required')
 source=source_proof(args.owner_receipt,args.owner_receipt_sha256)
 if sha(args.native_audit)!=args.native_audit_sha256:raise RuntimeError('parent-native-audit-hash-mismatch')
 save(Path(str(args.evidence)+'-preflight-source.json'),source)
 env=os.environ.copy();removed=[]
 for key in ['DOTNET_PROCESSOR_COUNT','CustomBeforeMicrosoftCommonTargets','CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration']:
  if key in env:removed.append(key)
  env.pop(key,None)
 assignments={'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','E2E_SUPPORTED_READS_ONLY':'1','E2E_RESOURCE_TIMEOUT_SECONDS':'900','E2E_SUPPORTED_EVIDENCE':str(args.evidence)};env.update(assignments)
 command=['npm','--prefix','tests/e2e','run','test:live:managed'];log=Path(str(args.evidence)+'-console.log');started=datetime.datetime.now(datetime.timezone.utc).isoformat()
 with log.open('wb')as stream:child=subprocess.Popen(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 receipt={'command':command,'cwd':str(ROOT),'environmentAssignments':assignments,'environmentRemovals':['DOTNET_PROCESSOR_COUNT'],'inheritedKeysActuallyRemoved':removed,'inheritedSourceMappingCleared':True,'startedAtUtc':started,'stdoutStderr':str(log),'launcherPid':child.pid,'launcherStartTicks':start(child.pid),'supervisorPid':os.getpid(),'supervisorStartTicks':start(os.getpid()),'ordinaryAcceptanceOnly':True,'profilerEnabled':False,'privateDiagnosticOverride':False,'cutoffEnabled':False,'sourceReceiptSha256':args.owner_receipt_sha256,'sourceCount':source['sourceCount'],'nativeAuditSha256':args.native_audit_sha256}
 save(Path(str(args.evidence)+'-command.json'),receipt);print(json.dumps({'phase':'running','evidence':str(args.evidence),'launcherPid':child.pid,'launcherStartTicks':receipt['launcherStartTicks'],'sourceCount':source['sourceCount'],'profilerEnabled':False}),flush=True)
 receipt['exitCode']=child.wait();receipt['finishedAtUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat();receipt['logSha256']=sha(log);save(Path(str(args.evidence)+'-command.json'),receipt)
 post=source_proof(args.owner_receipt,args.owner_receipt_sha256);save(Path(str(args.evidence)+'-postflight-source.json'),post);print(json.dumps({'phase':'managed-complete','exitCode':receipt['exitCode'],'sourceCount':post['sourceCount'],'allSourcesMatched':post['allMatched']}),flush=True)
if __name__=='__main__':main()
