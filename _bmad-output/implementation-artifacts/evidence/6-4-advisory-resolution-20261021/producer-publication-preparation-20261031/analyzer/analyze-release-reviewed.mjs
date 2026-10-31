#!/usr/bin/env node
// Analysis-only replay. Historical inputs/results are read-only; no release lifecycle is loaded.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import util from 'node:util';
import { execFileSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';
const [repoArg, inputArg, outputArg, expectedManifestSha256] = process.argv.slice(2);
if (!repoArg || !inputArg || !outputArg || !/^[a-f0-9]{64}$/.test(expectedManifestSha256 ?? '')) {
  throw new Error('Usage: analyze-release-reviewed.mjs <clean-selected-repo> <fresh-input-root> <absent-output-directory> <trusted-manifest-sha256>');
}
const repo = fs.realpathSync(repoArg), input = fs.realpathSync(inputArg), output = path.resolve(outputArg);
if (fs.existsSync(output)) throw new Error('Output directory already exists; refusing to overwrite results');
if (output === input || output.startsWith(input + path.sep)) throw new Error('Output must be separate from retained inputs');
for (const key of ['GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_REPLACE_REF_BASE']) {
  if (process.env[key]) throw new Error(`Git source override is forbidden: ${key}`);
}
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const read = relative => fs.readFileSync(path.join(input, relative));
const decode = relative => JSON.parse(read(relative));
const requireEqual = (actual, expected, label) => { if (actual !== expected) throw new Error(`${label} mismatch`); };
const manifestBytes = read('implementation-evidence-manifest.json');
requireEqual(sha(manifestBytes), expectedManifestSha256, 'Trusted implementation manifest digest');
const manifest = JSON.parse(manifestBytes);
const manifestRows = new Map();
for (const row of manifest.files) {
  if (manifestRows.has(row.file)) throw new Error('Duplicate implementation manifest entry');
  manifestRows.set(row.file, row);
}
for (const relative of ['analyzer/analysis-result.json', 'analyzer/verified-inputs.json']) {
  const bytes = read(relative), row = manifestRows.get(relative);
  if (!row) throw new Error(`Missing manifest entry: ${relative}`);
  requireEqual(sha(bytes), row.sha256, `Retained ${relative} digest`);
  requireEqual(bytes.length, row.sizeBytes, `Retained ${relative} size`);
}
const reference = decode('analyzer/analysis-result.json'), verified = decode('analyzer/verified-inputs.json');
const approval = decode('approval-package.json'), source = decode('source-inputs.json');
requireEqual(sha(read('analyzer/analysis-result.json')), approval.versionProposal.analysisSha256, 'Approval analysis digest');
requireEqual(sha(read('analyzer/verified-inputs.json')), reference.verifiedInputsSha256, 'Verified inputs digest');
for (const revision of [approval.selectedSource, source.selectedSource, verified.selectedRevision]) requireEqual(revision, reference.selectedRevision, 'Retained selected source');
for (const tree of [approval.selectedTree, source.tree]) requireEqual(tree, reference.selectedTree, 'Retained selected tree');
const git = (...args) => execFileSync('git', ['--no-replace-objects', '-C', repo, ...args], {encoding:'utf8'}).trim();
requireEqual(git('rev-parse', 'HEAD'), reference.selectedRevision, 'Selected HEAD');
requireEqual(git('rev-parse', 'HEAD^{tree}'), reference.selectedTree, 'Selected tree');
for (const [relative, digest] of [['package.json', reference.packageJsonSha256], ['package-lock.json', reference.packageLockSha256]]) {
  const committed = execFileSync('git', ['--no-replace-objects', '-C', repo, 'show', `${reference.selectedRevision}:${relative}`]);
  requireEqual(sha(committed), digest, `Committed ${relative}`);
  requireEqual(sha(fs.readFileSync(path.join(repo, relative))), digest, `Working ${relative}`);
  requireEqual(source.inputs[relative].sha256, digest, `Source input ${relative}`);
  requireEqual(approval.releaseInputs[relative].sha256, digest, `Approved input ${relative}`);
  requireEqual(git('rev-parse', `${reference.selectedRevision}:${relative}`), source.inputs[relative].blob, `Committed ${relative} blob`);
}
const packageJson = JSON.parse(fs.readFileSync(path.join(repo, 'package.json')));
const lock = JSON.parse(fs.readFileSync(path.join(repo, 'package-lock.json')));
if (packageJson.release.plugins.filter(plugin => plugin === '@semantic-release/commit-analyzer').length !== 1) throw new Error('Expected the committed default analyzer configuration');
const modulesRoot = fs.realpathSync(path.join(repo, 'node_modules'));
const validateModules = () => {
  for (const [relative, digest] of Object.entries(reference.moduleHashes)) {
    const absolute = fs.realpathSync(path.join(modulesRoot, relative));
    if (!absolute.startsWith(modulesRoot + path.sep)) throw new Error('Installed module escapes node_modules');
    requireEqual(sha(fs.readFileSync(absolute)), digest, `Installed module ${relative}`);
  }
  for (const [name, version] of [['semantic-release', reference.semanticReleaseVersion], ['@semantic-release/commit-analyzer', reference.commitAnalyzerVersion]]) {
    requireEqual(JSON.parse(fs.readFileSync(path.join(modulesRoot, name, 'package.json'))).version, version, `Installed ${name} version`);
    requireEqual(lock.packages[`node_modules/${name}`].version, version, `Locked ${name} version`);
  }
};
validateModules();
requireEqual(git('status', '--porcelain=v1'), '', 'Clean selected source');
for (const tag of verified.reachableStableTags) {
  requireEqual(git('rev-parse', `${tag.gitTag}^{commit}`), tag.officialCommit, `Official tag ${tag.gitTag}`);
  execFileSync('git', ['--no-replace-objects', '-C', repo, 'merge-base', '--is-ancestor', tag.officialCommit, reference.selectedRevision]);
}
const importPinned = relative => import(pathToFileURL(path.join(modulesRoot, relative)));
const { default:getLastRelease } = await importPinned('semantic-release/lib/get-last-release.js');
const { default:getCommits } = await importPinned('semantic-release/lib/get-commits.js');
const { default:getNextVersion } = await importPinned('semantic-release/lib/get-next-version.js');
const { analyzeCommits } = await importPinned('@semantic-release/commit-analyzer/index.js');
const logger = {log:(...args) => console.log(util.format(...args))};
const branch = {name:'main', type:'release', tags:verified.reachableStableTags};
const options = {...packageJson.release, tagFormat:'v${version}'};
const lastRelease = getLastRelease({branch, options});
requireEqual(lastRelease.gitTag, verified.previousRelease.gitTag, 'Previous release tag');
requireEqual(git('rev-parse', `${lastRelease.gitHead}^{commit}`), reference.previousReleaseCommit, 'Previous release source');
const context = {cwd:repo, env:process.env, branch, options, lastRelease, nextRelease:{gitHead:reference.selectedRevision}, logger};
const commits = await getCommits(context);
const releaseType = await analyzeCommits({}, {...context, commits});
const proposedVersion = releaseType ? getNextVersion({...context, nextRelease:{...context.nextRelease, type:releaseType}}) : null;
requireEqual(releaseType, reference.releaseType, 'Replayed release type');
requireEqual(proposedVersion, reference.proposedVersion, 'Replayed version');
requireEqual(JSON.stringify(commits.map(c => c.hash)), JSON.stringify(reference.commits.map(c => c.hash)), 'Replayed commit history');
validateModules();
requireEqual(git('status', '--porcelain=v1'), '', 'Source after analysis');
fs.mkdirSync(output);
const result = {...reference, schema:'story64.analysis-only-reviewed.v1', analyzedUtc:new Date().toISOString(), node:process.version, commits, releaseType, proposedVersion, executedLifecycle:false, referenceAnalysisSha256:sha(read('analyzer/analysis-result.json')), implementationManifestSha256:expectedManifestSha256, notes:'Read-only committed-input and retained-module fingerprint checks passed; fresh analysis output only. No lifecycle hooks executed.'};
fs.writeFileSync(path.join(output, 'analysis-result-reviewed.json'), JSON.stringify(result, null, 2) + '\n', {flag:'wx'});
console.log(JSON.stringify({proposedVersion, releaseType, commitCount:commits.length, executedLifecycle:false}));
