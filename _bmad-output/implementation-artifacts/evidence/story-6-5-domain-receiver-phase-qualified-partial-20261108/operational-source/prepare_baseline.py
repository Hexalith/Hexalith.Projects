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
assert qualified['coreSha256'] == CORE_SHA and qualified['helperSha256'] == HELPER_SHA and qualified['bootstrapSha256'] == BOOTSTRAP_SHA
handoff = json.loads((PREP / 'ordinary-handoff.json').read_text())
assert handoff['qualified'] is True
for key in ['ownerReceipt', 'nativeAudit', 'sourceControls', 'nativeBinaryAudit']:
    value = handoff[key]
    assert value['path'] and sha(value['path']) == value['sha256']
controls = json.loads((PRODUCER_PREP / 'source-core-binding.json').read_text())['sourceSha256']
assert len(controls) == 59 and all(sha(ROOT / relative) == digest for relative, digest in controls.items())
assert sha(module_expected(SDK, 'full')) == SDK_SHA and sha(module_expected(SDK, 'pdb')) == SDK_PDB_SHA
inventory = json.loads((PREP / 'affected-inventory.json').read_text())
assert inventory['baselineReady'] is False
inventory['projectInputsSha256'] = {relative: sha(ROOT / relative) for relative in [*inventory['projects'].values(), inventory['nativeProject'], inventory['sdkProject']]}
owned = {*inventory['projects'].values(), inventory['nativeProject']}
inventory['allDomainBaseline'] = census()
assert all(not row['privateMarker'] for row in inventory['allDomainBaseline'])
inventory['knownDomainCopies'] = [row for row in inventory['allDomainBaseline'] if row['project'] in owned]
inventory['allSdkBaseline'] = census(SDK)
assert all(not row['privateMarker'] for row in inventory['allSdkBaseline'])
assert len(inventory['allSdkBaseline']) == 137
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
for name, key in [('Hexalith.EventStore.Server', 'allServerBaseline'),
                  ('Hexalith.EventStore.Gateway', 'allGatewayBaseline'),
                  ('Hexalith.Projects.Server', 'knownOrdinaryRootServerCopies')]:
    inventory[key] = census(name)
    assert all(not row['privateMarker'] for row in inventory[key])
inventory.update(baselineReady=True, source59Sha256=controls,
    baselineIsFreshObservedOrdinaryBytesNotInventedHistoricalGenerationEquality=True,
    previouslyUnresolved10DependencyPathsRetainedAsPriorLimitationNotRetrospectivelyQualified=True,
    preparedAfterActualNewReceiverQualification=True)
save(PREP / 'affected-inventory.json', inventory)
print(json.dumps({'baselineReady': True, 'domainCopies':len(inventory['allDomainBaseline']), 'sdkCopies': len(inventory['allSdkBaseline']),
    'knownSdkRuntimeCopies': len(inventory['knownServiceDefaultsCopies']), 'sourceCount': len(controls),
    'rootFinalPinsReviewStillRequired': True, 'buildsHostsWindowsOrPrivateDirectoryOperations': 0}))
