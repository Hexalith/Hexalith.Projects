from pathlib import Path
from collections import Counter
import datetime
import gzip
import hashlib
import json
import math
import re

PACKET = Path('/tmp/story65-receiver-cpu-evidence-20261107')
LIVE_PROFILE = Path('/tmp/story65-receiver-cpu-owner-profile-20261107')
sha = lambda data: hashlib.sha256(data).hexdigest()
def unique(pairs):
    value = {}
    for key, item in pairs:
        assert key not in value, 'duplicate JSON key'
        value[key] = item
    return value
def decode(data):
    return json.loads(data, object_pairs_hook=unique)
entries = {}
for line in (PACKET / 'SHA256SUMS').read_text().splitlines():
    digest, name = line.split('  ', 1)
    assert re.fullmatch('[0-9a-f]{64}', digest) and name not in entries
    path = Path(name)
    assert not path.is_absolute() and '..' not in path.parts
    entries[name] = digest
actual = {str(path.relative_to(PACKET)) for path in PACKET.rglob('*') if path.is_file()}
assert actual == set(entries) | {'SHA256SUMS'} and len(entries) == 176
matches = 0
for name, digest in entries.items():
    path = PACKET / name
    assert not path.is_symlink() and sha(path.read_bytes()) == digest
    assert path.suffix.lower() not in {'.dll', '.pdb', '.exe', '.nettrace', '.etlx', '.pyc', '.pem', '.pfx', '.key'}
    raw = gzip.decompress(path.read_bytes()) if path.suffix == '.gz' else path.read_bytes()
    matches += len(re.findall(rb'eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----', raw))
assert matches == 0
copies = 0
for document in ('byte-copy-provenance.json', 'final-added-original-provenance.json'):
    for row in decode((PACKET / document).read_bytes()):
        data = (PACKET / row['packetPath']).read_bytes()
        assert len(data) == row['bytes'] and sha(data) == row['sha256']
        assert Path(row['source']).read_bytes() == data
        copies += 1
profiles = {}
frame_pattern = re.compile(r'^[A-Za-z_<>][A-Za-z0-9_.+`<>, \[\]:&*()!~=?|\-]*$')
for role in ('projects', 'tenants', 'folders'):
    folder = PACKET / 'profiles' / role
    summary = decode((folder / 'cpu-summary.json').read_bytes())
    compressed = (folder / 'cpu-samples.jsonl.gz').read_bytes()
    raw = gzip.decompress(compressed)
    compression = decode((folder / 'sample-compression-and-validation.json').read_bytes())
    assert sha(compressed) == compression['gzipSha256']
    assert sha(raw) == compression['originalSanitizedRowsSha256'] == summary['sanitizedRowsSha256']
    assert len(raw) == compression['originalBytes'] and raw == (LIVE_PROFILE / role / 'cpu-samples.jsonl').read_bytes()
    groups = {group['Id']: group for group in summary['stackGroups']}
    assert len(groups) == len(summary['stackGroups'])
    for group in groups.values():
        for frame in group['Frames']:
            assert frame == '[unresolved]' or (len(frame) <= 512 and frame_pattern.fullmatch(frame) and '.' in frame and not re.search(r'\b0x[0-9a-f]+\b|\.(?:cs|dll|exe|so|pdb)(?:\b|:)', frame, re.I))
    counts, types, threads = Counter(), Counter(), set()
    assert raw.endswith(b'\n')
    for line in raw.splitlines():
        row = decode(line)
        assert set(row) == {'timeUtc', 'relativeMilliseconds', 'threadId', 'sampleType', 'stackId'}
        assert type(row['threadId']) is int and row['threadId'] > 0
        assert type(row['stackId']) is int and row['stackId'] in groups
        assert row['sampleType'] == groups[row['stackId']]['SampleType']
        assert type(row['relativeMilliseconds']) in (int, float) and math.isfinite(row['relativeMilliseconds'])
        assert isinstance(row['timeUtc'], str) and datetime.datetime.fromisoformat(row['timeUtc']).tzinfo is not None
        counts[row['stackId']] += 1
        types[row['sampleType']] += 1
        threads.add(row['threadId'])
    assert counts == Counter({identity: group['Count'] for identity, group in groups.items()})
    assert types == Counter(summary['sampleTypeCounts'])
    assert sum(counts.values()) == summary['samples'] == summary['sanitizedRows'] == compression['originalRows']
    assert len(threads) == summary['sampledThreads']
    profiles[role] = {'rows': sum(counts.values()), 'groups': len(groups), 'originalSha256': sha(raw), 'gzipSha256': sha(compressed), 'originalBytes': len(raw), 'duplicateKeysRejected': True}
scalar_path = PACKET / 'managed/artifacts/advisory-readiness-scalars.jsonl'
scalar_keys = {'multiple', 'attempt', 'elapsedMilliseconds', 'httpStatus', 'result', 'incomplete', 'candidateCount', 'positiveCandidatesVerified', 'actualPublicGateway', 'nonBootstrapCredential', 'fabricatedProjectionWrite', 'runtimeAuthoritySupplied'}
scalars = [decode(row) for row in scalar_path.read_bytes().splitlines()]
assert len(scalars) == 32 and all(set(row) == scalar_keys for row in scalars)
result = {'auditedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'candidateChecksumManifestSha256': sha((PACKET / 'SHA256SUMS').read_bytes()), 'candidateArtifacts': len(entries), 'allChecksumsAndExactPathSetMatch': True, 'originalByteCopySourcesRechecked': copies, 'profiles': profiles, 'strictDuplicateCheckedRows': sum(p['rows'] for p in profiles.values()), 'allGzipOriginalBytesAndGroupsReconcile': True, 'scalarReadinessRows': len(scalars), 'closedScalarFieldSetOnly': True, 'jwtPrivateKeyPatternMatches': matches, 'rawTraceEtlxBinarySymlinkArtifacts': 0, 'privacyScanIsAdditionalToClosedSchemaAndDeletionProof': True, 'featureAcceptance': False, 'auditScriptSha256': sha(Path(__file__).read_bytes())}
target = Path(__file__).with_suffix('.json')
target.write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({'parentPacketAuditSha256': sha(target.read_bytes()), 'artifacts': len(entries), 'strictRows': result['strictDuplicateCheckedRows'], 'originalCopiesRechecked': copies, 'privacyPatternMatches': matches}))
