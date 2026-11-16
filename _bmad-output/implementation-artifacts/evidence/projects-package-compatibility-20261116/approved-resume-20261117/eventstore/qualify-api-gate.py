#!/usr/bin/env python3
"""Run the unchanged owner API gate with the exact approved Contracts archive."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

BASE=Path(__file__).resolve().parent
OUT=BASE/'api-attempt-2'
OUT.mkdir(exist_ok=False)
ROOT=Path('/home/administrator/projects/hexalith/projects')
SCRATCH=Path(os.environ['TMPDIR'])
SOURCE='0be0c922883d4ea61b18e843e24472162261d934'
BUILDS='e2bbbd0893f586e4d447ef8358ff747ea3d34435'
WORK=SCRATCH/'Hexalith.EventStore'
ENV=os.environ.copy()
ENV.update(NUGET_PACKAGES=str(SCRATCH/'packages'),DOTNET_CLI_HOME=str(SCRATCH/'dotnet-home'),DOTNET_CLI_USE_MSBUILD_SERVER='0')
RESULT={'source':SOURCE,'builds_source':BUILDS,'started_at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'steps':[],'status':'running','publication_performed':False}

def save():
    (OUT/'owner-api-gate-qualification.json').write_text(json.dumps(RESULT,indent=2)+'\n')

def run(label,args,cwd=ROOT,required=True):
    p=OUT/(label+'.log')
    with p.open('xb') as f:r=subprocess.run(args,cwd=cwd,env=ENV,stdout=f,stderr=subprocess.STDOUT)
    RESULT['steps'].append({'label':label,'command':args,'exit':r.returncode,'log':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()});save();print(label,r.returncode,flush=True)
    if required and r.returncode:raise RuntimeError(label+' failed')
    return r.returncode

try:
    run('api-owner-clone',['git','clone','--shared','--no-checkout',str(ROOT/'references/Hexalith.EventStore'),str(WORK)])
    run('api-owner-origin',['git','remote','set-url','origin','https://github.com/Hexalith/Hexalith.EventStore.git'],WORK)
    run('api-owner-checkout',['git','-c','submodule.recurse=false','checkout','--detach',SOURCE],WORK)
    target=WORK/'references/Hexalith.Builds'
    run('api-builds-clone',['git','clone','--shared','--no-checkout',str(ROOT/'references/Hexalith.Builds'),str(target)])
    run('api-builds-origin',['git','remote','set-url','origin','https://github.com/Hexalith/Hexalith.Builds.git'],target)
    run('api-builds-checkout',['git','-c','submodule.recurse=false','checkout','--detach',BUILDS],target)
    feed=SCRATCH/'candidate-feed'
    run('api-local-inventory',['python3','tools/pack-release-packages.py',str(feed),'5.3.0'],WORK)
    approved=BASE.parents[1]/'producer-replay-20261116T214600Z/unpublished-candidates/Hexalith.EventStore.Contracts.5.3.0.nupkg'
    if hashlib.sha256(approved.read_bytes()).hexdigest()!='1c3bdabc4778467d74b468a47f0413f5f85272539d9969891036c8865f6949c3':raise ValueError('Approved artifact changed')
    shutil.copy2(approved,feed/approved.name)
    RESULT['inventory']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(feed.glob('*.nupkg'))}
    RESULT['exact_approved_contracts_archive']=RESULT['inventory'][approved.name]
    work=SCRATCH/'api-evidence'
    exit_code=run('owner-api-gate',['python3','tools/verify-release-api-compatibility.py',str(feed),'5.3.0','--candidate-commit',SOURCE,'--work-directory',str(work)],WORK,required=False)
    retained=OUT/'api-gate-evidence'
    if work.exists():shutil.copytree(work,retained,ignore=shutil.ignore_patterns('scratch'))
    RESULT['gate_evidence']='api-gate-evidence';RESULT['status']='passed' if exit_code==0 else 'failed'
except Exception as e:
    RESULT['status']='failed';RESULT['error']=str(e)
finally:
    RESULT['ended_at']=datetime.datetime.now(datetime.timezone.utc).isoformat();save();print(json.dumps(RESULT,indent=2),flush=True)
