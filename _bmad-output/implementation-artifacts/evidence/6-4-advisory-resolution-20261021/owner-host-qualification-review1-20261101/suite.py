from record import ROOT,EVIDENCE,WORK,run,write,digest
from pathlib import Path
import json,sys,xml.etree.ElementTree as ET
SOURCE=Path(json.loads((EVIDENCE/'workspace.json').read_text())['source'])
def suite(label,owner,project,selectors=(),env=None):
    assembly=SOURCE/owner/'tests'/project/'bin/Debug/net10.0'/(project+'.dll')
    def closure():return {p.name:digest(p) for p in assembly.parent.iterdir() if p.is_file() and p.suffix in ('.dll','.pdb','.json')}
    before=closure();write(label+'-executables-before.json',before)
    argv=['dotnet',str(assembly)]
    for selector in selectors:argv.extend(['-class',selector])
    argv.extend(['-parallel','none','-result-xml',str(EVIDENCE/(label+'.xml')),'-failSkips'])
    code=run(label,argv,cwd=SOURCE/owner,explicit=env or {},private=True,timeout=900)
    after=closure();write(label+'-executables-after.json',after)
    result={'exit_code':code,'executable_closure_unchanged':before==after}
    if (EVIDENCE/(label+'.xml')).exists():
        tree=ET.parse(EVIDENCE/(label+'.xml'));a=tree.find('assembly')
        result.update({key:int(a.get(key,0)) for key in ['total','passed','failed','skipped','errors','not-run']})
        result['nonpassing']=[{'name':t.get('name'),'result':t.get('result'),'message':t.findtext('failure/message',''),'stack':t.findtext('failure/stack-trace','')} for t in tree.findall('.//test') if t.get('result')!='Pass']
    write(label+'-result.json',result)
    print(json.dumps({k:v for k,v in result.items() if k!='nonpassing'}),flush=True)
    return result
if __name__=='__main__':
    owner,label,*selectors=sys.argv[1:]
    folder,project={'conversations':('references/Hexalith.Conversations','Hexalith.Conversations.Server.Tests'),'folders':('references/Hexalith.Folders','Hexalith.Folders.Server.Tests')}[owner]
    env={'Configuration':'Debug'} if owner=='folders' else json.loads((EVIDENCE/(owner+'-environment.json')).read_text())
    result=suite(label,folder,project,selectors,env)
    raise SystemExit(result['exit_code'])
