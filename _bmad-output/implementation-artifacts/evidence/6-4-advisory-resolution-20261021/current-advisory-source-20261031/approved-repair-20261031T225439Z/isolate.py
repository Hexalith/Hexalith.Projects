"""Build disposable source checkouts; never build shared Parties or initialize submodules."""
from record import ROOT,EVIDENCE,WORK,run,write,git,digest,sources
from pathlib import Path
import json,shutil

def checked(name,argv,cwd=ROOT,**kw):
    code=run(name,argv,cwd=cwd,**kw)
    if code:raise SystemExit(code)

declarations=git(ROOT,'config','-f','.gitmodules','--get-regexp',r'^submodule\..*\.path$').decode().splitlines()
modules=[line.split(' ',1)[1] for line in declarations]
identity=[]
for relative in modules:
    repo=ROOT/relative;head=git(repo,'rev-parse','HEAD').decode().strip()
    indexed=git(ROOT,'ls-files','--stage','--',relative).decode().split()[1]
    assert head==indexed,(relative,head,indexed)
    identity.append({'path':relative,'head':head,'root_gitlink':indexed,'checkout_gitlink_equal':True})
write('direct-source-inventory.json',identity)
copyroot=WORK/'source';copyroot.mkdir()
for i,relative in enumerate(['.',*modules]):
    repo=ROOT/relative;destination=copyroot/relative if relative!='.' else copyroot/'projects'
    if relative=='.':isolated=destination
    else:destination=isolated/relative
    checked('clone-'+str(i),['git','clone','--shared','--no-checkout',str(repo),str(destination)],cwd=WORK)
    checked('sparse-'+str(i),['git','sparse-checkout','set','--no-cone','/*','!/references/'],cwd=destination)
    head=git(repo,'rev-parse','HEAD').decode().strip()
    checked('checkout-'+str(i),['git','-c','submodule.recurse=false','checkout','--detach',head],cwd=destination)
# Overlay only build/source inputs, including the already dirty catalog fixtures and approved changes.
sourcehashes=sources()
for relative,expected in sourcehashes.items():
    source=ROOT/relative;destination=isolated/relative
    destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,destination)
    assert digest(destination)==expected
write('isolated-source-inputs.json',sourcehashes)
write('isolated-workspace.json',{'root':str(isolated),'all_build_outputs_owned':True,'git_clones_share_objects_read_only':True,'nested_submodules_initialized':False,'all_shared_input_hashes_match':True})
cli=isolated/'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli'
checked('isolated-builds-cli-build',['dotnet','build',str(cli/'Hexalith.Builds.Module.Cli.csproj'),'-c','Debug','-m:1','-v:minimal'],cwd=isolated/'references/Hexalith.Builds',timeout=600)
for owner,manifest in [('projects',isolated/'module/hexalith.module-manifest.v2.json'),('conversations',isolated/'references/Hexalith.Conversations/module/hexalith.module-manifest.v2.json')]:
    cwd=manifest.parent.parent
    checked(owner+'-source-prepare',['dotnet',str(cli/'bin/Debug/net10.0/Hexalith.Builds.Module.Cli.dll'),'prepare-build','--manifest',str(manifest),'--mode','source','--workspace',str(WORK/(owner+'-mapping')),'--output','json'],cwd=cwd)
    result=json.loads((EVIDENCE/(owner+'-source-prepare.stdout.log')).read_text());assert result['status']=='prepared'
    write(owner+'-environment.json',result['buildPreparation']['environment'])
print('Isolated source prepared',flush=True)
