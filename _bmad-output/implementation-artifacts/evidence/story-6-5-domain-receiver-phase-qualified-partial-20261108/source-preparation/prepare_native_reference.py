"""Static source/XML derivation only. No assembly loads, tests, builds or hosts."""
from pathlib import Path
import collections
import hashlib
import json
import re
import shutil
import xml.etree.ElementTree as ET

ROOT = Path('/home/administrator/projects/hexalith/projects')
PREP = Path('/tmp/story65-domain-receiver-phase-prep-20261108')
PRIMARY = Path('/tmp/story65-receiver-phase-ops-prep-20261107')
NAMESPACE = 'Hexalith.EventStore.Server.Tests.Routing.'
SELECTIONS = [
    ('DevelopmentDomainRouteCatalogTests', None, 14),
    ('DomainSidecarCatalogLeaseTests', None, 12),
    ('CatalogReceiverScopeTests', None, 41),
    ('CatalogReceiverStateBoundaryTests', None, 16),
    ('CatalogReceiverVersionTests', None, 7),
    ('TechnicalHttpDomainPeerTests', None, 12),
    ('HeldMetadataDispatchTests', 'RealDomainFilterAndResultComposeWithSdkMetadataClient', 1),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def digest(value):
    return hashlib.sha256(value.encode()).hexdigest()


def store(name, value):
    (PREP / name).write_text(json.dumps(value, indent=2) + '\n')


def argument(value):
    # Pinned ArgumentFormatter.Format handles null before type dispatch.
    if value is None:
        return 'null'
    if type(value) is bool:
        return 'True' if value else 'False'
    if type(value) is int:
        return str(value)
    assert type(value) is str and all(32 <= ord(c) <= 126 for c in value)
    assert len(value) <= 50 and '\\' not in value and '"' not in value
    return '"' + value + '"'


def reporter_escape(value):
    assert all(32 <= ord(c) <= 126 for c in value)
    return value.replace('\\', '\\\\').replace('"', '\\"')


def source_cases(class_name, selected_method, expected_count):
    relative = f'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Routing/{class_name}.cs'
    text = (ROOT / relative).read_text()
    assert f'namespace {NAMESPACE[:-1]};' in text
    pattern = (r'((?:\s*\[(?:Fact|Theory|InlineData)[^\n]*\]\s*)+)public\s+(?:async\s+)?'
               r'(?:Task(?:<[^>]+>)?|void|ValueTask(?:<[^>]+>)?)\s+(\w+)\s*\(([^)]*)\)')
    matches = list(re.finditer(pattern, text))
    assert len(matches) == len(re.findall(r'^\s*\[(?:Fact|Theory)(?:\]|\()', text, re.M))
    identities = collections.Counter()
    cases = []
    methods = []
    for match in matches:
        attributes, method, parameters = match.groups()
        if selected_method and method != selected_method:
            continue
        names = [part.strip().split()[-1] for part in parameters.split(',')] if parameters.strip() else []
        assert 'Skip' not in attributes and 'DisplayName' not in attributes
        assert ('[Fact]' in attributes) != ('[Theory]' in attributes)
        data = re.findall(r'\[InlineData\((.*)\)\]', attributes)
        values = [json.loads('[' + row + ']') for row in data] if data else [[]]
        assert bool(data) == ('[Theory]' in attributes)
        for row in values:
            assert len(row) == len(names)
            formatted = NAMESPACE + class_name + '.' + method
            if row:
                formatted += '(' + ', '.join(name + ': ' + argument(v) for name, v in zip(names, row)) + ')'
            parsed_name = reporter_escape(formatted)
            key = (NAMESPACE + class_name, method, digest(parsed_name))
            identities[key] += 1
            cases.append({'type': key[0], 'method': method,
                          'sourceCaseSha256': digest(json.dumps([key[0], method, names, row], separators=(',', ':'))),
                          'formatterNameSha256': digest(formatted), 'parsedXmlNameSha256': key[2]})
        methods.append({'method': method, 'sourceDeclaredExecutions': len(values)})
    assert sum(identities.values()) == expected_count
    assert selected_method is None or len(methods) == 1
    return relative, identities, cases, methods


primary = json.loads((PRIMARY / 'xunit-formatter-primary-source.json').read_text())
(PREP / 'primary-xunit').mkdir(exist_ok=True)
for name, info in primary['files'].items():
    original = PRIMARY / 'primary-xunit' / name
    assert sha(original) == info['sha256']
    target = PREP / 'primary-xunit' / name
    if target.exists():
        assert sha(target) == info['sha256']
    else:
        shutil.copyfile(original, target)
store('xunit-formatter-primary-source.json', primary)

source_hashes = {}
selections = []
all_expected = collections.Counter()
source_proof = []
for index, (class_name, method, count) in enumerate(SELECTIONS, 1):
    relative, expected, cases, methods = source_cases(class_name, method, count)
    source_hashes[relative] = sha(ROOT / relative)
    all_expected.update(expected)
    selections.append({'alias': f'selection-{index}', 'flag': '-method' if method else '-class',
                       'filter': NAMESPACE + class_name + ('.' + method if method else ''),
                       'expectedExecutions': count,
                       'expectedDisplayMultiset': [dict(type=k[0], method=k[1], parsedXmlNameSha256=k[2], multiplicity=v)
                                                   for k, v in sorted(expected.items())]})
    source_proof.append({'source': relative, 'sourceSha256': source_hashes[relative],
                         'methods': methods, 'expectedExecutions': count, 'sourceCases': cases})
assert sum(all_expected.values()) == 103 and len(all_expected) == 103

historical = Path('/tmp/story65-memo-rw-owner-native-20261107/regression412.xml')
joint_type = NAMESPACE + 'HeldMetadataDispatchTests'
joint_method = 'RealDomainFilterAndResultComposeWithSdkMetadataClient'
tests = [t for t in ET.parse(historical).getroot().iter('test')
         if t.get('type') == joint_type and t.get('method') == joint_method]
actual_joint = collections.Counter((t.get('type'), t.get('method'), digest(t.get('name') or '')) for t in tests)
expected_joint = collections.Counter({k: v for k, v in all_expected.items() if k[0] == joint_type and k[1] == joint_method})
assert len(tests) == 1 and actual_joint == expected_joint and tests[0].get('result') == 'Pass'
history = [{'path': str(historical), 'sha256': sha(historical), 'selectedExecutions': 1,
            'exactCurrentSourceDerivedMultisetMatch': True, 'allSelectedHistoricalResultsPass': True,
            'notFull103RetentionOrNewExecution': True}]
reference = {'qualificationReady': False, 'metadataPreparationComplete': True, 'RootSelectionApproved': True,
             'expectedTotal': 103, 'distinctRenderedIdentities': 103, 'selections': selections,
             'fixtureSourceSha256': source_hashes, 'historicalCorroboration': history,
             'sourceDerivedExactTypeMethodParsedXmlNameMultiplicity': True,
             'notClaimedCurrentNative103AlreadyRun': True, 'notClaimedAll103SubsetOfCurrentOrdinary1048': True,
             'ordinaryUniqueNativeQualificationUnchanged': 1048, 'oldTechnical47NotReused': True,
             'unchangedRunnerDefaultStringLimit': 50, 'twoLayerFormatterThenReporterEscaping': True,
             'noFuzzyNormalization': True, 'noRunnerConfigEnvironmentOrTestChanges': True}
store('native-reference-case-set.json', reference)
store('native103-source-derivation.json', {'metadataOnly': True, 'assembliesLoaded': 0, 'nativeExecutions': 0,
                                        'primarySourceSha256': sha(PREP / 'xunit-formatter-primary-source.json'),
                                        'selectedSources': source_proof, 'expectedExecutions': 103,
                                        'exactHistoricalCorroborationOnlyOneJointCase': history,
                                        'actualVariantDomainServiceAndOrdinarySdkMappingStillMandatory': True})
print(json.dumps({'prepared': True, 'expectedExecutions': 103, 'distinctRenderedIdentities': 103,
                  'historicallyCorroboratedJointOnly': 1, 'referenceSha256': sha(PREP / 'native-reference-case-set.json')}))
