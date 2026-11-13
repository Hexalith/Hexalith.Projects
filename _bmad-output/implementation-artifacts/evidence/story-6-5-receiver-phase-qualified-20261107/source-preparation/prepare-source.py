from pathlib import Path
from collections import Counter
import difflib
import hashlib
import json

PREP = Path(__file__).parent
SOURCE = Path('/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/TechnicalHttpRouteCatalogReceiver.cs')
HELPER = Path('/tmp/story65-invocation-transport-prep-20261107/CaptureCounters.cs')
original = SOURCE.read_bytes()
assert (PREP / 'TechnicalHttpRouteCatalogReceiver.original.cs.txt').read_bytes() == original
text = original.decode()
changes = []

def replace(old, new):
    global text
    assert text.count(old) == 1, old
    text = text.replace(old, new)
    changes.append({'original': old, 'private': new, 'occurrences': 1})

replace('fence.RequestFingerprint(endpoint, out description) != captured',
        'fence.RequestFingerprint(endpoint, out description, counters) != captured')
replace('await fence.InvokeNativeAsync(context, next, endpoint, native, callback).ConfigureAwait(false); return;',
        'await fence.InvokeNativeWithTimingAsync(context, next, endpoint, native, callback, counters).ConfigureAwait(false); return;')
replace('    private async Task InvokeNativeAsync(HttpContext context, RequestDelegate next, RouteEndpoint endpoint,\n'
        '        RouteCatalogNativeActorTransport? native, RouteCatalogDaprCallbackTransport? callback)\n',
        '    private Task InvokeNativeAsync(HttpContext context, RequestDelegate next, RouteEndpoint endpoint,\n'
        '        RouteCatalogNativeActorTransport? native, RouteCatalogDaprCallbackTransport? callback)\n'
        '        => InvokeNativeWithTimingAsync(context, next, endpoint, native, callback, null);\n\n'
        '    private async Task InvokeNativeWithTimingAsync(HttpContext context, RequestDelegate next, RouteEndpoint endpoint,\n'
        '        RouteCatalogNativeActorTransport? native, RouteCatalogDaprCallbackTransport? callback, CaptureCounters? counters)\n')
replace('    private string? RequestFingerprint(RouteEndpoint endpoint, out RouteCatalogTechnicalHttpEndpoint? description)\n',
        '    private string? RequestFingerprint(RouteEndpoint endpoint, out RouteCatalogTechnicalHttpEndpoint? description, CaptureCounters? counters = null)\n')

def insert(old, new):
    global text
    assert text.count(old) == 1, old
    text = text.replace(old, new)

insert('using Hexalith.EventStore.ServiceDefaults.Diagnostics;\n',
       'using Hexalith.EventStore.ServiceDefaults.Diagnostics;\nusing Hexalith.PrivateCaptureCounter;\n')
insert('        app.Use(async (context, next) =>\n        {\n',
       '        app.Use(async (context, next) =>\n        {\n            using var counters = ReceiverPhaseBootstrap.Begin();\n')
insert('            if (context.GetEndpoint() is RouteEndpoint nativeEndpoint)\n            {\n',
       '            if (context.GetEndpoint() is RouteEndpoint nativeEndpoint)\n            {\n                using var precheckPhase = counters?.Stage(Stage.ComponentFile, AllocationMode.None);\n')
insert('                // These real sidecar handshake/discovery protocols must work before catalog activation.\n',
       '                precheckPhase?.Dispose();\n                // These real sidecar handshake/discovery protocols must work before catalog activation.\n')
insert('            if (native is not null || callback is not null)\n            {\n',
       '            if (native is not null || callback is not null)\n            {\n                using var invokeNativePhase = counters?.Stage(Stage.ResultTail, AllocationMode.None);\n')
insert('            if (captured.StartsWith("technical:", StringComparison.Ordinal))\n            {\n',
       '            if (captured.StartsWith("technical:", StringComparison.Ordinal))\n            {\n                using var authenticationPhase = counters?.Stage(Stage.ActorBoundary, AllocationMode.None);\n')

