"""Archive validated numeric receiver records only after owned closure/restoration."""
import hashlib
import json
import os
import stat
import sys
from pathlib import Path

OPS = Path('/tmp/story65-domain-receiver-phase-ops-prep-20261108')
EVIDENCE = Path('/tmp/story65-domain-receiver-phase-diagnostic-20261108')
OUT = Path('/tmp/story65-domain-receiver-phase-closed-recorders-20261108')
sys.dont_write_bytecode = True
sys.path.insert(0, str(OPS))
sys.path.insert(0, '/tmp/story65-domain-receiver-phase-partial-analysis-prep-20261108')
from ops_common import PRIVATE, private_directory, sha
from validate_partial import validate

parent = Path(sys.argv[1])
assert sha(parent) == sys.argv[2]
audit = json.loads(parent.read_text())
assert audit['passed'] and audit['allNormalOwnedCleanupOriginalFlagsPass']
assert audit['allTwelveEarlyIdentitiesFreshlyAbsent']
status = json.loads((OPS/'latest-operational-result.json').read_text())
assert status['ordinaryExitCode'] == 0 and status['measurementComplete'] is False
supervisor = json.loads((EVIDENCE/'supervisor.json').read_text())
assert supervisor['phase'] == 'managed-complete'
validated = validate(supervisor)
assert validated['complete'] is False and validated['availableRolesComplete'] and validated['missingRoles'] == ['tenants'] and len(validated['roles']) == 2
private_directory()
descriptor = os.open(PRIVATE, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
try:
    current = os.fstat(descriptor)
    assert (current.st_dev,current.st_ino,current.st_uid,stat.S_IMODE(current.st_mode)) == (2096,8752986,1000,0o700)
    names = {'window.control'} | {r+s for r in ['projects','folders'] for s in ['.jsonl','.closed.json']}
    assert set(os.listdir(descriptor)) == names
    records, tokens = {}, {}
    for name in names:
        fd = os.open(name, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=descriptor)
        try:
            before = os.fstat(fd)
            maximum = 8388608 if name.endswith('.jsonl') else 4096
            assert stat.S_ISREG(before.st_mode) and before.st_uid == 1000 and stat.S_IMODE(before.st_mode) == 0o600 and before.st_size <= maximum
            with os.fdopen(fd, 'rb', closefd=False) as stream:
                data = stream.read(maximum + 1)
            after = os.fstat(fd)
            assert len(data) == before.st_size and (before.st_dev,before.st_ino,before.st_size,before.st_mtime_ns) == (after.st_dev,after.st_ino,after.st_size,after.st_mtime_ns)
            records[name] = data
            tokens[name] = (after.st_dev,after.st_ino,after.st_size,after.st_mtime_ns)
        finally:
            os.close(fd)
    assert records['window.control'] == f"{supervisor['runOrdinal']}\n{supervisor['frequency']}\n{supervisor['admissionStartTicks']}\n{supervisor['admissionEndTicks']}\n".encode()
    digest = lambda data: hashlib.sha256(data).hexdigest()
    for role in validated['roles']:
        assert digest(records[role['role']+'.jsonl']) == role['rowSha256']
        assert digest(records[role['role']+'.closed.json']) == role['closedReceiptSha256']
    OUT.mkdir(mode=0o700)
    for name,data in records.items():
        if name == 'window.control':
            continue
        fd = os.open(OUT/name, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
        with os.fdopen(fd,'wb') as stream:
            stream.write(data)
        assert sha(OUT/name) == digest(data)
    for name in names:
        value = os.stat(name, dir_fd=descriptor, follow_symlinks=False)
        assert (value.st_dev,value.st_ino,value.st_size,value.st_mtime_ns) == tokens[name]
        os.unlink(name, dir_fd=descriptor)
    assert not os.listdir(descriptor)
    receipt = {'diagnosticOnly':True,'featureAcceptance':False,'parentAuditSha256':sys.argv[2],
        'validated':validated,'filesSha256':{n:digest(d) for n,d in records.items()},
        'controlContentsArchived':False,'onlyFiveValidatedOwnedFilesRemoved':True,'fixedPrivateDirectoryEmpty':True}
    (OUT/'archive-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
    print(json.dumps({'archived':True,'directory':str(OUT),'receiptSha256':sha(OUT/'archive-receipt.json')}))
finally:
    os.close(descriptor)
