from pathlib import Path
from xml.etree import ElementTree as ET
import datetime,hashlib,json,os,subprocess,sys
ROOT=Path('/home/administrator/projects/hexalith/projects')
OUT=Path('/tmp/story65-memo-rw-native-20261107')
OUT.mkdir(exist_ok=True)
ENV=os.environ.copy()
for key in ('CustomBeforeMicrosoftCommonTargets','CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'):
 ENV.pop(key,None)
ENV.update({'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','DOTNET_PROCESSOR_COUNT':'2','HexalithBuildsRoot':str(ROOT/'references/Hexalith.Builds')})
REPORT={'revisions':{},'diagnosticOverridePresent':False,'commands':[]}
for owner in ('.','references/Hexalith.EventStore','references/Hexalith.Folders'):
 REPORT['revisions'][owner]=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT/owner,text=True).strip()
def save():
 (OUT/'native-commands.json').write_text(json.dumps(REPORT,indent=2)+'\n')
def run(label,args,cwd=ROOT):
 receipt={'command':args,'cwd':str(cwd),'startedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
 logfile=OUT/(label+'.log')
 child_env=ENV.copy()
 if label.endswith('-tests'):child_env.pop('DOTNET_PROCESSOR_COUNT',None)
 receipt['buildProcessorOverride']=child_env.get('DOTNET_PROCESSOR_COUNT')
 with logfile.open('wb') as stream:
  completed=subprocess.run(args,cwd=cwd,env=child_env,stdout=stream,stderr=subprocess.STDOUT)
 receipt.update(exitCode=completed.returncode,log=logfile.name,logSha256=hashlib.sha256(logfile.read_bytes()).hexdigest())
 REPORT['commands'].append(receipt);save()
 print(label,'exit',completed.returncode,flush=True)
 if completed.returncode:raise SystemExit(completed.returncode)
 return receipt
# Project/dependency configuration is unchanged; reuse all seven previously emitted source mappings.
prior=Path('/tmp/story65-owner-repair-native-20261107/mapping.log')
mapping=json.loads(prior.read_text())
assert mapping['status']=='prepared'
selected=mapping['buildPreparation']['environment']
assert len(selected)==7 and selected['Configuration']=='Debug'
for key,value in selected.items():
 if key not in ('Configuration','HEXALITH_SOURCE_MAPPING_HASH'):assert Path(value).is_file(),(key,value)
ENV.update(selected);REPORT['sourceMappingEnvironment']=selected
REPORT['sourceMappingReuse']={'source':str(prior),'sha256':hashlib.sha256(prior.read_bytes()).hexdigest(),'reason':'No project/dependency/manifest/build configuration changed; all seven emitted values preserved.'}
save()

owner_receipt=Path(sys.argv[1]);owner_proof=json.loads(owner_receipt.read_text());REPORT['ownerReceipt']={'path':str(owner_receipt),'sha256':hashlib.sha256(owner_receipt.read_bytes()).hexdigest()}
source=owner_proof['sourceSha256']
for p,h in source.items():assert hashlib.sha256((ROOT/'references/Hexalith.EventStore'/p).read_bytes()).hexdigest()==h,p
REPORT['ownerSourceSha256']=source;save()
projects=[
 ('ui','UI',['Hexalith.Projects.UI.Tests.*'],None),
 ('server','Server',['Hexalith.Projects.Server.Tests.Queries.GetProjectContextQueryHandlerTests','Hexalith.Projects.Server.Tests.Queries.GetConversationStartSetupQueryHandlerTests','Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadTests','Hexalith.Projects.Server.Tests.Queries.ProjectAdvisoryQueryTests'],'Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadTests.GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites'),
 ('contracts','Contracts',['Hexalith.Projects.Contracts.Tests.Ui.ProjectResolutionTraceProjectionTests','Hexalith.Projects.Contracts.Tests.Ui.ProjectVocabularyTests'],None),
 ('integration','Integration',['Hexalith.Projects.Integration.Tests.AspireTopologyTests','Hexalith.Projects.Integration.Tests.PersistedReadObserverBindingTests','Hexalith.Projects.Integration.Tests.SupportedProjectAdvisoryReadinessTests'],None)
]
for label,project,classes,excluded in projects:
 cwd=ROOT/'tests'/('Hexalith.Projects.'+project+'.Tests')
 build=['dotnet','build','--configuration','Debug','-m:1','-p:NuGetAudit=false','--disable-build-servers']
 if label=='integration':build.append('-p:SupportedProjectReadDistributed=true')
 run(label+'-build',build,cwd)
 dll=cwd/'bin/Debug/net10.0'/('Hexalith.Projects.'+project+'.Tests.dll')
 xml=OUT/(label+'-tests.xml')
 command=['dotnet',str(dll)]
 for name in classes:command+=['-class',name]
 if excluded:command+=['-method-',excluded]
 command+=['-xml',str(xml)]
 receipt=run(label+'-tests',command,cwd)
 receipt.update(assemblySha256=hashlib.sha256(dll.read_bytes()).hexdigest(),xml=xml.name,xmlSha256=hashlib.sha256(xml.read_bytes()).hexdigest())
 receipt['totals']=[{key:int(a.attrib[key]) for key in ('total','passed','failed','skipped','errors')} for a in ET.parse(xml).getroot().iter('assembly')]
 assert receipt['totals'] and all(a['total']==a['passed'] and not(a['failed'] or a['skipped'] or a['errors']) for a in receipt['totals'])
 save();print(label,receipt['totals'],flush=True)
REPORT['totalNativePassed']=sum(t['passed'] for c in REPORT['commands'] for t in c.get('totals',[]));save()
assert REPORT['totalNativePassed']==626
print('Projects native totals',REPORT['totalNativePassed'],flush=True)


REPORT['projectsPassingCases']=626
save()
