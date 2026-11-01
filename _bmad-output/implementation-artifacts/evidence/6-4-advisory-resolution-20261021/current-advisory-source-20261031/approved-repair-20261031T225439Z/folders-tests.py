"""Direct xUnit v3 selectors and full assemblies using command-owned Redis/Dapr."""
from record import ROOT,EVIDENCE,WORK,BASE,run,write,digest
from pathlib import Path
import datetime,json,os,secrets,shutil,signal,socket,subprocess,time,urllib.request
import xml.etree.ElementTree as ET

SOURCE=Path(json.loads((EVIDENCE/'isolated-workspace.json').read_text())['root'])
projects=json.loads((EVIDENCE/'projects-environment.json').read_text())
conversations=json.loads((EVIDENCE/'conversations-environment.json').read_text())
RESULTS={}
def suite(name,assembly,selectors,explicit):
    argv=['dotnet',str(assembly)]
    for selector in selectors:argv.extend(['-class',selector])
    argv.extend(['-parallel','none','-result-xml',str(EVIDENCE/(name+'.xml')),'-failSkips'])
    def closure():return {str(p.relative_to(assembly.parent)):digest(p) for p in assembly.parent.iterdir() if p.is_file() and p.suffix in ('.dll','.json','.pdb')}
    before=closure();write(name+'-executables-before.json',before)
    code=run(name,argv,cwd=assembly.parents[4],explicit=explicit,private=True,timeout=900)
    after=closure();write(name+'-executables-after.json',after)
    summary={'exit_code':code,'executable_closure_unchanged':before==after,'xml_present':(EVIDENCE/(name+'.xml')).is_file()}
    if summary['xml_present']:
        tree=ET.parse(EVIDENCE/(name+'.xml'));a=tree.find('assembly')
        summary.update({key:int(a.get(key,0)) for key in ['total','passed','failed','skipped','errors','not-run']})
        summary['nonpassing']=[{'name':t.get('name'),'result':t.get('result'),'message':t.findtext('failure/message','')} for t in tree.findall('.//test') if t.get('result')!='Pass']
    write(name+'-result.json',summary);RESULTS[name]=summary
    print(name+': '+json.dumps(summary),flush=True)
    return code==0 and summary.get('total',0)>0 and summary.get('passed')==summary.get('total') and summary.get('errors')==0 and before==after

def assembly(owner,name):return SOURCE/owner/'tests'/name/'bin/Debug/net10.0'/(name+'.dll')
contracts=assembly('.','Hexalith.Projects.Contracts.Tests');core=assembly('.','Hexalith.Projects.Tests');server=assembly('.','Hexalith.Projects.Server.Tests')
conversation=assembly('references/Hexalith.Conversations','Hexalith.Conversations.Server.Tests')
folders=assembly('references/Hexalith.Folders','Hexalith.Folders.Server.Tests')
checks=[]
checks.append(suite('folders-owner-focused',folders,['*FolderOwnerReadQueryHandlerTests'],{'Configuration':'Debug'}))
checks.append(suite('folders-server-full',folders,[],{'Configuration':'Debug'}))
write('folders-test-summary.json',RESULTS)
if not all(checks):raise SystemExit(1)
