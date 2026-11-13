"""Fixed receiver-only closed metadata validation, called after owned cleanup only.

No copying, deleting, window reads, waits, target signals, or assembly operations.
Missing/incomplete metadata fails the diagnostic without preventing restoration.
"""
import collections
import hashlib
import json
import os
import stat
from ops_common import PRIVATE, private_directory

STAGES = {'Whole', 'ComponentFile', 'ComponentPolicy', 'AclFile',
          'SettingsEnumerationSortDictionary', 'AuthenticationSerialization', 'ActorBoundary', 'ResultTail'}
ZERO_FIELDS = {'allocationMode', 'allocatedBytes', 'yielded', 'nullValue', 'authenticationAccepted',
               'nonAuthenticationDiscarded', 'sensitiveSegmentExcluded', 'authenticationOtherExcluded'}
ROW_FIELDS = {'role', 'runOrdinal', 'captureOrdinal', 'stage', 'elapsedTicks', 'frequency'} | ZERO_FIELDS
RECEIPT_FIELDS = {'diagnosticOnly', 'featureAcceptance', 'role', 'runOrdinal', 'admitted', 'disposedOrUnwound',
                  'activeAtDrainEnd', 'queuedRowsDropped', 'closedRowsDropped', 'capRowsDropped', 'ioFailures',
                  'writtenRows', 'writtenBytes', 'queuedAtDrainEnd', 'incompleteHardDrain', 'countersFinal',
                  'lateDisposedOrDropCountersMayIncrease', 'maximumRows', 'maximumBytes', 'queueCapacity',
                  'windowStartTicks', 'windowEndTicks', 'frequency'}


def closed_json(data):
    def pairs(items):
        value = {}
        for key, item in items:
            assert key not in value
            value[key] = item
        return value
    return json.loads(data, object_pairs_hook=pairs, parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))


def read_regular(directory, name, limit):
    descriptor = os.open(name, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=directory)
    try:
        before = os.fstat(descriptor)
        assert stat.S_ISREG(before.st_mode) and before.st_uid == 1000 and stat.S_IMODE(before.st_mode) == 0o600
        assert before.st_size <= limit
        chunks = []
        while True:
            chunk = os.read(descriptor, min(65536, limit + 1 - sum(map(len, chunks))))
            if not chunk:
                break
            chunks.append(chunk)
            assert sum(map(len, chunks)) <= limit
        after = os.fstat(descriptor)
        assert (before.st_dev, before.st_ino, before.st_size, before.st_mtime_ns) == (after.st_dev, after.st_ino, after.st_size, after.st_mtime_ns)
        return b''.join(chunks)
    finally:
        os.close(descriptor)


def validate(supervisor):
    assert supervisor['controlPublished'] is True
    proof = private_directory()
    descriptor = os.open(PRIVATE, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
    try:
        actual = os.fstat(descriptor)
        assert (actual.st_dev, actual.st_ino, actual.st_uid, stat.S_IMODE(actual.st_mode)) == (proof['device'], proof['inode'], proof['uid'], 0o700)
        reports = []
        for role in ['projects', 'tenants', 'folders']:
            raw = read_regular(descriptor, role + '.jsonl', 8 * 1024 * 1024)
            closed = read_regular(descriptor, role + '.closed.json', 4096)
            receipt = closed_json(closed)
            assert set(receipt) == RECEIPT_FIELDS and receipt['role'] == role
            assert all(type(value) is int and value >= 0 for key, value in receipt.items() if key != 'role')
            assert receipt['diagnosticOnly'] == 1 and receipt['featureAcceptance'] == 0
            assert receipt['runOrdinal'] == supervisor['runOrdinal'] and receipt['frequency'] == supervisor['frequency']
            assert receipt['windowStartTicks'] == supervisor['admissionStartTicks'] and receipt['windowEndTicks'] == supervisor['admissionEndTicks']
            assert receipt['maximumRows'] == 65536 and receipt['maximumBytes'] == 8388608 and receipt['queueCapacity'] == 4096
            assert receipt['countersFinal'] == 1 and all(receipt[key] == 0 for key in ['activeAtDrainEnd', 'queuedRowsDropped', 'closedRowsDropped', 'capRowsDropped', 'ioFailures', 'queuedAtDrainEnd', 'incompleteHardDrain', 'lateDisposedOrDropCountersMayIncrease'])
            assert raw.endswith(b'\n')
            groups = collections.defaultdict(dict)
            lines = raw.splitlines()
            assert len(lines) == receipt['writtenRows'] and len(raw) == receipt['writtenBytes']
            for line in lines:
                row = closed_json(line)
                assert set(row) == ROW_FIELDS and row['role'] == role and row['stage'] in STAGES
                assert all(type(value) is int and value >= 0 for key, value in row.items() if key not in ['role', 'stage'])
                assert row['runOrdinal'] == receipt['runOrdinal'] and row['frequency'] == receipt['frequency'] and row['captureOrdinal'] > 0
                assert all(row[key] == 0 for key in ZERO_FIELDS)
                assert row['stage'] not in groups[row['captureOrdinal']]
                groups[row['captureOrdinal']][row['stage']] = row['elapsedTicks']
            assert 0 < receipt['admitted'] <= receipt['writtenRows'] <= receipt['maximumRows']
            assert len(groups) == receipt['admitted'] == receipt['disposedOrUnwound']
            assert set(groups) == set(range(1, receipt['admitted'] + 1))
            assert all('Whole' in stages and all(elapsed <= stages['Whole'] for stage, elapsed in stages.items() if stage != 'Whole') for stages in groups.values())
            reports.append({'role': role, 'captures': len(groups), 'rows': len(lines),
                            'rowSha256': hashlib.sha256(raw).hexdigest(), 'closedReceiptSha256': hashlib.sha256(closed).hexdigest(),
                            'strictClosedSchemaFinalZeroDrops': True})
        return {'diagnosticOnly': True, 'featureAcceptance': False, 'complete': True, 'roles': reports,
                'afterAllOwnedProcessClosureOnly': True, 'noControlReadCopyOrDeletion': True,
                'stage5LastIntervalOnly': True, 'stage7Overlaps3And4And5': True,
                'noOutcomeOrCancellationInferenceFromMissingStages': True}
    finally:
        os.close(descriptor)
