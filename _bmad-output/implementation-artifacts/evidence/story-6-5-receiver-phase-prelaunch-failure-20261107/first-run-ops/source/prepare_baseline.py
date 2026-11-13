"""Future read-only baseline capture, blocked until actual new receiver qualification is reviewed."""
from pathlib import Path
import argparse, json
from ops_common import *

parser = argparse.ArgumentParser()
parser.add_argument('--capture', action='store_true')
args = parser.parse_args()
if not args.capture:
    print(json.dumps({'preparationOnly': True, 'operation': 'current ordinary carrier/source hash census only',
        'newReceiverQualificationRequired': True, 'rootReviewRequired': True,
        'noBuildAssemblyLoadHostWindowOrPrivateDirectoryOperation': True}))
    raise SystemExit()
binding = json.loads((PREP / 'source-qualification-binding.json').read_text())
assert binding['sourceQualificationReady'] is True
for key in ['qualifiedReceipt', 'parentAudit', 'preparedReceipt', 'qualifiedProducerReceipt']:
    assert binding[key] and sha(binding[key]) == binding[key + 'Sha256']
qualified = json.loads(Path(binding['qualifiedReceipt']).read_text())
assert qualified['qualified'] is True and qualified['featureAcceptance'] is False
assert binding['expectedAggregateFields'] and all(qualified[key] == value for key, value in binding['expectedAggregateFields'].items())
assert qualified['coreSha256'] == PRIVATE_HANDLER_SHA and qualified['helperSha256'] == HELPER_SHA and qualified['bootstrapSha256'] == BOOTSTRAP_SHA
handoff = json.loads((PREP / 'ordinary-handoff.json').read_text())
assert handoff['qualified'] is True
for key in ['ownerReceipt', 'nativeAudit', 'sourceControls', 'nativeBinaryAudit']:
    value = handoff[key]
    assert value['path'] and sha(value['path']) == value['sha256']
controls = json.loads(Path(handoff['sourceControls']['path']).read_text())['sourceSha256']
assert len(controls) == 56 and all(sha(ROOT / relative) == digest for relative, digest in controls.items())
assert sha(ordinary_expected('full')) == handoff['sdkSha256'] == '6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
assert sha(ordinary_expected('pdb')) == handoff['sdkPdbSha256'] == 'ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
inventory = json.loads((PREP / 'affected-inventory.json').read_text())
assert inventory['baselineReady'] is False
inventory['projectInputsSha256'] = {relative: sha(ROOT / relative) for relative in [*inventory['projects'].values(), inventory['nativeProject']]}
owned = {*inventory['projects'].values(), inventory['nativeProject']}
inventory['allSdkBaseline'] = census()
assert all(not row['privateMarker'] for row in inventory['allSdkBaseline'])
inventory['knownSdkCopies'] = [row for row in inventory['allSdkBaseline'] if row['project'] in owned and '/legacy-sdk-' not in row['path']]
known = json.loads((PREP / 'runtime-copy-coordinates.json').read_text())
inventory['knownServiceDefaultsCopies'] = []
for relative in known['sdk8']:
    file = ROOT / relative
    assert sha(file) == handoff['sdkSha256'] and sha(file.with_suffix('.pdb')) == handoff['sdkPdbSha256']
    inventory['knownServiceDefaultsCopies'].append({'path': relative, 'sha256': sha(file), 'pdbSha256': sha(file.with_suffix('.pdb'))})
native = json.loads(Path(handoff['nativeBinaryAudit']['path']).read_text())
inventory['knownNative147AssemblyPaths'] = [row['path'] for row in native['files']]
assert len(inventory['knownNative147AssemblyPaths']) == 147
assert all(not marker(ROOT / relative) for relative in inventory['knownNative147AssemblyPaths'])
for name, key in [('Hexalith.EventStore.DomainService', 'ordinaryDomain98Baseline'),
                  ('Hexalith.EventStore.Server', 'allServerBaseline'),
                  ('Hexalith.EventStore.Gateway', 'allGatewayBaseline'),
                  ('Hexalith.Projects.Server', 'knownOrdinaryRootServerCopies')]:
    inventory[key] = census(name)
    assert all(not row['privateMarker'] for row in inventory[key])
inventory.update(baselineReady=True, source56Sha256=controls,
    baselineIsFreshObservedOrdinaryBytesNotInventedHistoricalGenerationEquality=True,
    previouslyUnresolved10DependencyPathsRetainedAsPriorLimitationNotRetrospectivelyQualified=True,
    preparedAfterActualNewReceiverQualification=True)
save(PREP / 'affected-inventory.json', inventory)
print(json.dumps({'baselineReady': True, 'sdkCopies': len(inventory['allSdkBaseline']),
    'knownSdkRuntimeCopies': len(inventory['knownServiceDefaultsCopies']), 'sourceCount': len(controls),
    'rootFinalPinsReviewStillRequired': True, 'buildsHostsWindowsOrPrivateDirectoryOperations': 0}))
