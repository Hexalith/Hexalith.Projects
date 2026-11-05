from pathlib import Path
import datetime,hashlib,importlib.util,json,os,subprocess,sys,time,xml.etree.ElementTree as ET
root=Path('/home/administrator/projects/hexalith/projects');packet=root/'_bmad-output/implementation-artifacts/evidence/story-6-1-platform-maintenance-20261105';output=packet/'parent-review/final-checks';output.mkdir(exist_ok=False)
selected=json.loads((packet/'completion/review-mapping/environment.json').read_text());env=os.environ|selected
spec=importlib.util.spec_from_file_location('story61_parent_final_launcher',root/'tools/qualification/run_supported_project_reads.py');m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
manifest=m.source_manifest();source=hashlib.sha256(json.dumps(manifest,sort_keys=True).encode()).hexdigest();(output/'source-manifest.json').write_text(json.dumps(manifest,sort_keys=True,indent=2)+'\n');(output/'environment.json').write_text(json.dumps(selected,indent=2)+'\n')
results=[]
def command(name,args,cwd,xml=False,allow_skip=False):
 log=output/(name+'.txt');t=time.monotonic()
 with log.open('wb') as f:r=subprocess.run(args,cwd=cwd,env=env,stdout=f,stderr=subprocess.STDOUT,timeout=1800)
 receipt={'command':args,'cwd':str(cwd),'environment':'environment.json','exitCode':r.returncode,'seconds':time.monotonic()-t,'output':log.name,'outputSha256':hashlib.sha256(log.read_bytes()).hexdigest(),'sourceManifestSha256':source}
 if xml:
  path=output/(name+'.xml');assemblies=ET.parse(path).getroot().findall('assembly');receipt['totals']={k:sum(int(a.get(k,'0')) for a in assemblies) for k in ('total','passed','failed','errors','skipped','not-run')};receipt['xml']=path.name;receipt['xmlSha256']=hashlib.sha256(path.read_bytes()).hexdigest()
  counts=receipt['totals'];assert counts['passed']>0 and counts['failed']==counts['errors']==counts['not-run']==0 and (allow_skip or counts['skipped']==0),receipt
 (output/(name+'.command.json')).write_text(json.dumps(receipt,indent=2)+'\n');results.append(receipt);print(json.dumps({'check':name,'exitCode':r.returncode,'totals':receipt.get('totals'),'seconds':receipt['seconds']}),flush=True);assert r.returncode==0,receipt
 assert m.source_manifest()==manifest,'Source drifted during parent final verification'
for project,name in [('Hexalith.EventStore.DomainService.Tests','sdk'),('Hexalith.EventStore.Testing.Tests','helper')]:
 cwd=root/'references/Hexalith.EventStore';command(name+'-build',['dotnet','build','tests/'+project+'/'+project+'.csproj','--configuration','Debug','--verbosity','minimal','--disable-build-servers','-m:1'],cwd)
 command(name+'-full',['dotnet','tests/'+project+'/bin/Debug/net10.0/'+project+'.dll']+(['-class','Hexalith.EventStore.Testing.Tests.OwnerSourceReadTests'] if name=='helper' else [])+['-xml',str(output/(name+'-full.xml'))],cwd,xml=True,allow_skip=name=='sdk')
cwd=root/'references/Hexalith.Folders';project='Hexalith.Folders.Server.Tests';command('folders-build',['dotnet','build','tests/'+project+'/'+project+'.csproj','--configuration','Debug','--verbosity','minimal','--disable-build-servers','-m:1'],cwd)
command('folders-default-full',['dotnet','tests/'+project+'/bin/Debug/net10.0/'+project+'.dll','-xml',str(output/'folders-default-full.xml')],cwd,xml=True)
command('launcher-controls',['python3','-m','unittest','discover','-s','tests/tools','-p','test_supported_project_reads_launcher.py','-v'],root)
(output/'result.json').write_text(json.dumps({'sourceManifestSha256':source,'unchanged':True,'results':results,'result':'passed'},indent=2)+'\n')
