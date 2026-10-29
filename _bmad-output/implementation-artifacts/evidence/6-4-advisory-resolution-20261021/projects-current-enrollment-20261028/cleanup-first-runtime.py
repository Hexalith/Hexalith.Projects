"""Remove only the failed container created by the initial owned fixture attempt."""
from run import *
report = json.loads((EVIDENCE / 'current-runtime-runtime-lane.json').read_text())
name = report['app_id']
output = run('initial-failed-container-probe', ['docker', 'ps', '--all', '--no-trunc', '--filter', 'name=^/' + name + '$', '--format', '{{.ID}} {{.State}}'], WORK / 'projects')
rows = output.splitlines()
assert len(rows) <= 1
if rows:
    identifier, state = rows[0].split()
    assert state in ['created', 'exited', 'dead'], 'Refuse to remove an unexpectedly running resource'
    run('initial-failed-container-remove', ['docker', 'rm', identifier], WORK / 'projects')
output = run('initial-failed-container-absent', ['docker', 'ps', '--all', '--no-trunc', '--filter', 'name=^/' + name + '$', '--format', '{{.ID}}'], WORK / 'projects')
assert not output.strip()
run('local-redis-server-probe', ['sh', '-c', 'command -v redis-server'], WORK / 'projects', check=False)
