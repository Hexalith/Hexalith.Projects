import argparse
import collections
import hashlib
import json
import pathlib
import statistics

parser = argparse.ArgumentParser()
parser.add_argument('input_directory')
parser.add_argument('output')
args = parser.parse_args()
base = pathlib.Path(args.input_directory)
zero = {'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
fields = zero | {'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
labels = ['Whole','ComponentFile','ComponentPolicy']
aliases = ['WholePrimaryHandler','ExactLeasedTransportAwait','OwnedLeaseDisposeAwait']
closed_path = base / 'gateway.closed.json'
raw_path = base / 'gateway.jsonl'
closed = json.loads(closed_path.read_bytes())
assert closed['diagnosticOnly'] == 1 and closed['featureAcceptance'] == 0
assert closed['role'] == 'gateway' and closed['admitted'] == closed['disposedOrUnwound']
assert closed['countersFinal'] == 1 and all(closed[k] == 0 for k in ['activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease'])
raw = raw_path.read_bytes()
assert len(raw) == closed['writtenBytes']
captures = collections.defaultdict(dict)
by_stage = collections.defaultdict(list)
for line in raw.splitlines():
    pairs = json.loads(line, object_pairs_hook=lambda pairs: pairs)
    row = dict(pairs)
    assert len(pairs) == len(row) and set(row) == fields
    assert row['role'] == 'gateway' and row['stage'] in labels
    assert all(type(row[k]) is int for k in fields - {'role','stage'}) and all(row[k] == 0 for k in zero)
    assert row['runOrdinal'] == closed['runOrdinal'] and row['frequency'] == closed['frequency']
    assert row['elapsedTicks'] >= 0 and row['captureOrdinal'] > 0
    stage = row['stage']
    capture = captures[row['captureOrdinal']]
    assert stage not in capture
    ms = row['elapsedTicks'] * 1000 / row['frequency']
    capture[stage] = ms
    by_stage[stage].append(ms)
assert sum(map(len, captures.values())) == closed['writtenRows']
assert len(captures) == closed['admitted']
stats = lambda values: dict(count=len(values), median=statistics.median(values), minimum=min(values), maximum=max(values))
summary = dict(diagnosticOnly=True, featureAcceptance=False, originalRawSha256=hashlib.sha256(raw).hexdigest(), captures=len(captures), rows=closed['writtenRows'], wholeExcludesEligibilityAndAdmission=True, transportExcludesOuterHttpClientResponseContentReadBuffering=True, populationIsEveryEntryIntoSelectedSharedPrimaryIncludingOrdinaryControlStateAndPotentialIssuer=True, fixedGatewayRoleOnlyFreshEventStoreEntryNameInSelectedSixRuntimeComposition=True, noClientControllerInvokerRootOrPriorRunPairing=True, noMissingStageOutcomeInference=True, residualIsUnattributed=True, ownedDisposeStageDoesNotProveReleaseCallbackOrFenceRemoval=True, possibleRecursiveWholeCapturesOverlap=True, noNetworkReceiverOrCpuAttribution=True)
summary['millisecondsByStage'] = {aliases[i]:stats(by_stage[label]) for i,label in enumerate(labels) if by_stage[label]}
summary['stageSetCounts'] = [{'stages':[aliases[labels.index(k)] for k in labels if k in stages], 'captures':count} for stages,count in collections.Counter(tuple(k for k in labels if k in c) for c in captures.values()).items()]
summary['withinSameCapturePercentOfWhole'] = {}
for label, alias in zip(labels[1:], aliases[1:]):
    ratios = [c[label] * 100 / c['Whole'] for c in captures.values() if label in c and c.get('Whole',0) > 0]
    if ratios:
        summary['withinSameCapturePercentOfWhole'][alias] = stats(ratios)
pathlib.Path(args.output).write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(summary,indent=2))
