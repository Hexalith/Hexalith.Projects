# Fixed prerequisite dispatch implementation

This isolated candidate adds three CiCd files:

- `tools/dispatch_contracts_prerequisite.py`
- `tools/tests/test_dispatch_contracts_prerequisite.py`
- `.github/workflows/dispatch-contracts-prerequisites.yml`

`owner-dispatch-implementation.patch` contains only those three additions.
`tools/delivery_runtime.py` is an unchanged verification copy from CiCd
`264e5de34341187a0ad4724dd6a39248b9cb9b58` (`origin/main` when read); do not
include that copy as a source change. No proposal or publisher payload was
invented or changed. Shared CiCd files and remotes were not changed.

The controller checks the exact owner workflow/ref/event and three equal full
source SHAs, successful behavioral qualification, the existing App identity,
and the token inventory containing exactly the chosen owner. It verifies local
script/runtime/workflow/test/proposal bytes against exact-SHA Contents API reads
whose complete bytes reproduce the returned Git blob hashes. It executes no
module source, checkout, Git command or other subprocess with the token.

Publish requires the current complete module tree to equal the reviewed result
and its sole parent to equal the reviewed base. Projects additionally permits
the exact result tree/direct base parent of the optional committed
`projects-prerequisite-policy.json`, whose repository/schema and two fixed
policy paths are validated and whose local bytes must match the exact owner
source. Missing proof permits only the original publisher binding. Neither
binding permits unrelated later trees. Both operations also require the
exact reviewed release workflow blob. Verify permits later pin/tree changes and
can dispatch only `contracts-prerequisite-verify`. Owner and module main are
reread immediately before the single fixed POST. The atomic, fsynced attempted
receipt precedes that POST. Errors, SIGTERM, SIGINT and SIGKILL cannot authorize a
repeat POST. Reusing an attempted receipt performs read-only correlation only;
current-main and source authorization still apply. A workflow rerun on a fresh
runner refuses writes because it lacks the previous checkpoint. The workflow
does not restore receipt artifacts. Unresolved older attempts require
coordinator read-only API observation; there is no workflow-level automatic
recovery after owner main advances.

The workflow uses exact pinned actions, an exact-SHA sparse checkout with no
submodules or persisted credentials, a secret-free behavioral job, and the
existing `cicd-app` environment. The writer shares `deliver-owned-writes`,
requests only Actions write and Contents read for one fixed repository, runs
the controller after token failure, and retains a preinitialized receipt for
90 days. Neither job grants Actions write to the GitHub token.

Focused revision validation, run from this candidate directory:

```sh
run-with-temp python3.12 -I -B tools/tests/test_dispatch_contracts_prerequisite.py
```

The suite preserves all source, proposal, token, refusal, timeout and actual
SIGKILL controls and adds request-specific identity and competing-run cases.
`correlation-fix-tests.log` records the focused qualification of this revision.
Earlier Python 3.12/3.14 and patch-check logs describe the previous candidate;
full verification of the revised candidate is left to the coordinator. The
workflow is unchanged by this revision; its actionlint 1.7.12 result remains in
`actionlint.log`.

The API 2026-03-10 dispatch response must be status 200 with a valid
`workflow_run_id`, `run_url` and `html_url`. No additional request parameter is
used. Only that returned ID can succeed after its exact source, workflow,
actor and repository are verified in the run inventory. Additional same-App,
same-SHA runs do not prevent the returned run from succeeding and cannot be
substituted for it. HTTP refusals remain failures. A 204, lost response or
malformed accepted result retains bounded candidate observations and stays
unresolved, even when matching candidates exist. The run inventory supports
the exact `head_sha` filter.
Sources: [workflow dispatch API](https://docs.github.com/en/rest/actions/workflows?apiVersion=2026-03-10#create-a-workflow-dispatch-event)
and [workflow run API](https://docs.github.com/en/rest/actions/workflow-runs#list-workflow-runs-for-a-workflow).

Remaining integration and operational limits:

- The coordinator must review, integrate and commit the candidate alongside the
  two real approved `docs/proposals/contracts-prerequisites/{eventstore,projects}-publisher.json`
  files and, when publishing the corrected Projects policy tree, the real
  approved `projects-prerequisite-policy.json`. This candidate has not been
  pushed or dispatched.
- A publish remains ineligible until module main actually has the exact reviewed
  complete result tree and single base parent. Branch protection or a different
  merge result cannot be bypassed by this controller.
- Correlation stops within 60 seconds, with API time counted and sleeps at most
  five seconds. Missing returned runs, malformed or oversized inventories, and
  unavailable direct dispatch identities remain unresolved and never cause
  another POST. At most 1,000 runs at the exact target SHA are accepted in an
  inventory. Candidate observations alone do not identify the requested
  publish or verify operation.
- Independent read-and-dispatch APIs cannot atomically lock module main. A
  change after the final reread can produce an unresolved correlation, which
  requires investigation and never automatic retry.
- No live App call or hosted dispatch was performed here. Owner committed-source
  reads are deliberately unauthenticated and fail closed on API availability
  or rate-limit failures. A fresh, deliberately authorized workflow dispatch
  is a separate invocation; the controller does not provide cross-run global
  deduplication for deliberate new dispatches.
