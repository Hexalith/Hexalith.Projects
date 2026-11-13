"""Read-only independent verification after managed completion. No cleanup mutation."""
from pathlib import Path
import argparse,hashlib,json,subprocess,statistics
ROOT=Path('/home/administrator/projects/hexalith/projects')
def sha(path):
 with path.open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def absent(pid,recorded):
 try:
  fields=Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split();return fields[0]=='Z'or fields[19]!=str(recorded)
 except FileNotFoundError:return True
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--evidence',type=Path,default=Path('/tmp/story65-canonical-hash-acceptance-live-20261108'));parser.add_argument('--output',type=Path,required=True);args=parser.parse_args();run=json.loads((args.evidence/'run.json').read_text());identities=[]
 session=run.get('ownedAppHostSession',{})
 for role in ['appHost','cli']:
  if role+'Pid'in session:identities.append({'role':role,'pid':session[role+'Pid'],'startTicks':session[role+'StartTicks']})
 for name,row in run.get('runtimeModes',{}).items():
  for label,pf,sf in [('host','pid','startTicks'),('wrapper','wrapperPid','wrapperStartTicks')]:identities.append({'role':name+'-'+label,'pid':row[pf],'startTicks':row[sf]})
 for row in run.get('discovery',{}).get('ownedSidecars',[]):identities.append({'role':'sidecar','pid':row['pid'],'startTicks':row['startTicks']})
 command=json.loads(Path(str(args.evidence)+'-command.json').read_text());identities.append({'role':'managed-launcher','pid':command['launcherPid'],'startTicks':command['launcherStartTicks']})
 for row in identities:row['recordedIdentityAbsent']=absent(row['pid'],row['startTicks'])
 containers=[]
 for row in run.get('cleanup',[]):
  if'containerId'not in row:continue
  probe=subprocess.run(['docker','inspect',row['containerId']],capture_output=True,timeout=10);containers.append({'resource':row['resource'],'containerId':row['containerId'],'absenceVerified':probe.returncode!=0 and b'no such object'in probe.stderr.lower()})
 private=next((Path(arg).parent for command in run['commands']for arg in command if isinstance(arg,str)and arg.startswith('/tmp/supported-project-reads-private-')and arg.endswith('/source-mapping')),None)
 oidc=next((row['resource']for row in run.get('cleanup',[])if row['resource'].endswith('-oidc')),None);namespace_absent=None
 if oidc:
  namespace=oidc[:-5];probe=subprocess.run(['docker','ps','--all','--filter','label=hexalith.supported-reads='+namespace,'--format','{{.ID}}'],capture_output=True,timeout=10);namespace_absent=probe.returncode==0 and not probe.stdout.strip()
 manifest=json.loads((args.evidence/'source-manifest.json').read_text());mismatches=sum(sha(ROOT/path)!=digest for path,digest in manifest.items());rows=[];path=args.evidence/'browser-advisory-seed-readiness.jsonl'
 if path.exists():rows=[json.loads(line)for line in path.read_bytes().split(b'\n')[:-1]]
 single=[row for row in rows if row.get('multiple')is False];multi=[row for row in rows if row.get('multiple')is True]
 summary={'ordinaryAcceptanceOnly':True,'profilerEnabled':False,'runResult':run['result'],'matrixCompleted':run.get('matrixCompleted'),'singleAttempts':len(single),'singleVerifiedPositives':sum(row.get('positiveCandidatesVerified')is True for row in single),'multipleAttempts':len(multi),'multipleVerifiedPositives':sum(row.get('positiveCandidatesVerified')is True for row in multi),'multipleExternalFixtureElapsedMedianMs':statistics.median(row['elapsedMilliseconds']for row in multi)if multi else None,'elapsedIncludesSubsequentEvidenceReads':True,'identities':identities,'containers':containers,'namespaceContainersAbsent':namespace_absent,'privateRuntimeDirectoryAbsent':not private.exists()if private else None,'allOriginalCleanupAbsenceVerified':all(row.get('absenceVerified')is True for row in run.get('cleanup',[])),'preexistingResourcesPreserved':run.get('preexistingResourcesPreserved'),'ownedSidecarsAbsent':run.get('ownedSidecarsAbsent'),'privateMaterialRemoved':run.get('privateMaterialRemoved'),'nativeTestProcessCleanup':run.get('testProcessCleanup'),'fullLiveManifestSourceCount':len(manifest),'fullLiveManifestHashMismatches':mismatches,'allIndependentRecordedProcessAndContainerAbsences':all(row['recordedIdentityAbsent']for row in identities)and all(row['absenceVerified']for row in containers),'actualBrowserArtifactsRequireIndependentXmlAndPersistencePrivacyReview':True}
 args.output.write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps({k:summary[k]for k in ['ordinaryAcceptanceOnly','profilerEnabled','runResult','matrixCompleted','singleAttempts','singleVerifiedPositives','multipleAttempts','multipleVerifiedPositives','allIndependentRecordedProcessAndContainerAbsences','fullLiveManifestSourceCount','fullLiveManifestHashMismatches']}))
if __name__=='__main__':main()
