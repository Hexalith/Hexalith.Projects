#!/usr/bin/env node
// Executes only pinned pure/history analysis modules; never loads release hooks.
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { execFileSync } from 'node:child_process';
import crypto from 'node:crypto';
import util from 'node:util';
const [repo, evidence] = process.argv.slice(2);
if (!repo || !evidence) throw new Error('Usage: analyze-release.mjs <selected-worktree> <analyzer-directory>');
const git = (...args) => execFileSync('git', ['--no-replace-objects','-C',repo,...args], {encoding:'utf8'}).trim();
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const inputs = JSON.parse(fs.readFileSync(path.join(evidence,'verified-inputs.json')));
if (git('rev-parse','HEAD') !== inputs.selectedRevision) throw new Error('Selected HEAD changed');
const pkg = JSON.parse(fs.readFileSync(path.join(repo,'package.json')));
if (pkg.release.plugins.filter(p => p === '@semantic-release/commit-analyzer').length !== 1) throw new Error('Expected the configured default analyzer');
const importPinned = relative => import(pathToFileURL(path.join(repo,'node_modules',relative)));
const { default:getLastRelease } = await importPinned('semantic-release/lib/get-last-release.js');
const { default:getCommits } = await importPinned('semantic-release/lib/get-commits.js');
const { default:getNextVersion } = await importPinned('semantic-release/lib/get-next-version.js');
const { analyzeCommits } = await importPinned('@semantic-release/commit-analyzer/index.js');
const logs=[];
const logger={log:(...args)=>{const line=util.format(...args);logs.push(line);console.log(line);},error:(...args)=>{throw new Error(util.format(...args));}};
// semantic-release's default tagFormat and normalized main release branch. Tags
// are restricted to independently verified, official, reachable stable objects.
const branch={name:'main',type:'release',channel:undefined,tags:inputs.reachableStableTags};
const options={...pkg.release,tagFormat:'v${version}'};
const lastRelease=getLastRelease({branch,options});
const previous=inputs.previousRelease;
if(lastRelease.gitTag!==previous.gitTag || git('rev-parse',lastRelease.gitHead+'^{commit}')!==previous.officialCommit) throw new Error('Previous release identity mismatch');
const context={cwd:repo,env:process.env,branch,options,lastRelease,nextRelease:{gitHead:inputs.selectedRevision},logger};
const commits=await getCommits(context);
const type=await analyzeCommits({}, {...context,commits});
const version=type ? getNextVersion({...context,nextRelease:{...context.nextRelease,type}}) : null;
const modules=['semantic-release/lib/get-last-release.js','semantic-release/lib/get-commits.js','semantic-release/lib/get-next-version.js','semantic-release/lib/git.js','@semantic-release/commit-analyzer/index.js','@semantic-release/commit-analyzer/lib/default-release-rules.js','@semantic-release/commit-analyzer/lib/load-parser-config.js'];
const output={schema:'story64.analysis-only.v1',analyzedUtc:new Date().toISOString(),node:process.version,selectedRevision:inputs.selectedRevision,selectedTree:git('rev-parse','HEAD^{tree}'),previousRelease:lastRelease,previousReleaseCommit:previous.officialCommit,commits,analyzerConfiguration:{},releaseType:type,proposedVersion:version,semanticReleaseVersion:JSON.parse(fs.readFileSync(path.join(repo,'node_modules/semantic-release/package.json'))).version,commitAnalyzerVersion:JSON.parse(fs.readFileSync(path.join(repo,'node_modules/@semantic-release/commit-analyzer/package.json'))).version,packageJsonSha256:hash(path.join(repo,'package.json')),packageLockSha256:hash(path.join(repo,'package-lock.json')),verifiedInputsSha256:hash(path.join(evidence,'verified-inputs.json')),moduleHashes:Object.fromEntries(modules.map(m=>[m,hash(path.join(repo,'node_modules',m))])),executedLifecycle:false,notes:'Conditional proposal for selected exact source. No plugins other than analyzeCommits loaded; no push verification, tag, verifyRelease, publish, or lifecycle call.'};
fs.writeFileSync(path.join(evidence,'analysis-result.json'),JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify({previousRelease:output.previousReleaseCommit,commitCount:commits.length,releaseType:type,proposedVersion:version,executedLifecycle:false},null,2));
