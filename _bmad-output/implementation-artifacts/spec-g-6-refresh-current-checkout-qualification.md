---
title: 'G-6 Refresh Current Checkout Qualification'
type: 'chore'
created: '2026-10-03'
status: 'done'
route: 'oneshot'
baseline_commit: '0f03582b3457a6d9212d60e2f9146a6043af5f7e'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-g-6-ci-readiness.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current root `0f03582b3457a6d9212d60e2f9146a6043af5f7e` advances Conversations, Folders, Memories, and Parties after the recorded G-6 qualification at `387b5af242bc99f48848f3d0265fa3c38575bc03`. These runtime-owning gitlink changes require fresh proof; the approved tuple remains unchanged and evidence reuse remains disabled.

**Approach:** Observe the already running exact-source CI `37118950623`, verify its G-6 result and uploaded artifact, retain byte-identical evidence with a SHA-256 index, and refresh only the G-6 current-checkout tracking after a qualifying result. Require approved audit, qualifier 1/1, support 33/33, zero critical skips, successful consumer builds and cleanup, and exact root/gitlinks. Preserve historical packets, tuple approval, other prerequisites and downstream states. Keep EOL fingerprint reproducibility, Platform app-model startup and obsolete packet-reference tests in their existing deferred scope. If the run fails, retain its failure and record truthful current status; replan any substantive repair rather than widening this evidence refresh. Do not dispatch release or enable proof reuse.

</frozen-after-approval>

## Implementation Notes

Investigation found no new tuple decision or external mutation necessary. The existing exact-source CI supplies the proof, so this run uses the small-change route. Local read-only audit exits 0 with tupleApproved=true, zero issues, and 7,026 material files. Focused runner/gate controls pass 25/25; Builds tuple controls pass 3/3. The historical result fails current validation on materialInputs, while all 91 historical retention checks pass. The source checkout was clean before this spec was added.

At investigation time, job `111191288139` passed preflight, pinned PostgreSQL provisioning, and all effective graph restores; live qualification and artifact upload were running. No CI dispatch, duplicate live proof, dependency update, or Git mutation was performed during investigation.

CI and G-6 job succeeded on the exact baseline source. Retained the original artifact ZIP, API metadata/job log and extracted result/capture/command/cleanup documents in qualification-evidence/g-6-checkout-refresh-20261003. GitHub ZIP digest, canonical result digest, exact source, controlled graphs and material fingerprint match. An independent unmodified validator replay on a clean disposable exact-source checkout exited 0; it used ignored existing local restored graphs and ran no runtime tests. Qualifier 1/1, support 33/33, all 18 commands and ten AppHosts/McpCli pass; all five cleanup flags are true. Updated only G-6 current_checkout provenance fields, including an explicit source revision; prior approval, packets and all other status fields are preserved.

Review follow-up retained the exact replay preparation/validation recipe, commands and exit code, local audit, eleven copied graph-input hashes, verification procedure and approval/deferred-scope links. The recipe passed the full unmodified validator and removed its disposable checkout. Local investigation controls were repeated solely to retain missing review receipts (25/25 and 3/3); the expected stale-result rejection and all 91 historical-preservation checks are now retained. No new deferred work or runtime change was introduced.

## Review Triage Log

| Finding | Verdict and evidence | Resolution |
| --- | --- | --- |
| Replay command omits preparation | medium: nondefault EOL/source/graph preparation was absent from the example | patch: replay.py and exact preparation commands in replay/receipt.json |
| Replay log lacks invocation metadata | low: initial log contained only the success banner | patch: timestamp, cwd, validator revision/hash, command and exit code in replay receipt |
| Local matching audit is unretained | low: initial audit existed only in /tmp | patch: retain replay/audit.json |
| Copied graph inputs lack paths/hashes | medium: eleven ignored local asset inputs were not individually identified | patch: graphInputs in replay receipt; unmodified validator compares controlled summaries |
| Local 25/25 and 3/3 checks lack receipts | low: investigation counts were not archived | patch: focused logs and exact commands in investigation/controls.json |
| Historical rejection lacks evidence identity/output | low: investigation recorded only its reason | patch: investigation/historical-checks.json and historical-result-rejection.log bind path/digest/command/exit 1 |
| Historical 91-file check lacks output | low: successful check existed only in session output | patch: preserve command, index digest and historical-preservation.log |
| ZIP/receipt booleans lack reproduction procedure | medium: original manual checks were not executable from retained evidence | patch: replay.py verifies ZIP/API digest, extracted bytes and canonical digest, then invokes original strict receipt validator |
| Tuple decision lacks link | low: approval coordinates were present but the scope link was missing | patch: link existing owner decision |
| Deferred limitations lack links/warning context | low: three explicit limitations lacked their ledger link | patch: link ledger and explain retained Platform ASPIRE010 warning without claiming startup |
