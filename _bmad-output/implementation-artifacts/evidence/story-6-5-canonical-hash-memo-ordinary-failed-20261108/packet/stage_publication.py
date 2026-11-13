"""Copy safe closed acceptance metadata into fresh TEMP staging; never publish or mutate original inputs."""
from pathlib import Path
import hashlib,json,re,shutil
A=Path('/tmp/story65-canonical-hash-acceptance-independent-audit-20261108')
P=Path('/tmp/story65-canonical-hash-acceptance-prep-20261108')
L=Path('/tmp/story65-canonical-hash-acceptance-live-20261108')
D=Path('/tmp/story65-canonical-hash-acceptance-failure-evidence-20261108')
H=Path('/tmp/story65-canonical-hash-acceptance-history-20261108/census-preflight')
entries={}
sha=lambda b:hashlib.sha256(b).hexdigest()
def read(p):return json.loads(Path(p).read_bytes())
def add(source,dest):
 source=Path(source);assert source.is_file() and not source.is_symlink();data=source.read_bytes()
 out=D/dest;assert not out.exists();out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(data)
 entries[dest]={'sha256':sha(data),'bytes':len(data),'sourcePath':str(source),'sourceSha256':sha(data),'exactBytes':True}
def derived(dest,obj):
 data=(json.dumps(obj,indent=2)+'\n').encode();out=D/dest;assert not out.exists();out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(data)
 entries[dest]={'sha256':sha(data),'bytes':len(data),'closedDerivedMetadata':True}