for stage in ['ReceiverAcquire', 'NativeAcquire']:
    marker = 'using (var stage = diagnostics?.Start(DispatchStageName.' + stage + '))\n'
    index = text.index(marker)
    start = text.index('{\n', index) + 2
    indent = text[start:text.index('lease = await', start)]
    text = text[:start] + indent + 'using var acquirePhase = counters?.Stage(Stage.AclFile, AllocationMode.None);\n' + text[start:]
    line = 'lease = await context.RequestServices.GetRequiredService<IRouteCatalogAuthority>().AcquireAsync(context.RequestAborted).ConfigureAwait(false);\n'
    insert(line + indent + 'stage?.Complete();', line + indent + 'acquirePhase?.Dispose();\n' + indent + 'stage?.Complete();')

lines = []
release_sites = next_sites = 0
for line in text.splitlines(True):
    value = line.strip()
    indent = line[:len(line) - len(line.lstrip())]
    if value in ['await lease.DisposeAsync().ConfigureAwait(false);', 'await lease!.DisposeAsync().ConfigureAwait(false);']:
        lines.extend([indent + 'using var releasePhase = counters?.Stage(Stage.AuthenticationSerialization, AllocationMode.None);\n',
                      line, indent + 'releasePhase?.Dispose();\n'])
        release_sites += 1
    elif value == 'await next(context).ConfigureAwait(false);' and len(indent) == 20:
        lines.extend([indent + 'using var nextPhase = counters?.Stage(Stage.SettingsEnumerationSortDictionary, AllocationMode.None);\n',
                      line, indent + 'nextPhase?.Dispose();\n'])
        next_sites += 1
    else:
        lines.append(line)
text = ''.join(lines)
assert release_sites == 5 and next_sites == 2
signature = '    private string? RequestFingerprint(RouteEndpoint endpoint, out RouteCatalogTechnicalHttpEndpoint? description, CaptureCounters? counters = null)\n    {\n'
insert(signature, signature + '        using var fingerprintPhase = counters?.Stage(Stage.ComponentPolicy, AllocationMode.None);\n')

normalized = text
for change in reversed(changes):
    assert normalized.count(change['private']) == 1
    normalized = normalized.replace(change['private'], change['original'])
added = []
for line in difflib.ndiff(original.decode().splitlines(True), normalized.splitlines(True)):
    if line.startswith('+ '): added.append(line[2:])
    elif line.startswith('- '): raise AssertionError('original line changed outside four documented suffixes')
for line, count in Counter(added).items():
    assert normalized.count(line) == count
    normalized = normalized.replace(line, '')
assert normalized.encode() == original
assert 'counters?.Count(' not in text and 'AllocationMode.SynchronousThread' not in text
helper = HELPER.read_bytes()
assert hashlib.sha256(helper).hexdigest() == '112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
private = PREP / 'TechnicalHttpRouteCatalogReceiver.private.cs'
assert hashlib.sha256(private.read_bytes()).hexdigest() == '53cc005392503cc35fe892197b73b565688fd852c7039d960fd4798de8f63d4c'
private.write_text(text)
(PREP / 'CaptureCounters.cs').write_bytes(helper)
(PREP / 'source.patch').write_text(''.join(difflib.unified_diff(original.decode().splitlines(True), text.splitlines(True),
    fromfile='original/TechnicalHttpRouteCatalogReceiver.cs', tofile='private/TechnicalHttpRouteCatalogReceiver.cs')))
proof = {'filePreparationOnly': True, 'originalPath': str(SOURCE), 'originalSha256': hashlib.sha256(original).hexdigest(),
         'privateSha256': hashlib.sha256(private.read_bytes()).hexdigest(), 'helperOriginalPath': str(HELPER),
         'helperSha256': hashlib.sha256(helper).hexdigest(), 'sameClassPrivateSignatureCallRenameAndTaskForwardingChanges': changes,
         'insertedLineMultiset': [{'line': line, 'occurrences': count} for line, count in Counter(added).items()],
         'insertedLineCount': len(added), 'ownedDisposeSites': release_sites, 'leasedNextSites': next_sites,
         'normalizationByteExact': True, 'originalStatementsExpressionsTokensCatchAndDisposalOrderPreserved': True,
         'stage5RepeatedIntervalsAreLastWinsNotSum': True, 'originalUniqueFiveArgumentInvokeNativeAsyncTaskForwarderPreserved': True, 'builds': 0, 'loads': 0, 'runs': 0}
(PREP / 'source-normalization.json').write_text(json.dumps(proof, indent=2) + '\n')
print(json.dumps({key: proof[key] for key in ['originalSha256', 'privateSha256', 'insertedLineCount', 'ownedDisposeSites', 'leasedNextSites', 'normalizationByteExact']}, indent=2))
