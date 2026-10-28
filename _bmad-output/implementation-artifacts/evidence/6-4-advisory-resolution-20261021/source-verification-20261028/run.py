"""Capture exact source-verification commands without changing caller Git state."""
from pathlib import Path
import datetime
import hashlib
import json
import os
import subprocess
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path('/home/administrator/projects/hexalith/projects')
EVIDENCE = Path(__file__).resolve().parent
WORK = Path('/tmp/story64-source-verification-20261028')
SPEC = '_bmad-output/implementation-artifacts/spec-6-4-current-source-verification.md'
BUILD_ENVIRONMENT_KEYS = ['PATH','HOME','TMPDIR','TMP','TEMP','CI','GITHUB_ACTIONS',
    'DOTNET_ROOT','DOTNET_ROOT_X64','DOTNET_HOST_PATH','DOTNET_CLI_HOME',
    'DOTNET_ROLL_FORWARD','DOTNET_ROLL_FORWARD_TO_PRERELEASE','DOTNET_MULTILEVEL_LOOKUP',
    'DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR','DOTNET_MSBUILD_SDK_RESOLVER_SDKS_VER',
    'DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR','DOTNET_SKIP_FIRST_TIME_EXPERIENCE',
    'DOTNET_CLI_TELEMETRY_OPTOUT','NUGET_PACKAGES','NUGET_HTTP_CACHE_PATH',
    'NUGET_FALLBACK_PACKAGES','NUGET_CONFIG_FILE','MSBuildSDKsPath','MSBUILD_EXE_PATH',
    'MSBuildExtensionsPath','MSBuildEnableWorkloadResolver','MSBUILDDISABLENODEREUSE',
    'Configuration']


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(label, argv, cwd=ROOT, env=None, check=True):
    commands_file = EVIDENCE / 'commands.json'
    records = json.loads(commands_file.read_text()) if commands_file.exists() else []
    assert not any(r['label'] == label for r in records), label
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    before = time.monotonic()
    effective = os.environ.copy()
    effective['GIT_OPTIONAL_LOCKS'] = '0'
    effective.update(env or {})
    with (EVIDENCE / (label + '.stdout')).open('wb') as out, (EVIDENCE / (label + '.stderr')).open('wb') as err:
        result = subprocess.run(argv, cwd=cwd, env=effective, stdout=out, stderr=err)
    # A harness may itself record child commands. Read the ledger again after it ends.
    records = json.loads(commands_file.read_text()) if commands_file.exists() else []
    assert not any(r['label'] == label for r in records), label
    records.append(dict(label=label, argv=argv, cwd=str(cwd),
                        environment={'GIT_OPTIONAL_LOCKS': '0', **(env or {})},
                        effective_build_environment={k:effective.get(k) for k in BUILD_ENVIRONMENT_KEYS},
                        started_utc=started, seconds=round(time.monotonic()-before, 3),
                        exit_code=result.returncode,
                        stdout=label+'.stdout', stderr=label+'.stderr'))
    commands_file.write_text(json.dumps(records, indent=2)+'\n')
    print(label, result.returncode, flush=True)
    if check and result.returncode:
        raise RuntimeError(label + ': ' + (EVIDENCE/(label+'.stderr')).read_text(errors='replace')[-2000:])
    return (EVIDENCE/(label+'.stdout')).read_text(errors='replace')


def git(repo, *args):
    return subprocess.check_output(['git', *args], cwd=repo,
                                   env={**os.environ, 'GIT_OPTIONAL_LOCKS': '0'})


def snapshot():
    paths = [ROOT] + [ROOT/line.split(' ', 1)[1] for line in
             git(ROOT, 'config', '--file', '.gitmodules', '--get-regexp', r'^submodule\..*\.path$').decode().splitlines()]
    result = {}
    for repo in paths:
        status = git(repo, 'status', '--porcelain=v1', '-z', '--untracked-files=all', '--ignore-submodules=all')
        files = {}
        fields = status.split(b'\0')
        i = 0
        while i < len(fields):
            row = fields[i]
            i += 1
            if not row:
                continue
            name = os.fsdecode(row[3:])
            if row[:1] in (b'R', b'C'):
                i += 1
            p = repo/name
            if repo == ROOT and (p.is_relative_to(EVIDENCE) or name == SPEC):
                continue
            files[name] = digest(p) if p.is_file() else ('symlink:'+os.readlink(p) if p.is_symlink() else 'absent-or-directory')
        index = Path(os.fsdecode(git(repo, 'rev-parse', '--path-format=absolute', '--git-path', 'index')).strip())
        result[str(repo.relative_to(ROOT))] = dict(
            head=git(repo, 'rev-parse', 'HEAD').decode().strip(),
            branch=git(repo, 'symbolic-ref', '-q', 'HEAD').decode().strip(),
            refs_sha256=hashlib.sha256(git(repo, 'show-ref')).hexdigest(),
            index_sha256=digest(index),
            index_entries_sha256=hashlib.sha256(git(repo, 'ls-files', '--stage', '-z')).hexdigest(),
            changed_files=files,
            nested_markers=sorted(str(p.relative_to(repo)) for p in (repo/'references').glob('*/.git')) if repo != ROOT else [])
    return result


if __name__ == '__main__':
    import sys
    if sys.argv[1] == 'baseline':
        target = EVIDENCE/'preservation-before.json'
        assert not target.exists()
        target.write_text(json.dumps(snapshot(), indent=2)+'\n')
        print(target)
    elif sys.argv[1] == 'audit':
        before = json.loads((EVIDENCE/'preservation-before.json').read_text())
        after = snapshot()
        (EVIDENCE/'preservation-after.json').write_text(json.dumps(after, indent=2)+'\n')
        assert before == after, 'Original Git identities, indexes, refs or user files changed'
        print('PASS: original HEADs, refs, raw indexes, entries, existing changed/untracked files and nested markers unchanged')
