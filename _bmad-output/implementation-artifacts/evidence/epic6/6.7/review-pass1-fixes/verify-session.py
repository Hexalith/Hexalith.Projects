"""Owned incremental verification session; run under run-with-temp and submit one JSON job per line."""
import json, os, socket, subprocess, time, urllib.request
from pathlib import Path
original=Path.cwd(); scratch=Path(os.environ['TMPDIR']); root=scratch/'source'; out=original/'_bmad-output/implementation-artifacts/evidence/epic6/6.7/review-pass1-fixes'
env=os.environ.copy(); commands=[]
def run(label,cmd,cwd=root):
    path=out/(label+'.log')
    if path.exists(): raise RuntimeError('Immutable receipt already exists: '+str(path))
    with path.open('w') as log:r=subprocess.run(cmd,cwd=cwd,env=env,stdout=log,stderr=subprocess.STDOUT)
    commands.append({'label':label,'command':cmd,'cwd':str(cwd),'exitCode':r.returncode})
    (out/'session-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
    print(label,'exit',r.returncode,flush=True);print(path.read_text()[-2500:],flush=True)
    return r.returncode
# A private copy excludes pre-existing nested submodules. Metadata and source are copied;
# no checkout, branch, dependency update or mutation of the user's Git state is performed.
import shutil, re
ignored={"bin", "obj", "node_modules", "artifacts", "TestResults", ".venv", "validation", "nupkgs", ".pytest_cache", ".playwright-mcp", ".git", "references", "_bmad-output", "_bmad", ".agents", ".codex", ".claude", ".aspire"}
shutil.copytree(original,root,symlinks=True,ignore=lambda folder,names: [name for name in names if name in ignored])
(root/'.git').symlink_to(original/'.git',target_is_directory=True)
for name in ['_bmad-output','_bmad','.agents']:
    if (original/name).exists():(root/name).symlink_to(original/name,target_is_directory=True)
(root/'references').mkdir()
for source in (original/'references').iterdir():
    if not source.is_dir():continue
    target=root/'references'/source.name
    shutil.copytree(source,target,symlinks=True,ignore=lambda folder,names: [name for name in names if name in ignored])
    marker=source/'.git'
    if marker.is_file():
        gitdir=(source/marker.read_text().strip().split('gitdir: ',1)[1]).resolve()
        metadata=target/'.git';metadata.mkdir()
        for name in ['HEAD','index','packed-refs','shallow']:
            if (gitdir/name).exists():shutil.copy2(gitdir/name,metadata/name)
        if (gitdir/'refs').exists():shutil.copytree(gitdir/'refs',metadata/'refs')
        (metadata/'objects').symlink_to(gitdir/'objects',target_is_directory=True)
        config=(gitdir/'config').read_text()
        config=re.sub(r'(?m)^\s*worktree\s*=.*$', '\tworktree = '+str(target), config)
        (metadata/'config').write_text(config)
# Correct the default argument after selecting the private source root.
run.__defaults__=(root,)
mapper=original/'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net11.0/Hexalith.Builds.Module.Cli.dll'
if run('private-source-mapping',['dotnet',str(mapper),'prepare-build','--manifest','module/hexalith.module-manifest.v2.json','--mode','source','--workspace',str(scratch/'prepared'),'--output','json']):raise SystemExit(1)
env.update(json.loads((out/'private-source-mapping.log').read_text())['buildPreparation']['environment'])
for entry in root.iterdir():
    p=scratch/entry.name
    if not p.exists():p.symlink_to(entry,target_is_directory=entry.is_dir())
resources=scratch/'dapr-resources';resources.mkdir()
(resources/'statestore.yaml').write_text('apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.in-memory\n  version: v1\n  metadata: []\n')
(resources/'conversation-start.yaml').write_text('apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: projects-conversation-start-setup\nspec:\n  type: state.in-memory\n  version: v1\n  metadata: []\n')
ports=[]
for _ in range(4):
    with socket.socket() as s:s.bind(('127.0.0.1',0));ports.append(s.getsockname()[1])
log=(out/'final-session-daprd.log').open('w')
child=subprocess.Popen([str(Path.home()/'.dapr/bin/daprd'),'--app-id','story67-v2-'+str(os.getpid()),'--resources-path',str(resources),'--dapr-http-port',str(ports[0]),'--dapr-grpc-port',str(ports[1]),'--metrics-port',str(ports[2]),'--dapr-internal-grpc-port',str(ports[3]),'--log-level','warn'],stdout=log,stderr=subprocess.STDOUT)
try:
    for _ in range(100):
        try:urllib.request.urlopen('http://127.0.0.1:'+str(ports[0])+'/v1.0/healthz/outbound',timeout=1).close();break
        except Exception:
            if child.poll() is not None:raise RuntimeError('Owned Dapr exited')
            time.sleep(.2)
    else:raise RuntimeError('Owned Dapr not ready')
    env.update(STORY61_DAPR_HTTP_ENDPOINT='http://127.0.0.1:'+str(ports[0]),STORY61_DAPR_GRPC_ENDPOINT='http://127.0.0.1:'+str(ports[1]))
    print('READY FOR JSON JOBS',flush=True)
    for line in __import__('sys').stdin:
        job=json.loads(line)
        if job.get('quit'):break
        suffix=job.get('suite','Server');label=job['label'];name='Hexalith.Projects'+('' if suffix=='Domain' else '.'+suffix)+'.Tests'
        env['STORY67_AUTHENTICATED_OBSERVATIONS']=str(out/(label+'-observations.json'))
        env['STORY67_MATRIX_OBSERVATIONS']=str(out/(label+'-matrix.json'))
        if job.get('suite')=='Owner':
            name='Hexalith.Conversations.Server.Tests';cwd=root/'references/Hexalith.Conversations';artifact=scratch/'owner-work/artifacts'
            artifact.parent.mkdir(exist_ok=True)
            for entry in cwd.iterdir():
                p=artifact.parent/entry.name
                if not p.exists():p.symlink_to(entry,target_is_directory=entry.is_dir())
        else:cwd=root;artifact=scratch/'artifacts'
        if run(label+'-build',['dotnet','build','tests/'+name+'/'+name+'.csproj','--configuration','Debug','-m:1','--disable-build-servers','--artifacts-path',str(artifact)],cwd):continue
        cmd=['dotnet',str(artifact/'bin'/name/'debug'/(name+'.dll')),'-xml',str(out/(label+'.xml'))]
        if job.get('class'):cmd.extend(['-class',job['class']])
        if job.get('method'):cmd.extend(['-method',job['method']])
        run(label+'-tests',cmd,cwd)
        print('READY FOR JSON JOBS',flush=True)
finally:
    child.terminate()
    try:child.wait(timeout=15)
    except subprocess.TimeoutExpired:child.kill();child.wait()
    log.close();(out/'sidecar-cleanup.json').write_text(json.dumps({'ownedPid':child.pid,'reaped':child.poll() is not None,'productionAcceptance':False},indent=2)+'\n')
