"""Source/XML metadata only. No assembly load, build, test, or topology operation."""
from pathlib import Path
import collections
import hashlib
import json
import re
import shutil
import xml.etree.ElementTree as ET

ROOT = Path('/home/administrator/projects/hexalith/projects')
PREP = Path('/tmp/story65-receiver-phase-ops-prep-20261107')
PRIOR = Path('/tmp/story65-dapr-invoker-ops-prep-20261107/live-xml-reporter-retry')
NAMESPACE = 'Hexalith.EventStore.Server.Tests.Routing.'
CLASSES = [
    ('TechnicalHttpInstalledMetadataTests', 8),
    ('TechnicalHttpNativeRpcTests', 12),
    ('ReceiverImagePrecheckTests', 20),
    ('NativeDispatchDiagnosticsTests', 7),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def digest(value):
    return hashlib.sha256(value.encode()).hexdigest()


def store(name, value):
    target = PREP / name
    target.write_text(json.dumps(value, indent=2) + '\n')


def argument(value):
    if type(value) is bool:
        return 'True' if value else 'False'
    if type(value) is int:
        return str(value)
    assert type(value) is str and all(32 <= ord(c) <= 126 for c in value)
    # All selected strings are ASCII and below the unchanged default 50 chars.
    # Pin the general primary algorithm, but reject a new unsupported literal.
    assert len(value) <= 50 and '\\' not in value and '"' not in value
    return '"' + value + '"'


def reporter_escape(value):
    # XmlUtility.Escape runs AFTER argument formatting; ElementTree then removes
    # only the XML attribute encoding, preserving these literal backslashes.
    assert all(32 <= ord(c) <= 126 for c in value)
    return value.replace('\\', '\\\\').replace('"', '\\"')


def current_source(class_name, expected_count):
    relative = f'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Routing/{class_name}.cs'
    text = (ROOT / relative).read_text()
    assert f'namespace {NAMESPACE[:-1]};' in text
    pattern = r'((?:\s*\[(?:Fact|Theory|InlineData)[^\n]*\]\s*)+)public\s+async\s+Task\s+(\w+)\(([^)]*)\)'
    identities = collections.Counter()
    cases = []
    methods = []
    for match in re.finditer(pattern, text):
        attributes, method, parameters = match.groups()
        names = [part.strip().split()[-1] for part in parameters.split(',')] if parameters.strip() else []
        assert 'Skip' not in attributes and 'DisplayName' not in attributes
        data = re.findall(r'\[InlineData\((.*)\)\]', attributes)
        assert ('[Fact]' in attributes) != ('[Theory]' in attributes)
        values = [json.loads('[' + row + ']') for row in data] if data else [[]]
        assert bool(data) == ('[Theory]' in attributes)
        for row in values:
            assert len(row) == len(names)
            formatted = NAMESPACE + class_name + '.' + method
            if row:
                formatted += '(' + ', '.join(name + ': ' + argument(value) for name, value in zip(names, row)) + ')'
            parsed_name = reporter_escape(formatted)
            case_hash = digest(json.dumps([NAMESPACE + class_name, method, names, row], separators=(',', ':')))
            key = (NAMESPACE + class_name, method, digest(parsed_name))
            identities[key] += 1
            cases.append({'type': key[0], 'method': method, 'sourceCaseSha256': case_hash,
                          'formatterNameSha256': digest(formatted), 'parsedXmlNameSha256': key[2]})
        methods.append({'method': method, 'sourceDeclaredExecutions': len(values)})
    assert sum(identities.values()) == expected_count
    assert len(re.findall(r'^\s*\[(?:Fact|Theory)(?:\]|\()', text, re.M)) == len(methods)
    return relative, sha(ROOT / relative), identities, cases, methods


primary = json.loads((PRIOR / 'xunit-formatter-primary-source.json').read_text())
(PREP / 'primary-xunit').mkdir(exist_ok=True)
for name, info in primary['files'].items():
    source = PRIOR / 'primary-xunit' / name
    assert sha(source) == info['sha256']
    target = PREP / 'primary-xunit' / name
    if target.exists():
        assert sha(target) == info['sha256']
    else:
        shutil.copyfile(source, target)
store('xunit-formatter-primary-source.json', primary)

source_hashes = {}
selections = []
expected_all = collections.Counter()
case_proof = []
for index, (class_name, count) in enumerate(CLASSES, 1):
    relative, source_sha, counter, cases, methods = current_source(class_name, count)
    source_hashes[relative] = source_sha
    expected_all.update(counter)
    selections.append({'alias': f'selection-{index}', 'flag': '-class', 'filter': NAMESPACE + class_name,
                       'expectedExecutions': count,
                       'expectedDisplayMultiset': [dict(type=k[0], method=k[1], parsedXmlNameSha256=k[2], multiplicity=v)
                                                   for k, v in sorted(counter.items())]})
    case_proof.append({'source': relative, 'sourceSha256': source_sha, 'methods': methods,
                       'expectedExecutions': count, 'sourceCases': cases})

historical = [
    (Path('/tmp/story65-memo-rw-owner-native-20261107/regression412.xml'),
     {'TechnicalHttpInstalledMetadataTests', 'ReceiverImagePrecheckTests'}, 'current-owner-regression-subset-28'),
    (ROOT / '_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/atomic-authority.xml',
     {'TechnicalHttpNativeRpcTests', 'NativeDispatchDiagnosticsTests'}, 'historical-19-no-current-execution-claim'),
]
corroborated = collections.Counter()
history_proof = []
for xml, classes, label in historical:
    tests = [t for t in ET.parse(xml).getroot().iter('test') if t.get('type', '').rsplit('.', 1)[-1] in classes]
    actual = collections.Counter((t.get('type'), t.get('method'), digest(t.get('name') or '')) for t in tests)
    expected = collections.Counter({k: v for k, v in expected_all.items() if k[0].rsplit('.', 1)[-1] in classes})
    assert actual == expected and all(t.get('result') == 'Pass' for t in tests)
    corroborated.update(actual)
    history_proof.append({'path': str(xml.relative_to(ROOT)) if xml.is_relative_to(ROOT) else str(xml),
                          'sha256': sha(xml), 'provenance': label, 'selectedExecutions': len(tests),
                          'selectedDistinctRenderedIdentities': len(actual), 'exactCurrentSourceDerivedMultisetMatch': True,
                          'allSelectedHistoricalResultsPass': True, 'notNewExecutionOrFullCurrentQualification': True})
assert sum(expected_all.values()) == 47 and corroborated == expected_all

reference = {'qualificationReady': False, 'metadataPreparationComplete': True,
             'expectedTotal': 47, 'distinctRenderedIdentities': len(expected_all), 'selections': selections,
             'fixtureSourceSha256': source_hashes, 'historicalCorroboration': history_proof,
             'sourceDerivedExactTypeMethodParsedXmlNameMultiplicity': True,
             'currentOwnerRegressionCorroboratedSubset': 28, 'olderHistoricalCorroboratedRemainder': 19,
             'notClaimedCurrentNative47AlreadyRun': True, 'notClaimedAll47SubsetOfCurrentOrdinary1048': True,
             'ordinaryUniqueNativeQualificationUnchanged': 1048, 'oldInvocation33NotReused': True,
             'unchangedRunnerDefaultStringLimit': 50, 'twoLayerFormatterThenReporterEscaping': True,
             'noFuzzyNormalization': True, 'noRunnerConfigEnvironmentOrTestChanges': True}
store('native-reference-case-set.json', reference)
store('native47-source-derivation.json', {'metadataOnly': True, 'assembliesLoaded': 0, 'nativeExecutions': 0,
                                       'primarySourceSha256': sha(PREP / 'xunit-formatter-primary-source.json'),
                                       'selectedClasses': case_proof, 'expectedExecutions': 47,
                                       'exactHistoricalUnionCorroboration': history_proof,
                                       'actualRuntimePrivateSdkMappingStillMandatoryPerFutureProcess': True,
                                       'futureFourNativeProcessGroupsMustBeAbsentBeforeManaged': True})
print(json.dumps({'prepared': True, 'expectedExecutions': 47, 'distinctRenderedIdentities': len(expected_all),
                  'currentOwnerCorroboration': 28, 'olderHistoricalCorroboration': 19,
                  'referenceSha256': sha(PREP / 'native-reference-case-set.json')}))
