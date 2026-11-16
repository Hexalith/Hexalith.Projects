#!/usr/bin/env python3
"""Qualify exact owner source in a private standalone repository with direct dependencies."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

OUT = Path(__file__).resolve().parent
ROOT = Path('/home/administrator/projects/hexalith/projects')
SOURCE = '0be0c922883d4ea61b18e843e24472162261d934'
REMOTE = 'd8171e6cc18cd4b67ea310822b36e49d007fa2ec'
SCRATCH = Path(os.environ['TMPDIR'])
WORK = SCRATCH / 'Hexalith.EventStore'
ENV = os.environ.copy()
ENV.update(NUGET_PACKAGES=str(SCRATCH/'packages'), DOTNET_CLI_HOME=str(SCRATCH/'dotnet-home'), DOTNET_CLI_USE_MSBUILD_SERVER='0')
CONFIG = SCRATCH/'NuGet.Config'
CONFIG.write_text('<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><clear/><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>\n')
ENV['RestoreConfigFile'] = str(CONFIG)
RESULT = {'source':SOURCE, 'observed_remote_main':REMOTE, 'started_at':datetime.datetime.now(datetime.timezone.utc).isoformat(), 'status':'running','steps':[],'direct_dependencies':{},'publication_performed':False,'nested_submodule_initialization':False}

def save():
    (OUT/'owner-qualification.json').write_text(json.dumps(RESULT,indent=2)+'\n')

def run(label,args,cwd=WORK,required=True,timeout=2400):
    log=OUT/(label+'.log')
    with log.open('xb') as f:
        try:
            p=subprocess.run(args,cwd=cwd,env=ENV,stdout=f,stderr=subprocess.STDOUT,timeout=timeout)
            code=p.returncode
        except subprocess.TimeoutExpired:
            code=124
    RESULT['steps'].append({'label':label,'command':args,'exit':code,'required':required,'log':log.name,'sha256':hashlib.sha256(log.read_bytes()).hexdigest()})
    save(); print(label,code,flush=True)
    if required and code:raise RuntimeError(label+' failed; inspect '+str(log))
    return code

def git(*args,cwd=WORK):
    return subprocess.check_output(['git','-C',str(cwd),*args],env=ENV,text=True).strip()

try:
    run('owner-clone',['git','clone','--shared','--no-checkout',str(ROOT/'references/Hexalith.EventStore'),str(WORK)],cwd=ROOT)
    run('owner-origin',['git','remote','set-url','origin','https://github.com/Hexalith/Hexalith.EventStore.git'])
    run('owner-current-source',['git','-c','submodule.recurse=false','fetch','--no-recurse-submodules','--no-tags','origin',REMOTE])
    run('owner-approved-checkout',['git','-c','submodule.recurse=false','checkout','--detach',SOURCE])
    RESULT['source_tree']=git('rev-parse','HEAD^{tree}')
    RESULT['source_status_before']=git('status','--porcelain=v1')
    RESULT['live_query_difference']=git('diff',SOURCE,REMOTE,'--','src/Hexalith.EventStore.Contracts/Queries','src/Hexalith.EventStore.RestApi.Generators')
    RESULT['live_owner_difference']=git('diff','--name-status',SOURCE,REMOTE)
    for path in ['.github/workflows/release.yml','scripts/verify-release-source.sh','scripts/validate-publication-preflight.sh','.releaserc.json']:
        payload=subprocess.check_output(['git','-C',str(WORK),'show',REMOTE+':'+path])
        target=OUT/'current-owner-protocol'/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(payload)
    for line in git('ls-tree','-r',SOURCE,'references').splitlines():
        before,path=line.split('\t',1);mode,kind,rev=before.split()
        if mode!='160000':continue
        name=Path(path).name
        candidates=[ROOT/path,Path('/home/administrator/projects/hexalith/cicd')/path]
        local=next((p for p in candidates if (p/'.git').exists()),None)
        target=WORK/path
        run('clone-'+name,['git','clone','--shared','--no-checkout',str(local),str(target)] if local else ['git','clone','--no-checkout','https://github.com/Hexalith/'+name+'.git',str(target)])
        run('origin-'+name,['git','remote','set-url','origin','https://github.com/Hexalith/'+name+'.git'],target)
        present=subprocess.run(['git','-C',str(target),'cat-file','-e',rev+'^{commit}'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
        if not present:run('fetch-'+name,['git','-c','submodule.recurse=false','fetch','--no-recurse-submodules','--no-tags','origin',rev],target)
        run('checkout-'+name,['git','-c','submodule.recurse=false','checkout','--detach',rev],target)
        RESULT['direct_dependencies'][path]={'source':rev,'actual_source':git('rev-parse','HEAD',cwd=target),'origin':git('remote','get-url','origin',cwd=target),'nested_initialized':bool(git('config','--local','--get-regexp',r'^submodule\.',cwd=target)) if subprocess.run(['git','-C',str(target),'config','--local','--get-regexp',r'^submodule\.'],stdout=subprocess.DEVNULL).returncode==0 else False}
        save()
    common=['--configuration','Release','-p:UseHexalithProjectReferences=false','-p:Version=5.3.0','-m:1','--disable-build-servers']
    project='tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj'
    run('contracts-build',['dotnet','build',project,*common,'-warnaserror'])
    # The owner tests require a complete local release inventory. Producing disposable
    # packages is qualification only; no package leaves this private feed.
    inventory=WORK/'qualification-packages'
    run('full-local-release-inventory',['python3','tools/pack-release-packages.py',str(inventory),'5.3.0'])
    RESULT['inventory']={'package_count':len(list(inventory.glob('*.nupkg'))),'packages':[p.name for p in sorted(inventory.glob('*.nupkg'))],'scope':'disposable qualification inputs only; no family publication'}
    ENV['EVENTSTORE_PACKAGE_CONTRACT_DIR']=str(inventory)
    xml=OUT/'full-contracts-tests.xml'
    run('full-contracts-tests',['dotnet',str(WORK/'tests/Hexalith.EventStore.Contracts.Tests/bin/Release/net11.0/Hexalith.EventStore.Contracts.Tests.dll'),'-xml',str(xml)],required=False,timeout=2700)
    root=ET.parse(xml).getroot();assemblies=root.findall('assembly')
    RESULT['full_contracts_tests']={k:sum(int(a.attrib[k]) for a in assemblies) for k in ['total','passed','failed','skipped','errors','not-run']}
    RESULT['full_contracts_tests']['failures']=[{'name':t.attrib['name'],'result':t.attrib['result'],'message':t.findtext('failure/message') or t.findtext('reason/message')} for t in root.findall('.//test') if t.attrib['result']!='Pass']
    RESULT['source_status_after']=git('status','--porcelain=v1','--untracked-files=no')
    RESULT['status']='passed' if RESULT['full_contracts_tests']['passed']==RESULT['full_contracts_tests']['total'] and RESULT['source_status_after']=='' else 'failed'
except Exception as e:
    RESULT['status']='failed';RESULT['error']=str(e)
finally:
    RESULT['ended_at']=datetime.datetime.now(datetime.timezone.utc).isoformat();save()
    print(json.dumps({k:v for k,v in RESULT.items() if k not in ['steps','live_owner_difference']},indent=2),flush=True)
