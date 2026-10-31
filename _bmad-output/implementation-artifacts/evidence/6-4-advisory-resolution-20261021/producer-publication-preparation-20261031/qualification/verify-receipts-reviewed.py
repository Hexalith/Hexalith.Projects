#!/usr/bin/env python3
"""Read-only verification of the frozen child manifest, approval binding and receipts."""
import argparse
import hashlib
import json
from pathlib import Path
import sys


def digest(data):
    return hashlib.sha256(data).hexdigest()


def require(condition, label):
    if not condition:
        raise ValueError(label)


def verify(root, expected_manifest_digest):
    root = root.resolve()
    manifest_bytes = (root / 'implementation-evidence-manifest.json').read_bytes()
    require(digest(manifest_bytes) == expected_manifest_digest, 'trusted implementation manifest digest mismatch')
    manifest = json.loads(manifest_bytes)
    seen = set()
    for row in manifest['files']:
        relative = Path(row['file'])
        require(not relative.is_absolute() and '..' not in relative.parts and row['file'] not in seen, 'unsafe or duplicate manifest path')
        seen.add(row['file'])
        data = (root / relative).read_bytes()
        require(digest(data) == row['sha256'] and len(data) == row['sizeBytes'], f'implementation manifest file mismatch: {relative}')
    for required in ['analyzer/analysis-result.json', 'analyzer/verified-inputs.json']:
        require(required in seen, f'missing implementation manifest entry: {required}')
    def load(relative):
        return json.loads((root / relative).read_bytes())
    analysis_bytes = (root / 'analyzer/analysis-result.json').read_bytes()
    analysis = json.loads(analysis_bytes)
    approval, source = load('approval-package.json'), load('source-inputs.json')
    require(approval['versionProposal']['analysisSha256'] == digest(analysis_bytes), 'approval analysis digest mismatch')
    verified_bytes = (root / 'analyzer/verified-inputs.json').read_bytes()
    verified = json.loads(verified_bytes)
    require(analysis['verifiedInputsSha256'] == digest(verified_bytes), 'verified inputs digest mismatch')
    require(analysis['selectedRevision'] == approval['selectedSource'] == source['selectedSource'] == verified['selectedRevision'], 'selected source mismatch')
    require(analysis['selectedTree'] == approval['selectedTree'] == source['tree'], 'selected tree mismatch')
    proposal = approval['versionProposal']
    require((analysis['proposedVersion'], analysis['releaseType'], len(analysis['commits']), analysis['semanticReleaseVersion'], analysis['commitAnalyzerVersion']) == (proposal['version'], proposal['type'], proposal['commitCount'], proposal['semanticRelease'], proposal['commitAnalyzer']), 'approval analysis proposal mismatch')
    require(analysis['executedLifecycle'] is False and proposal['lifecycleExecuted'] is False, 'lifecycle executed')
    for name, key in [('package.json', 'packageJsonSha256'), ('package-lock.json', 'packageLockSha256')]:
        require(analysis[key] == source['inputs'][name]['sha256'] == approval['releaseInputs'][name]['sha256'], f'package input mismatch: {name}')
    receipts = []
    for relative in sorted(seen):
        if not relative.endswith('.command.json'):
            continue
        receipt_path = root / relative
        receipt = json.loads(receipt_path.read_bytes())
        for kind in ['stdout', 'stderr']:
            row = receipt[kind]
            require(Path(row['file']).name == row['file'], 'unsafe receipt stream path')
            data = (receipt_path.parent / row['file']).read_bytes()
            require(digest(data) == row['sha256'] and len(data) == row['sizeBytes'], f'command stream mismatch: {relative}/{kind}')
        receipts.append({'file': relative, 'exitCode': receipt['exitCode']})
    archives = load('qualification/local-archive-observations.json')
    require(len(archives['archives']) == 4 and archives['releaseEligible'] is False, 'archive qualification state mismatch')
    for row in archives['archives']:
        data = (root / 'qualification/local-packages-4.39.0' / row['file']).read_bytes()
        require(digest(data) == row['sha256'] and len(data) == row['sizeBytes'], 'archive digest mismatch')
        require(row['repository']['commit'] == analysis['selectedRevision'] and row['version'] == analysis['proposedVersion'], 'archive source mismatch')
        require(all(row[field] is False for field in ['published', 'signed', 'qualificationPassed']), 'archive acceptance state mismatch')
    require(not (root / 'qualification/local-packages-4.39.0/g4-tool-package-inventory.json').exists(), 'unexpected official inventory')
    require(approval['fullLocalQualification']['releaseEligible'] is False and approval['publicationReady'] is False and approval['publicationAuthorized'] is False and approval['candidateAcceptance'] is False, 'blocked approval state changed')
    full = load('qualification/full-qualification-status.json')
    require(full['result'] == 'not-passed' and full['exitCode'] is None and full['publicationBlocked'] is True and approval['fullLocalQualification']['status'] == 'not-run', 'full qualification status changed')
    vulnerability = load('analyzer/npm-vulnerability-audit.stdout.log')['metadata']['vulnerabilities']
    security = approval['securityObservation']
    require(security['vulnerableDependencyEntries'] == {key: vulnerability[key] for key in ['moderate', 'high', 'total']}, 'security finding counts mismatch')
    require(security['vulnerabilityAuditExit'] == load('analyzer/npm-vulnerability-audit.command.json')['exitCode'] and security['signatureAuditExit'] == load('analyzer/npm-audit-signatures.command.json')['exitCode'], 'security command exit mismatch')
    return {'consistent': True, 'implementationManifestSha256': expected_manifest_digest, 'manifestFileCount': len(seen), 'commandReceipts': receipts, 'archiveCount': 4, 'qualificationPassed': False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('--manifest-sha256', required=True)
    parser.add_argument('--output', type=Path, help='optional absent output path; no historical output is rewritten')
    args = parser.parse_args()
    if args.output and args.output.exists():
        parser.error('result already exists; refusing to overwrite')
    result = verify(args.root, args.manifest_sha256)
    encoded = json.dumps(result, indent=2) + '\n'
    if args.output:
        with args.output.open('x') as stream:
            stream.write(encoded)
    print(encoded, end='')


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, KeyError, TypeError) as exc:
        print(f'Verification failed: {exc}', file=sys.stderr)
        sys.exit(1)
