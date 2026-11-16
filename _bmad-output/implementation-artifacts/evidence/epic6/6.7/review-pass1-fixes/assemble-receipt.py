"""Derive the activation/security packet from actual native XML and retained transport observations."""
import copy
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[6]
packet_dir = Path(__file__).resolve().parent
# Resolve from the script location without depending on the caller working directory.
while not (root / 'src/Hexalith.Projects.Client').is_dir():
    root = root.parent


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def native(name):
    path = packet_dir / name
    xml = ET.parse(path)
    tests = xml.findall('.//test')
    assemblies = xml.findall('.//assembly')
    assert assemblies and tests
    for assembly in assemblies:
        a = assembly.attrib
        assert int(a['total']) == int(a['passed']) == len(assembly.findall('.//test')) > 0
        assert all(a[k] == '0' for k in ('failed', 'skipped', 'not-run', 'errors'))
    assert all(test.attrib['result'] == 'Pass' for test in tests)
    return {'path': name, 'sha256': digest(path)}, {t.attrib['name'] for t in tests}


matrix_path = packet_dir / 'Server-full-2-matrix.json'
matrix = json.loads(matrix_path.read_text())
revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
contract = re.search(r'ContractSpineSha256 = "([a-f0-9]{64})"',
                     (root / 'src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs').read_text())[1]
main_receipt, main_names = native('Server-full-2.xml')
main_test = 'Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests.NativeConversationMembershipUsesTheRealGatewayOwnerAndPersistedNativeSource'
assert main_test in main_names
host_test = 'Hexalith.Projects.Server.Tests.Queries.ReadCutoverTests.EveryDeclaredSupportedQueryHasTheSameTypedResultInTheHost'
assert host_test in main_names
ui_receipt, ui_names = native('UI-full-2.xml')
client_receipt, client_names = native('Client-full-2.xml')
domain_receipt, domain_names = native('Domain-full-1.xml')
contracts_receipt, contracts_names = native('Contracts-full-1.xml')
parity_tests = ['Hexalith.Projects.Client.Tests.ClientGenerationTests.FingerprintVerifiesCurrentForCheckedInArtifacts',
                'Hexalith.Projects.Client.Tests.ReadClientCompatibilityTests.AdditiveMetadataMethodsHaveDefaultBodiesForExistingClientImplementers',
                'Hexalith.Projects.Client.Tests.ReadClientCompatibilityTests.ExistingConstructorsAndDeconstructorsRemainCallable']
assert set(parity_tests) <= client_names
identity_tests = sorted(n for n in domain_names if '.SchemaEvolution.ReadCutoverHistoryTests.' in n)
assert len(identity_tests) == 2 and any('opaque-historical' in n for n in identity_tests) and any('01HZ' in n for n in identity_tests)
assert 'Hexalith.Projects.Contracts.Tests.Identifiers.ProjectIdTests.ConstructorAcceptsValidUlidShapedValue' in contracts_names
operations = ['list', 'open', 'conversation-start', 'context', 'explain', 'resolve-conversation', 'resolve-attachments']
scenarios = ['complete', 'partial', 'unavailable', 'absent-denied', 'foreign-tenant', 'stale-authority',
             'historical-pending-folderless', 'cursor-scope-continuation-expiry', 'descriptive-audit', 'conversation-binding', 'no-domain-writes']
flags = ['executed', 'passed', 'canonicalOutput', 'independentKeys', 'exactPositions', 'cursorScope', 'isolation', 'supportedOnlyAssertions', 'noDomainWrites']
cells = matrix['cases']
assert len(cells) == 77
assert {(c['operation'], c['scenario']) for c in cells} == {(o, s) for o in operations for s in scenarios}
assert matrix['normalizationRevision'] == 'story67-read-normalization-2026-11-16-v3'
assert matrix['pendingAdvisoryCoverage']['passed'] and matrix['pendingAdvisoryCoverage']['noDomainWrites']
for cell in cells:
    assert cell['rawLegacyKeys'] and cell['rawSupportedKeys']
    assert cell['legacyAggregateIdentities'] == cell['supportedAggregateIdentities']
    assert cell['legacyCursorEvidence'] != cell['supportedCursorEvidence']
    if cell['scenario'] == 'absent-denied':
        assert cell['missingTargetProof'] and cell['missingTargetEvidence']['executed'] and cell['missingTargetEvidence']['passed']
        assert cell['missingTargetEvidence']['noDomainWrites'] and cell['missingTargetEvidence']['tenantAuthorityCurrent']
    assert all(cell[f] is True for f in flags) and cell['skipped'] is False
    assert not cell['unexpectedSdkWriteKeys'] and not cell['comparisonDifferences']
    assert cell['transportCorrelation'] and cell['causeSourceProof']
    assert cell['domainBytesAndEtagsBefore']
    if cell['operation'] == 'conversation-start' and cell['legacyStatus'] == 200:
        headers = cell['legacyConversationStartHeaders']
        assert headers['projectionWatermark'] == str(headers['expectedOriginalGlobalPosition'])
        assert headers['freshness'] == json.loads(cell['legacyRawResponse'])['freshness']