def main():
 assert not D.exists();D.mkdir(mode=0o755)
 audit=read(A/'independent-ordinary-acceptance-audit.json');assert audit['passed'] is True and audit['featureAcceptance'] is False
 for name in ['audit.py','independent-ordinary-acceptance-audit.json','source-generation-profile-proof.json','safe-readiness.json','safe-seed-xml.json']:
  add(A/name,'independent/'+name)
 for path in sorted((A/'history').rglob('*')):
  if path.is_file():add(path,'independent/history/'+str(path.relative_to(A/'history')))
 generic=read(A/'generic-readonly-audit.json')
 derived('independent/generic-readonly-closed-summary.json',{'originalAuditSha256':sha((A/'generic-readonly-audit.json').read_bytes()),**{k:v for k,v in generic.items() if not isinstance(v,(dict,list))}})
 for name in ['supervise.py','audit.py','commands.json','preparation-receipt.json','README.md']:
  add(P/name,'supervisor/prepared/'+name)
 add(Path('/tmp/story65-canonical-hash-acceptance-release-census-corrected-20261108.json'),'supervisor/actual-release.json')
 for suffix in ['supervisor.json','postflight-generations.json','preflight.json','cleanup-observation-history.json']:
  add(Path(str(L)+'-'+suffix),'supervisor/'+suffix)
 for path in sorted(H.iterdir()):
  if path.is_file():add(path,'supervisor/history/census-preflight/'+path.name)
 observations=[]
 for path in sorted(L.parent.glob(L.name+'-cleanup-observation-[0-9]*.json')):
  row=read(path)
  keys=['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent','fullLiveManifestSourceCount','fullLiveManifestHashMismatches']
  observations.append({'ordinal':len(observations)+1,'sourceSha256':sha(path.read_bytes()),**{k:row[k] for k in keys},'recordedProcessIdentities':len(row['identities']),'stillPresentRecordedIdentities':sum(v['recordedIdentityAbsent'] is not True for v in row['identities']),'recordedOwnedContainers':len(row['containers']),'containerAbsencesVerified':sum(v['absenceVerified'] is True for v in row['containers'])})
 assert len(observations)==5 and observations[-1]['allIndependentRecordedProcessAndContainerAbsences'] is True
 derived('supervisor/closed-cleanup-observations.json',{'normalCompletionNoSignalsOrMutation':True,'observations':observations})
 command=read(Path(str(L)+'-command.json'))
 assert command['command']==['npm','--prefix','tests/e2e','run','test:live:managed']
 derived('supervisor/safe-managed-command.json',{'sourceSha256':sha(Path(str(L)+'-command.json').read_bytes()),'command':command['command'],'launcherPid':command['launcherPid'],'launcherStartTicks':command['launcherStartTicks'],'startedUtc':command['startedUtc'],'sourceMappingHooksClearedBeforeOriginalLauncherReemitsSeven':command['sourceMappingHooksClearedBeforeOriginalLauncherReemitsSeven'],'runtimeCpuOverrideAbsent':True,'noLauncherReplacementNoBuildCutoffOrTargetSignals':True,'rawEnvironmentNotCopied':True})
 binding=read(Path('/tmp/story65-canonical-hash-acceptance-release-census-corrected-20261108.json'))
 add(Path(binding['sourceControls']['path']),'inputs/source-controls65.json')
 add(L/'source-manifest.json','inputs/full-source-manifest.json')
 original=read(L/'run.json')
 derived('supervisor/original-cleanup-privacy-closed-flags.json',{'sourceRunSha256':sha((L/'run.json').read_bytes()),'acceptedEquivalence':original['acceptedEquivalence'],'productionReadiness':original['productionReadiness'],'matrixCompleted':original['matrixCompleted'],'result':original['result'],'originalCleanupFlags':[{k:v for k,v in row.items() if k in {'removed','alreadyAbsent','absenceVerified'}} for row in original['cleanup']],'ownedSidecarsObservedBeforeStop':original['ownedSidecarsObservedBeforeStop'],'ownedSidecarsAbsent':original['ownedSidecarsAbsent'],'preexistingResourcesPreserved':original['preexistingResourcesPreserved'],'privateMaterialRemoved':original['privateMaterialRemoved'],'freshArtifacts':original['freshArtifacts'],'launcherControlTotal':original['launcherControls']['total'],'launcherControlPassed':original['launcherControls']['passed'],'launcherControlFailed':original['launcherControls']['failed'],'launcherControlSkipped':original['launcherControls']['skipped'],'nativeTestProcessCleanup':original['testProcessCleanup'],'rawFailureTextCommandsGraphObjectsEnvAndCredentialsExcluded':True})
 derived('qualification-reference.json',{'alreadyPublishedRepairPacket':'_bmad-output/implementation-artifacts/evidence/story-6-5-canonical-hash-memo-qualified-20261108','repairPacketManifestSha256':'ce311643d2490ead2debdeb7f4fe15cb85ffced80484b49d0621bdc2a825868f','repairPacketSumsSha256':'0bd24a1a3f6d9d85d230665f4f9d4e9094f3589859448b8ff49a5a609a773e07','native1076AndCompiledComponentQualificationNotRepeatedOrInflated':True,'ordinaryAcceptanceResult':'failed','failureCauseInferred':False})
 add(A/'README-publication.md','README.md');add(Path(__file__),'packet/stage_publication.py')
 for p in D.rglob('*'):
  assert not p.is_symlink()
  if p.is_file():assert p.suffix not in {'.dll','.pdb','.log','.xml','.nettrace','.etlx','.jsonl'},p
 manifest={'schemaVersion':1,'safePublicationCandidate':True,'published':False,'featureAcceptance':False,'artifactCount':len(entries),'artifacts':entries,'rawOriginalRunReadinessObjectsBrowserXmlAndLogsNotCopied':True,'correctionHistoryIsReadOnlyAuditMetadataOnly':True}
 (D/'artifact-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 sums=[sha(p.read_bytes())+'  '+str(p.relative_to(D)) for p in sorted(D.rglob('*')) if p.is_file()]
 (D/'SHA256SUMS').write_text('\n'.join(sums)+'\n')
 for p in D.rglob('*'):
  if p.is_file():p.chmod(0o444)
 print(json.dumps({'path':str(D),'artifactCount':len(entries),'filesIncludingManifestAndSums':len(entries)+2,'manifestSha256':sha((D/'artifact-manifest.json').read_bytes()),'sumsSha256':sha((D/'SHA256SUMS').read_bytes()),'bytes':sum(p.stat().st_size for p in D.rglob('*') if p.is_file())}))
if __name__=='__main__':main()
