"""Read-only analysis, released only after Root's closed/restored handoff.

Reads closed scalar recorder files and existing metadata; never reads control,
loads assemblies, starts processes, signals, copies, deletes, or edits inputs.
"""
from pathlib import Path
import argparse
import collections
import hashlib
import json
import math
import os
import stat
import statistics

ROOT = Path('/home/administrator/projects/hexalith/projects')
OPS = Path('/tmp/story65-domain-receiver-phase-ops-prep-20261108')
PREP = Path('/tmp/story65-domain-receiver-phase-prep-20261108')
ROLES = ('projects', 'folders')
ALIASES = {
    'Whole': 'Whole',
    'ComponentFile': 'Guard',
    'ComponentPolicy': 'AuthorityAcquire',
    'AclFile': 'PostAcquireValidation',
    'SettingsEnumerationSortDictionary': 'Next',
    'AuthenticationSerialization': 'LastRelease',
    'ActorBoundary': 'ResultExecute',
    'ResultTail': 'FilterUntilReturn',
}
ZERO = {'allocationMode', 'allocatedBytes', 'yielded', 'nullValue', 'authenticationAccepted',
        'nonAuthenticationDiscarded', 'sensitiveSegmentExcluded', 'authenticationOtherExcluded'}
ROW_FIELDS = {'role', 'runOrdinal', 'captureOrdinal', 'stage', 'elapsedTicks', 'frequency'} | ZERO
RECEIPT_FIELDS = {'diagnosticOnly', 'featureAcceptance', 'role', 'runOrdinal', 'admitted',
                 'disposedOrUnwound', 'activeAtDrainEnd', 'queuedRowsDropped', 'closedRowsDropped',
                 'capRowsDropped', 'ioFailures', 'writtenRows', 'writtenBytes', 'queuedAtDrainEnd',
                 'incompleteHardDrain', 'countersFinal', 'lateDisposedOrDropCountersMayIncrease',
                 'maximumRows', 'maximumBytes', 'queueCapacity', 'windowStartTicks', 'windowEndTicks', 'frequency'}
FINAL_ZERO = ('activeAtDrainEnd', 'queuedRowsDropped', 'closedRowsDropped', 'capRowsDropped',
              'ioFailures', 'queuedAtDrainEnd', 'incompleteHardDrain', 'lateDisposedOrDropCountersMayIncrease')


def require(value, code):
    if not value:
        raise ValueError(code)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def read_file(path, maximum):
    """Bounded regular-file read: reject symlinks and concurrent modifications."""
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    try:
        before = os.fstat(fd)
        require(stat.S_ISREG(before.st_mode) and before.st_size <= maximum, 'regular-file-bound')
        chunks = []
        remaining = maximum + 1
        while remaining:
            chunk = os.read(fd, min(65536, remaining))
            if not chunk:
                break
            chunks.append(chunk)
            remaining -= len(chunk)
        data = b''.join(chunks)
        after = os.fstat(fd)
        require(len(data) <= maximum and len(data) == before.st_size, 'read-bound')
        require((before.st_dev, before.st_ino, before.st_size, before.st_mtime_ns, before.st_ctime_ns) ==
                (after.st_dev, after.st_ino, after.st_size, after.st_mtime_ns, after.st_ctime_ns), 'stable-file')
        return data, before
    finally:
        os.close(fd)


def closed_json(data):
    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 'duplicate-json-key')
            result[key] = value
        return result
    return json.loads(data, object_pairs_hook=pairs,
                      parse_constant=lambda _: (_ for _ in ()).throw(ValueError('nonfinite-json')))