cases = [{**{k: c[k] for k in ['operation', 'scenario', 'skipped', *flags]},
          'nativeXmlHash': main_receipt['sha256'], 'nativeTestName': main_test,
          'diagnosticCell': c['operation'] + '/' + c['scenario']} for c in cells]
security = []
for operation in operations:
    rows = [c for c in cells if c['operation'] == operation]
    complete = next(c for c in rows if c['scenario'] == 'complete')
    denials = [c for c in rows if c['scenario'] in ('absent-denied', 'foreign-tenant')]
    assert complete['currentProjectAuthority']
    assert all(c['legacyStatus'] == 404 and c['supportedStatus'] == 404 and c['isolation'] for c in denials)
    security.append({'operation': operation, 'executed': True, 'passed': all(c['passed'] for c in rows),
                     'currentAuthority': complete['currentProjectAuthority'],
                     'safeMetadata': all(c['canonicalOutput'] and c['transportCorrelation'] for c in rows),
                     'safeDenial': all(c['legacyStatus'] == 404 and c['isolation'] for c in denials),
                     'noDomainWrites': all(c['noDomainWrites'] and not c['unexpectedSdkWriteKeys'] for c in rows),
                     'nativeXmlHash': main_receipt['sha256'], 'nativeTestName': main_test,
                     'diagnosticCells': [c['operation'] + '/' + c['scenario'] for c in rows]})
packet = {'normalizationRevision': matrix['normalizationRevision'], 'sourceRevision': revision,
          'sourceWorkingTree': 'dirty; exact source hashes retained in source-manifest.json', 'contractHash': contract,
          'sourceManifest': {'path': 'source-manifest.json', 'sha256': digest(packet_dir / 'source-manifest.json')},
          'contractParity': True, 'identityCompatibility': True, 'cases': cases, 'legacySecurity': security,
          'nativeReceipts': [main_receipt, client_receipt, domain_receipt, contracts_receipt, ui_receipt],
          'diagnostics': {'path': matrix_path.name, 'sha256': digest(matrix_path)},
          'contractParityProof': {'nativeXmlHash': client_receipt['sha256'], 'nativeTests': parity_tests,
                                  'hostNativeXmlHash': main_receipt['sha256'], 'hostNativeTest': host_test,
                                  'kind': 'source/project-reference Debug; no package publication claimed'},
          'identityCompatibilityProof': {'nativeXmlHash': domain_receipt['sha256'], 'nativeTests': identity_tests,
                                         'currentUlidNativeXmlHash': contracts_receipt['sha256']},
          'ruleApproval': {'path': '../../../../../planning-artifacts/story-6-7-approved-read-cutover-amendments-2026-11-16.md',
                          'decisions': ['D1', 'D2', 'D3', 'D4', 'D5', 'D6']},
          'releaseObligations': ['Intrinsic binding to running application/contract/dependency fingerprints before production adoption; current source gate trusts declared startup identity.', 'Publish/version corrected owner/Projects contracts and client artifacts before package-mode adoption.',
                                 'External Chatbot adoption requires separate owner/consumer execution evidence.'],
          'historicalEvidence': 'Earlier v1/v2 blocked packets and observations remain unchanged.'}


def write(name, value):
    path = packet_dir / name
    assert not path.exists(), f'Refusing to overwrite retained evidence: {path}'
    path.write_text(json.dumps(value, indent=2) + '\n')
    return path


accepted = write('read-cutover-final.json', packet)
regression = copy.deepcopy(packet)
regression['cases'][0]['passed'] = False
regressed = write('control-regression-final.json', regression)
missing = copy.deepcopy(packet)
missing['legacySecurity'].pop()
missing_path = write('control-incomplete-legacy-final.json', missing)
configuration = {'sourceRevision': revision, 'contractHash': contract, 'evidencePath': str(accepted), 'evidenceHash': digest(accepted),
                 'regressionPath': str(regressed), 'regressionHash': digest(regressed),
                 'missingLegacyPath': str(missing_path), 'missingLegacyHash': digest(missing_path),
                 'outputPath': str(packet_dir / 'rollback-final-proof.json')}
write('Rollback-full-2-matrix.routing-proof.json', configuration)
print(json.dumps({'packet': str(accepted), 'sha256': digest(accepted), 'cells': len(cases), 'legacySecuritySlices': len(security)}))
