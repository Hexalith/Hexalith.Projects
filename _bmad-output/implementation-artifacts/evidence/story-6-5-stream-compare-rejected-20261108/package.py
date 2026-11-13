"""File-only rejected-benchmark packet; no production, build, runtime or Git actions."""
from pathlib import Path
import gzip, hashlib, json
PREP = Path('/tmp/story65-stream-compare-prep-20261108')
BUILD = Path('/tmp/story65-stream-compare-build-20261108')
RESULTS = Path('/tmp/story65-stream-compare-results-20261108')
OUT = Path('/tmp/story65-stream-compare-publication-prep-20261108')
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
provenance = []
def copy(source, name, compressed=False):
    source = Path(source); assert source.is_file() and not source.is_symlink()
    data = source.read_bytes(); target = OUT / name; target.parent.mkdir(parents=True, exist_ok=True)
    with target.open('xb') as f: f.write(gzip.compress(data, mtime=0) if compressed else data)
    provenance.append({'originalPath': str(source), 'originalSha256': hashlib.sha256(data).hexdigest(), 'originalBytes': len(data), 'destination': name, 'storedSha256': sha(target), 'storedBytes': target.stat().st_size, 'transformation': 'gzip-mtime-zero' if compressed else 'byte-exact'})
assert not OUT.exists()
assert sha(RESULTS / 'root-rejection.json') == 'c3396940ba1b64f7c30e295a128ed2d948400637454c5329eb8a9577f14b4c49'
manifest = json.loads((PREP / 'preparation-receipt.json').read_bytes())
assert sha(PREP / 'preparation-receipt.json') == 'bb25da6a857941f806125b71c60d0fdbb4877e31053a4e02dac369c51a9ae628'
assert len(manifest['filesSha256']) == 53 and len(manifest['inputsSha256']) == 747
for name, digest in manifest['filesSha256'].items():
    assert sha(PREP / name) == digest
    copy(PREP / name, 'preparation/' + name)
copy(PREP / 'preparation-receipt.json', 'preparation/preparation-receipt.json')
for source in sorted(RESULTS.rglob('*')):
    if source.is_file():
        assert source.suffix in ['.json', '.jsonl']
        name = str(source.relative_to(RESULTS))
        copy(source, 'results/' + name + ('.gz' if source.suffix == '.jsonl' else ''), source.suffix == '.jsonl')
for name in ['receipt.json', 'commands.json', 'build.log', 'restore.log', 'root-build-release.json', 'metadata-only-cohort-counts.json', 'count-correction/receipt.json', 'count-correction/build.log', 'runtime-release/root-release.json', 'runtime-release/run-receipt.json']:
    copy(BUILD / name, 'build-and-execution/' + name)
copy(Path(__file__), 'package.py')
(OUT / 'provenance.json').write_text(json.dumps({'decision': 'reject', 'featureAcceptance': False, 'productionEdits': False, 'copies': provenance, 'notCopied': ['binary outputs/binobj', 'duplicate historical preparation manifests', 'historical uncompiled IO-lock draft', 'private runtime data', 'native stdout or trace data'], 'historicalCompileRevisions': manifest['priorCompileOnly'], 'actualCompiledBuildReceipt': manifest['compiledBuildReceipt']}, indent=2) + '\n')
print(json.dumps({'destination': str(OUT), 'sourceFiles': len(manifest['filesSha256']), 'copiedArtifacts': len(provenance)}))
