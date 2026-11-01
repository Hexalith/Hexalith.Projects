"""Bounded current-source verification; all public outputs are command-owned."""
from pathlib import Path
import datetime, hashlib, json, os, subprocess, tempfile
ROOT = Path('/home/administrator/projects/hexalith/projects')
EVIDENCE = Path(__file__).resolve().parent
WORK = Path(json.loads((EVIDENCE/'workspace.json').read_text())['work'])
BASE = {'GIT_OPTIONAL_LOCKS': '0', 'DOTNET_PROCESSOR_COUNT': '2', 'MSBUILDDISABLENODEREUSE': '1'}
RECEIPTS = []
def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def write(name, value):
    target = EVIDENCE / name
    with target.open('x', encoding='utf-8') as handle:
        json.dump(value, handle, indent=2); handle.write('\n')
def git(repo, *args):
    return subprocess.check_output(['git', *args], cwd=repo, env={**os.environ, 'GIT_OPTIONAL_LOCKS':'0'})
def state():
    result = {}
    for repo in [ROOT, *sorted((ROOT/'references').glob('Hexalith.*'))]:
        if not (repo/'.git').exists(): continue
        index = Path(git(repo, 'rev-parse', '--path-format=absolute', '--git-path', 'index').decode().strip())
        headfile = Path(git(repo, 'rev-parse', '--path-format=absolute', '--git-path', 'HEAD').decode().strip())
        result[str(repo.relative_to(ROOT))] = {
            'head':git(repo,'rev-parse','HEAD').decode().strip(),
            'tree':git(repo,'rev-parse','HEAD^{tree}').decode().strip(),
            'symbolic_head':headfile.read_text().strip(),
            'index_sha256':digest(index),
            'refs_sha256':hashlib.sha256(git(repo,'show-ref')).hexdigest(),
            'status':git(repo,'status','--porcelain=v1').decode(),
            'gitlinks':git(repo,'ls-files','--stage','--','references').decode()}
    return result
def sources():
    result = {}
    for repo in [ROOT, *sorted((ROOT/'references').glob('Hexalith.*'))]:
        if not (repo/'.git').exists(): continue
        names=git(repo,'ls-files','--cached','--others','--exclude-standard','-z','--','src','tests','test','tools','Tools','Props','docs','module','eng','*.props','*.targets','*.slnx','global.json','nuget.config','NuGet.Config','AGENTS.md','.editorconfig','.gitattributes','.gitmodules').decode().split('\0')
        for name in sorted(set(names)):
            path=repo/name
            if name and path.is_file() and not any(part in ('references','bin','obj','node_modules','.git','__pycache__') for part in Path(name).parts):
                if path.name == 'SKILL.md' or '.agents' in path.parts: continue
                result[str(path.relative_to(ROOT))]=digest(path)
    return result
def run(name, argv, cwd=ROOT, explicit=None, private=False, timeout=600):
    target=WORK if private else EVIDENCE
    stdout=target/(name+'.stdout.log'); stderr=target/(name+'.stderr.log')
    env={**os.environ, **BASE, **(explicit or {})}
    # CI is not a local source mode. Preserve the normal CI guards in source files.
    env.pop('CI',None)
    start=datetime.datetime.now(datetime.timezone.utc).isoformat()
    code=None; error=None
    with stdout.open('xb') as out, stderr.open('xb') as err:
        try:
            code=subprocess.run([str(a) for a in argv],cwd=cwd,env=env,stdout=out,stderr=err,timeout=timeout).returncode
        except (OSError,subprocess.TimeoutExpired) as exc:
            error=type(exc).__name__; code=124 if isinstance(exc,subprocess.TimeoutExpired) else 127
    receipt={'name':name,'argv':[str(a) for a in argv],'cwd':str(cwd),'explicit_environment':{**BASE,**(explicit or {})},'removed_environment':['CI'],'started_utc':start,'finished_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exit_code':code,'launch_or_timeout_error':error,'stdout':str(stdout),'stderr':str(stderr),'stdout_sha256':digest(stdout),'stderr_sha256':digest(stderr),'private_output':private}
    write(name+'.command.json',receipt); RECEIPTS.append(receipt)
    print(name+': exit '+str(code),flush=True)
    return code
