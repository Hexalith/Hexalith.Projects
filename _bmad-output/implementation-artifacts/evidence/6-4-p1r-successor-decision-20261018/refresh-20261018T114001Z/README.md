# Current P1R publication readiness observation

Capture began `2026-10-18T11:40:01.624257Z`. Exact-source gate recheck ended `2026-10-18T11:49:26.545729Z`; live `main` remained `39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2`. These read-only observations support the [decision packet](../../../6-4-p1r-successor-publication-decision-2026-10-18.md), not publication, package qualification or acceptance. No checkout, dependency, owner contract, Story 6.4 state or fixed acceptance was changed.

- `git-remote-refs.json`: independent read-only Git ref queries confirm EventStore main/latest tag, absence of the proposed tag, and Builds main.
- `collection-commands.json`: read-only GET command arrays, explicit source/run selection and literal NuGet byte-capture recipe; use a new output location for any recapture.
- `source-observations.json`: GitHub main refs and exact post-v3.124.0 compare commit records; changed-file patch array omitted. Local checkout is recorded separately.
- `gate-observations.json`: exact-main push lists, selected run responses, separate resolved reusable workflow identities and job/step outcomes. CI is in progress with no conclusion; P1R preparation and Commitlint succeeded. A pending result is not success or failure.
- `release-history.json`: latest/recent releases, latest tag target, explicit v3.125.0 tag/release 404 probes and release-workflow history.
- `publication-config.json`: exact-source workflow/release config/manifest/preflight text, Git blob IDs, SHA-256 values, enabled repository publication flag and production reviewer/branch policy. No owner review is inferred.
- `builds-observations.json`: package-build gitlink `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`, CI reusable revision `4629ff2d3bd66376b29a553663c241d3a7b87853`, release execution `22a578b576a515d2af214fe81859447fffc97981`, workspace/remote revisions and catalog. Actual package-build execution is unverified before publication.
- `release-analysis.json`: all 22 full commit messages, analyzer 13.0.1 output and config hash; minor proposes 3.125.0. No Semantic Release dry-run or publication lifecycle ran.
- `nuget-observations.json` plus five `*.index.json`: official URL, exact times/status/headers and SHA-256 of each byte-preserved response. All HTTP 200, latest 3.124.0, proposed 3.125.0 absent.
- `analyzer-dependency-identity.json`: exact-source lockfile blob/hash and analyzer lock/installed version identity; the installed lock matches the observed source.
- `protected-before.json` and `verification.json`: baseline hashes and final preservation comparison for existing user changes, fixed acceptance, sprint state and package catalogs.
- `sha256-manifest.json`: hashes of the capture artifacts; no new acceptance authority is inferred.

Reproduce release analysis from the **already installed** EventStore Node dependencies, without dependency updates or Semantic Release execution:

```bash
node --input-type=module <<'NODE'
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { analyzeCommits } from '@semantic-release/commit-analyzer';
import semver from 'semver';
const path = '../../_bmad-output/implementation-artifacts/evidence/6-4-p1r-successor-decision-20261018/refresh-20261018T114001Z/';
const identity = JSON.parse(readFileSync(path + 'analyzer-dependency-identity.json', 'utf8'));
const installed = JSON.parse(readFileSync('node_modules/@semantic-release/commit-analyzer/package.json', 'utf8'));
if (installed.version !== identity.analyzer_lock_entry.version ||
    createHash('sha256').update(readFileSync('package-lock.json')).digest('hex') !== identity.sha256) {
  throw new Error('Analyzer version or lockfile differs from the captured source');
}
const input = JSON.parse(readFileSync(path + 'source-observations.json', 'utf8'));
const history = JSON.parse(readFileSync(path + 'release-history.json', 'utf8'));
const commits = input.compare.data.commits.map(c => ({ hash: c.sha, message: c.commit.message }));
const releaseType = await analyzeCommits({}, { commits, cwd: process.cwd(), logger: { log: () => {} } });
console.log({ count: commits.length, releaseType, proposedVersion: semver.inc(history.latest.data.tag_name.slice(1), releaseType) });
NODE
```

Run this command with `references/Hexalith.EventStore` as the working directory. The captured config selects the default commit analyzer without custom options. It is a prediction only. Before a decision or dispatch, recheck live refs, all three required successful exact-source push proofs, their resolved Builds identity, readiness disposition, release history and package indexes; coordinate drift requires an updated decision.
