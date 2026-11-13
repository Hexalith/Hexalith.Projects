from pathlib import Path
import datetime
import hashlib
import json
import re

BASE = Path('/tmp/story65-image-memo-rw-evidence-20261107')
sha = lambda data: hashlib.sha256(data).hexdigest()
def unique(pairs):
    result = {}
    for key, value in pairs:
        assert key not in result
        result[key] = value
    return result
def load(data):
    return json.loads(data, object_pairs_hook=unique)
manifest = BASE / 'SHA256SUMS'
assert sha(manifest.read_bytes()) == '760885dd69540c4be82a9d2c569f65eedf3e76e0d20c076c72ddefee2314d74f'
entries = {}
for line in manifest.read_text().splitlines():
    digest, name = line.split('  ', 1)
    assert re.fullmatch('[0-9a-f]{64}', digest) and name not in entries
    assert not Path(name).is_absolute() and '..' not in Path(name).parts
    entries[name] = digest
actual = {str(path.relative_to(BASE)) for path in BASE.rglob('*') if path.is_file()}
assert len(entries) == 323 and actual == set(entries) | {'SHA256SUMS'}
bad_patterns = 0
for name, digest in entries.items():
    path = BASE / name
    data = path.read_bytes()
    assert not path.is_symlink() and sha(data) == digest
    assert path.suffix.lower() not in {'.dll', '.pdb', '.exe', '.nettrace', '.etlx', '.pyc', '.pem', '.pfx', '.key'}
    bad_patterns += len(re.findall(rb'eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----', data))
assert bad_patterns == 0
copies = 0
for name in ('byte-copy-provenance.json', 'final-added-original-provenance.json'):
    for row in load((BASE / name).read_bytes()):
        data = (BASE / row['packetPath']).read_bytes()
        assert len(data) == row['bytes'] and sha(data) == row['sha256']
        assert Path(row['source']).read_bytes() == data
        copies += 1
raw = (BASE / 'actual-results/benchmark-samples.jsonl').read_bytes()
assert raw == Path('/tmp/story65-image-memo-rw-results-retry-20261107/benchmark-samples.jsonl').read_bytes()
rows = [load(line) for line in raw.splitlines()]
assert len(rows) == 324
observations = sum(len(worker['Observations']) for row in rows for worker in row['Workers'])
calls = sum(row['CompleteObservations'] * row['ImagesPerObservation'] for row in rows)
hits = sum(row['ReportedMemoHits'] for row in rows)
non_hits = sum(row['FreshResultsWithoutReportedReuse'] for row in rows)
assert (observations, calls, hits, non_hits) == (4032, 266112, 258984, 7128)
assert all(row['ImagesPerObservation'] == 66 and row['MemoCountAfter'] == 66 and row['ChargedBytesAfter'] == 27689784 for row in rows)
assert all(worker['FailureType'] is None for row in rows for worker in row['Workers'])
result = {'auditedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'candidateArtifacts': len(entries), 'candidateManifestSha256': sha(manifest.read_bytes()), 'allChecksumsAndExactPathSetMatch': True, 'originalCopySourcesRechecked': copies, 'rawOriginalBytes': len(raw), 'rawOriginalSha256': sha(raw), 'strictDuplicateCheckedNestedRows': len(rows), 'observations': observations, 'imageCalls': calls, 'memoHits': hits, 'freshNonReused': non_hits, 'all66OccupancyAnd27689784Charge': True, 'jwtPrivateKeyPatternMatches': bad_patterns, 'binaryRawTraceSymlinkArtifacts': 0, 'independent54GroupAuditAlreadyVerifiedByRoot': True, 'newProductNativeCasesCounted': 0, 'featureAcceptance': False, 'auditScriptSha256': sha(Path(__file__).read_bytes())}
target = Path(__file__).with_suffix('.json')
target.write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({'parentAuditSha256': sha(target.read_bytes()), 'candidateArtifacts': len(entries), 'originalCopiesRechecked': copies, 'rawRows': len(rows), 'observations': observations, 'privacyPatternMatches': bad_patterns}))
