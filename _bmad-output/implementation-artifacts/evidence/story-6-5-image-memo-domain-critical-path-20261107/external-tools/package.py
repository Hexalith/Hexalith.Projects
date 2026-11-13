from pathlib import Path
import json,hashlib,gzip,shutil,subprocess,re,os,statistics
ROOT=Path('/home/administrator/projects/hexalith/projects');OVERLAY=Path('/tmp/story65-owner-domain-overlay-20261107');RUN=Path('/tmp/story65-owner-domain-diagnostic-20261107');PACKET=ROOT/'_bmad-output/implementation-artifacts/evidence/story-6-5-image-memo-domain-critical-path-20261107'
def put(source,target):
 destination=PACKET/target;destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,destination)
def sha(data):return hashlib.sha256(data).hexdigest()
for source in sorted(OVERLAY.iterdir()):
 if source.is_file() and source.suffix!='.jsonl':put(source,Path('private-overlay')/source.name)
for source in sorted(Path('/tmp/story65-owner-domain-prep-20261107').glob('*.py')):put(source,Path('external-tools')/source.name)
for source in sorted(RUN.iterdir()):
 if source.is_file():put(source,Path('diagnostic-run')/source.name)
for source in sorted((RUN/'managed').iterdir()):
 if source.is_file():put(source,Path('diagnostic-run/managed')/source.name)
for source in sorted((RUN/'prior-proof').rglob('*')):
 if source.is_file():put(source,Path('prior-proof')/source.relative_to(RUN/'prior-proof'))
raw=[];allowed={'component','pid','stage','state','ordinal','parent','ticks','elapsedTicks','frequency','traceId','spanId','parentSpanId','count','firstTicks','lastTicks','units','minUnits','maxUnits'};stages=set()
for file in sorted(OVERLAY.glob('*.jsonl')):
 data=file.read_bytes();rows=[json.loads(line) for line in data.splitlines()]
 for row in rows:
  assert set(row)<=allowed;assert row['state'] in ['begin','end','aggregate'];stages.add(row['stage'])
  for name in ['traceId','spanId','parentSpanId']:
   if row.get(name) is not None:assert re.fullmatch('[a-f0-9]{16}|[a-f0-9]{32}',row[name])
 compressed=gzip.compress(data,mtime=0);target=PACKET/'timing'/f'{file.name}.gz';target.parent.mkdir(exist_ok=True);target.write_bytes(compressed);assert gzip.decompress(compressed)==data
 raw.append({'rawFile':file.name,'rawBytes':len(data),'rawSha256':sha(data),'records':len(rows),'retainedFile':str(target.relative_to(PACKET)),'gzipBytes':len(compressed),'gzipSha256':sha(compressed),'decompressedMatchesRaw':True})
(PACKET/'timing-files.json').write_text(json.dumps({'diagnosticOnly':True,'featureAcceptance':False,'files':raw,'closedStageLabels':sorted(stages),'rawTimingSchemaValidated':True},indent=2)+'\n')
run=json.loads((RUN/'managed/run.json').read_text());identities=[]
def current_start(pid):
 try:
  text=Path(f'/proc/{pid}/stat').read_text();parts=text[text.rfind(')')+2:].split();return None if parts[0]=='Z' else parts[19]
 except FileNotFoundError:return None
for name,pidfield,startfield in [('apphost','appHostPid','appHostStartTicks'),('aspire-cli','cliPid','cliStartTicks')]:
 entry=run['ownedAppHostSession'];pid=entry[pidfield];start=str(entry[startfield]);current=current_start(pid);identities.append({'role':name,'pid':pid,'recordedStartTicks':start,'currentStartTicks':current,'recordedIdentityAbsent':current!=start})
for name,entry in run['runtimeModes'].items():
 for role,pf,sf in [('host','pid','startTicks'),('wrapper','wrapperPid','wrapperStartTicks')]:
  pid=entry[pf];start=str(entry[sf]);current=current_start(pid);identities.append({'role':f'{name}-{role}','pid':pid,'recordedStartTicks':start,'currentStartTicks':current,'recordedIdentityAbsent':current!=start})
for entry in run['discovery']['ownedSidecars']:
 pid=entry['pid'];start=str(entry['startTicks']);current=current_start(pid);identities.append({'role':f"{entry['appId']}-sidecar",'pid':pid,'recordedStartTicks':start,'currentStartTicks':current,'recordedIdentityAbsent':current!=start})
containers=[]
for entry in run['cleanup']:
 if 'containerId' not in entry:continue
 probe=subprocess.run(['docker','inspect',entry['containerId']],capture_output=True,text=True,timeout=10)
 containers.append({'resource':entry['resource'],'containerId':entry['containerId'],'absenceVerified':probe.returncode!=0 and 'no such object' in probe.stderr.lower()})
namespace=next(e['resource'][:-len('-oidc')] for e in run['cleanup'] if e['resource'].endswith('-oidc'))
probe=subprocess.run(['docker','ps','--all','--no-trunc','--filter',f'label=hexalith.supported-reads={namespace}','--format','{{.ID}}'],capture_output=True,text=True,timeout=10)
private=next(Path(arg).parent for c in run['commands'] for arg in c if isinstance(arg,str) and arg.startswith('/tmp/supported-project-reads-private-') and arg.endswith('/source-mapping'))
cleanup={'diagnosticOnly':True,'featureAcceptance':False,'originalRunSha256':sha((RUN/'managed/run.json').read_bytes()),'originalResult':run['result'],'originalFailure':run['failure'],'intentionalCutoff':True,'signalTarget':'directly owned managed launcher only','recordedIdentities':identities,'containers':containers,'namespaceContainerListingSucceeded':probe.returncode==0,'namespaceContainersAbsent':probe.returncode==0 and not probe.stdout.strip(),'recordedPrivateDirectory':str(private),'privateDirectoryAbsent':not private.exists(),'originalOwnedSidecarsAbsent':run['ownedSidecarsAbsent'],'originalPreexistingResourcesPreserved':run['preexistingResourcesPreserved'],'originalPrivateMaterialRemoved':run['privateMaterialRemoved'],'originalTestProcessCleanup':run['testProcessCleanup']}
cleanup['allRecordedOwnedAbsencesVerified']=all(x['recordedIdentityAbsent'] for x in identities) and all(x['absenceVerified'] for x in containers) and cleanup['namespaceContainersAbsent'] and cleanup['privateDirectoryAbsent']
assert cleanup['allRecordedOwnedAbsencesVerified'];(PACKET/'cleanup-recheck.json').write_text(json.dumps(cleanup,indent=2)+'\n')
(PACKET/'tested-revisions.json').write_text(json.dumps({'revisions':run['revisions'],'sourceDiffSha256':run['sourceDiffSha256'],'sourceManifestSha256':run['sourceManifestSha256'],'sourceMappingHash':run['sourceMappingHash'],'compilationProfile':run['compilationProfile'],'qualification':'private timing diagnostic only; ordinary acceptance not exercised'},indent=2)+'\n')
print('packaged raw timing',sum(f['records'] for f in raw),'records',len(raw),'files; owned absences',len(identities),'process identities',len(containers),'containers all verified')
