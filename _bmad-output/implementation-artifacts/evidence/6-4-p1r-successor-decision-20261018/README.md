# Current observation — publication readiness refresh

Use [refresh-20261018T114001Z](refresh-20261018T114001Z/README.md) for the latest full source/Builds revisions, exact-source CI/P1R preparation/Commitlint outcomes, release history and five package indexes. The captures below are historical and retained unchanged. The successor remains proposed; publication, qualification and Story 6.4 remain gated.

# P1R successor publication observations — 2026-10-18

Read-only evidence for the conditional [owner decision](../../6-4-p1r-successor-publication-decision-2026-10-18.md). The first NuGet capture and `release-analysis.json` reflect EventStore source `269b20d58236f07228bc2afd54807b94bd96f37b` before live `main` advanced. They remain historical. The **earlier selected observation** is `source-observations.json`, `release-analysis-current.json`, `observations-current.json` and the five `*.index-current.json` raw response bodies. These are observations, not source proof, publication, package selection or qualification.

`source-observations.json` records the GitHub API responses used: `git/ref/heads/main` before and after capture, `releases/latest`, the five most recent releases, `git/ref/tags/v3.124.0`, the HTTP 404 probe for `git/ref/tags/v3.125.0`, `compare/v3.124.0...<main-sha>`, `contents/references/Hexalith.Builds?ref=<main-sha>`, `contents/.releaserc.json?ref=<main-sha>`, `contents/.github/workflows/ci.yml?ref=<main-sha>`, and exact-head `actions/workflows/{ci.yml,p1r-qualification.yml,commitlint.yml}/runs`. It retains the full analyzed commit messages, release config text and SHA-256, tag target, Builds gitlink, and run outcomes. At that earlier capture, the CI reusable Builds workflow was referenced as `@main` and a resolved successful-run SHA was unavailable. See the refresh above for the newly resolved identity and actual run status.

The historical analyzer result is reproducible from `references/Hexalith.EventStore` with its already installed, locked Node dependencies and the captured source file:

```bash
node --input-type=module <<'NODE'
import { readFileSync } from 'node:fs';
import { analyzeCommits } from '@semantic-release/commit-analyzer';
import semver from 'semver';
const input = JSON.parse(readFileSync('../../_bmad-output/implementation-artifacts/evidence/6-4-p1r-successor-decision-20261018/source-observations.json', 'utf8'));
const commits = input.compare.commits.map(item => ({ hash: item.sha, message: item.message }));
const releaseType = await analyzeCommits({}, { commits, cwd: process.cwd(), logger: { log: () => {} } });
console.log({ source: input.main_sha, count: commits.length, releaseType, nextVersion: semver.inc(input.latest_release.tag_name.slice(1), releaseType) });
NODE
```

The five official flat-container URLs are recorded individually in each observation file. Requests sent `Cache-Control: no-cache`; each `*.index-current.json` retains the exact response body, whose SHA-256 is in `observations-current.json`. The second capture occurred after EventStore `main` advanced. Raw bodies are kept byte-for-byte, without editor formatting.