def distribution(values):
    values = sorted(values)
    return {'n': len(values), 'median': statistics.median(values) if values else None,
            'p95': values[math.ceil(.95 * len(values)) - 1] if values else None,
            'minimum': values[0] if values else None, 'maximum': values[-1] if values else None}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--evidence', type=Path, default=Path('/tmp/story65-domain-receiver-phase-diagnostic-20261108'))
    parser.add_argument('--operational-pins-sha256', required=True)
    parser.add_argument('--recorder-directory', type=Path, required=True)
    parser.add_argument('--ordinary-receipt', type=Path, required=True)
    parser.add_argument('--ordinary-receipt-sha256', required=True)
    parser.add_argument('--parent-audit', type=Path)
    parser.add_argument('--parent-audit-sha256')
    parser.add_argument('--output-directory', type=Path, required=True)
    args = parser.parse_args()
    require(len(args.operational_pins_sha256) == 64 and all(c in '0123456789abcdef' for c in args.operational_pins_sha256), 'operational-pin-shape')
    provenance = {}

    def load(path, maximum=4 * 1024 * 1024):
        data, _ = read_file(path, maximum)
        provenance[str(path)] = sha(data)
        return closed_json(data)

    pins = load(OPS / 'operational-pins.json')
    require(provenance[str(OPS / 'operational-pins.json')] == args.operational_pins_sha256, 'exact-operational-generation')
    require(pins['executionReleased'] is True and pins['sourceQualificationReady'] is True, 'qualified-generation')
    status = load(OPS / 'latest-operational-result.json')
    require(status.get('privateAttempted') is True and status.get('privateExitCode') == 0 and
            status.get('nativeReceiverExitCode') == 0 and status.get('managedAttempted') is True and
            status.get('ordinaryRestoreAttempted') is True and status.get('ordinaryExitCode') == 0,
            'pipeline-closed-and-ordinary-restored')
    ordinary = load(args.ordinary_receipt)
    require(provenance[str(args.ordinary_receipt)] == args.ordinary_receipt_sha256, 'pinned-ordinary-receipt')
    require(ordinary.get('phase') == 'ordinary' and ordinary.get('completed') is True and
            ordinary.get('source59Match') is True and ordinary.get('allPrivateMarkersAbsent') is True,
            'closed-ordinary-receipt')
    cleanup = load(args.evidence / 'independent-cleanup-source-audit.json')
    require(all(cleanup.get(k) is True for k in ('allIndependentRecordedProcessAndContainerAbsences',
            'allOriginalCleanupAbsenceVerified', 'preexistingResourcesPreserved', 'privateMaterialRemoved',
            'namespaceContainersAbsent', 'privateRuntimeDirectoryAbsent', 'ownedSidecarsAbsent')) and
            cleanup.get('fullLiveManifestHashMismatches') == 0, 'closed-managed-cleanup')
    supplemental = load(args.evidence / 'supplemental-cleanup-qualification.json')
    require(supplemental.get('safeForOrdinaryRestoration') is True and
            supplemental.get('allEarlyIdentitiesAbsent') is True and
            supplemental.get('earlyRuntimeIdentityCount') == 12, 'closed-early-identities')
    entries = load(args.evidence / 'execution-entry-counts.json')
    for key in ('nativeInvocationEntries', 'browserInvocationEntries'):
        require(type(entries[key]) is int and entries[key] >= 0, 'closed-execution-counts')
    native_pointer = load(OPS / 'latest-native-attempt.json')
    native_path = Path(native_pointer['attemptDirectory'])
    require(native_path.parent == OPS, 'native-attempt-scope')
    native = load(native_path / 'receipt.json')
    require(native.get('passed') is True and native.get('caseCount') == 103 and
            native.get('nativeProcessAndDescendantsAbsent') is True, 'native103-closed')
    require(len(native['processes']) == 7 and all(p['processAndDescendantsAbsent'] is True for p in native['processes']),
            'native-seven-groups-closed')
    supervisor = load(args.evidence / 'supervisor.json')
    require(supervisor.get('phase') == 'managed-complete' and supervisor.get('controlPublished') is True,
            'closed-admission')
    validation = load(args.recorder_directory / 'partial-validation.json')
    require(validation.get('complete') is False and validation.get('availableRolesComplete') is True and validation.get('missingRoles') == ['tenants'] and validation.get('afterAllOwnedProcessClosureOnly') is True,
            'strict-closed-recorder-gate')
    expected = {row['role']: row for row in validation['roles']}
    require(len(validation['roles']) == 2 and set(expected) == set(ROLES), 'exact-two-available-role-validation')
    input_directory = args.recorder_directory.lstat()
    require(stat.S_ISDIR(input_directory.st_mode) and not stat.S_ISLNK(input_directory.st_mode), 'recorder-directory')
    role_reports = []
    for role in ROLES:
        raw, raw_stat = read_file(args.recorder_directory / (role + '.jsonl'), 8388608)
        closed, closed_stat = read_file(args.recorder_directory / (role + '.closed.json'), 4096)
        require(all(s.st_uid == 1000 and stat.S_IMODE(s.st_mode) == 0o600 for s in (raw_stat, closed_stat)),
                'private-recorder-file-modes')
        require(sha(raw) == expected[role]['rowSha256'] and sha(closed) == expected[role]['closedReceiptSha256'],
                'original-recorder-hash-chain')
        provenance[str(args.recorder_directory / (role + '.jsonl'))] = sha(raw)
        provenance[str(args.recorder_directory / (role + '.closed.json'))] = sha(closed)
        receipt = closed_json(closed)
        require(set(receipt) == RECEIPT_FIELDS and receipt['role'] == role, 'closed-receipt-schema')
        require(all(type(v) is int and v >= 0 for k, v in receipt.items() if k != 'role'), 'nonnegative-receipt')
        require(receipt['diagnosticOnly'] == 1 and receipt['featureAcceptance'] == 0 and receipt['countersFinal'] == 1 and
                all(receipt[k] == 0 for k in FINAL_ZERO), 'final-zero-drops-io-active-queued')
        require((receipt['maximumRows'], receipt['maximumBytes'], receipt['queueCapacity']) == (65536, 8388608, 4096),
                'qualified-recorder-limits')
        require(receipt['frequency'] == supervisor['frequency'] > 0 and receipt['runOrdinal'] == supervisor['runOrdinal'] and
                receipt['windowStartTicks'] == supervisor['admissionStartTicks'] and
                receipt['windowEndTicks'] == supervisor['admissionEndTicks'], 'same-operator-window')
        require(raw.endswith(b'\n'), 'complete-row-newline')
        lines = raw.splitlines()
        require(len(lines) == receipt['writtenRows'] == expected[role]['rows'] and len(raw) == receipt['writtenBytes'],
                'row-byte-reconciliation')
        require(0 < receipt['admitted'] <= receipt['writtenRows'] <= receipt['maximumRows'] and
                receipt['windowEndTicks'] - receipt['windowStartTicks'] == 8 * receipt['frequency'] and
                receipt['runOrdinal'] > 0, 'bounded-eight-second-admission')
        groups = collections.defaultdict(dict)
        for line in lines:
            row = closed_json(line)
            require(set(row) == ROW_FIELDS and row['role'] == role and row['stage'] in ALIASES, 'strict-scalar-row')
            require(all(type(v) is int and v >= 0 for k, v in row.items() if k not in ('role', 'stage')), 'nonnegative-row')
            require(all(row[k] == 0 for k in ZERO) and row['runOrdinal'] == receipt['runOrdinal'] and
                    row['frequency'] == receipt['frequency'] and row['captureOrdinal'] > 0, 'zero-count-allocation-row')
            group = groups[row['captureOrdinal']]
            require(row['stage'] not in group, 'one-final-value-per-stage')
            group[row['stage']] = row['elapsedTicks']
        require(0 < len(groups) == receipt['admitted'] == receipt['disposedOrUnwound'] == expected[role]['captures'] and
                set(groups) == set(range(1, receipt['admitted'] + 1)), 'exact-closed-capture-ordinals')
        require(all('Whole' in g and all(t <= g['Whole'] for t in g.values()) for g in groups.values()), 'stage-within-whole')
        frequency = receipt['frequency']
        phases = []
        for stage, alias in ALIASES.items():
            paired = [(g[stage], g['Whole']) for g in groups.values() if stage in g]
            phases.append({'stage': stage, 'phase': alias,
                           'wallMilliseconds': distribution([t * 1000 / frequency for t, _ in paired]),
                           'sameLocalCaptureFractionOfWhole': distribution([t / whole for t, whole in paired if whole > 0]),
                           'zeroWholeDenominatorsExcludedFromFractions': sum(whole == 0 for _, whole in paired)})
        filter_groups = [g for g in groups.values() if 'ResultTail' in g]
        # Same-local-capture interval complement, without assigning it a cause or outcome.
        outside_filter = distribution([(g['Whole'] - g['ResultTail']) * 1000 / frequency for g in filter_groups])
        role_reports.append({'role': role, 'captures': len(groups), 'rows': len(lines),
                             'phases': phases, 'wholeMinusFilterUntilReturnMillisecondsUnattributed': outside_filter,
                             'allStagesWithinWhole': True, 'strictFinalZeroDropsIoActiveQueued': True,
                             'rowSha256': sha(raw), 'closedReceiptSha256': sha(closed)})
    manifest = load(args.evidence / 'managed/source-manifest.json', 16 * 1024 * 1024)
    for relative, digest in manifest.items():
        file = (ROOT / relative).resolve()
        require(file.is_relative_to(ROOT.resolve()), 'source-containment')
        data, _ = read_file(file, 512 * 1024 * 1024)
        require(sha(data) == digest, 'live-source-manifest-current')
    require(len(manifest) == cleanup['fullLiveManifestSourceCount'], 'source-count-reconciliation')
    prepared = load(PREP / 'preparation-receipt.json')
    require(provenance[str(PREP / 'preparation-receipt.json')] ==
            pins['externalFiles'][str(PREP / 'preparation-receipt.json')], 'pinned-qualified-source-generation')
    require(len(prepared['sourceSha256']) == 59, 'source59-count')
    for relative, digest in prepared['sourceSha256'].items():
        file = (ROOT / relative).resolve()
        require(file.is_relative_to(ROOT.resolve()), 'source59-containment')
        data, _ = read_file(file, 64 * 1024 * 1024)
        require(sha(data) == digest, 'source59-current')
    readiness_raw, _ = read_file(args.evidence / 'managed/browser-advisory-seed-readiness.jsonl', 4 * 1024 * 1024)
    provenance[str(args.evidence / 'managed/browser-advisory-seed-readiness.jsonl')] = sha(readiness_raw)
    require(readiness_raw.endswith(b'\n'), 'completed-readiness-lines')
    readiness = [closed_json(line) for line in readiness_raw.splitlines()]
    outcomes = {}
    for multiple, label in ((False, 'SINGLE'), (True, 'MULTIPLE')):
        population = [r for r in readiness if r.get('multiple') is multiple]
        count = sum(r.get('positiveCandidatesVerified') is True for r in population)
        expected_prefix = 'multiple' if multiple else 'single'
        require(len(population) == cleanup[expected_prefix + 'Attempts'] and
                count == cleanup[expected_prefix + 'VerifiedPositives'], 'actual-outcome-reconciliation')
        outcomes[label] = {'completedAttempts': len(population), 'verifiedPositives': count}
    if args.parent_audit:
        require(args.parent_audit_sha256 is not None, 'parent-audit-pin-required')
        load(args.parent_audit)
        require(provenance[str(args.parent_audit)] == args.parent_audit_sha256, 'parent-audit-pin')
    report = {
        'diagnosticOnly': True, 'featureAcceptance': False,
        'operationalPinsSha256': args.operational_pins_sha256, 'roles': role_reports,
        'coverage': {'complete': False, 'availableRolesComplete': True, 'missingRoles': ['tenants'], 'missingRoleReason': 'unobserved; no request, outcome, or cause inferred'},
        'outcomes': {**outcomes, 'browserInvocations': entries['browserInvocationEntries'],
                     'browserOutcome': 'not-executed' if entries['browserInvocationEntries'] == 0 else 'requires-independent-browser-artifact-review',
                     'matrixCompleted': cleanup.get('matrixCompleted'), 'managedRunResult': cleanup.get('runResult')},
        'sourcePreservation': {'source59Current': True, 'fullLiveManifestCount': len(manifest), 'fullLiveManifestMismatches': 0},
        'separateQualifications': {'ownedCleanupAndOrdinaryRestorationClosedBeforeAnalysis': True,
                                  'ordinaryReceiptSha256': args.ordinary_receipt_sha256,
                                  'ordinaryBinaryGenerationsNotReauditedHere': True,
                                  'priorOrdinaryNative1048NotRerunOrInflatedByThisAnalysis': True,
                                  'actualPrivateNative103SeparateDiagnosticGate': True,
                                  'matrixAndFrozenControlQualificationNotInferredFromScalarTimings': True,
                                  'optionalParentAuditIsPinnedReferenceOnly': bool(args.parent_audit)},
        'conventions': {'percentile': 'nearest-rank ceil(0.95*n), median midpoint for even n',
                        'fractions': 'per capture having that stage, paired only to its own local Whole; no service or request joins',
                        'missingStage': 'no outcome, route, denial, expiry, or cancellation inference',
                        'stage5': 'last recorded release interval only, may include implicit unwind glue; not aggregate release work, release count, or callback count',
                        'stage7': 'FilterUntilReturn overlaps Guard, AuthorityAcquire, PostAcquireValidation, Next and in-filter LastRelease when present; it excludes later IResult execution; stage durations are not an additive partition',
                        'guard': 'Guard includes AuthorityAcquire and PostAcquireValidation when present; no subtraction or additive residual assigned to those intervals',
                        'nested': 'Folders may have two distinct nested filters and overlapping local capture lifetimes; capture ordinals are not request counts or a cross-capture join',
                        'residual': 'Whole minus FilterUntilReturn is an unattributed same-local-capture interval complement only; it is not assigned to a handoff, response, disposal, cancellation, or other cause; no other residual computed',
                        'whole': 'excludes entry-name eligibility and initial admission lock wait; includes post-timestamp admission/allocation/bookkeeping and may span deferred IResult handoff, execution and owned release; stops before queueing',
                        'population': 'all admitted DomainRouteCatalogFilter invocations in the two available processes during one failed-MULTI-triggered window; endpoint, request, command/query kind and outcome are unrecorded',
                        'limits': 'no request/root/Activity/trace joins, prior probe pairing, missing-stage outcome or cancellation inference, cause assertion, overhead subtraction, or ordinary host/deadline gain claim'},
        'inputSha256': provenance,
    }
    args.output_directory.mkdir(mode=0o700, parents=False, exist_ok=False)
    target = args.output_directory / 'independent-domain-receiver-phase-audit.json'
    encoded = (json.dumps(report, indent=2, allow_nan=False) + '\n').encode()
    fd = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'wb') as output:
        output.write(encoded)
    print(json.dumps({'reportSha256': sha(encoded), 'roles': len(role_reports),
                      'captures': sum(r['captures'] for r in role_reports), 'rows': sum(r['rows'] for r in role_reports)}))


if __name__ == '__main__':
    main()
