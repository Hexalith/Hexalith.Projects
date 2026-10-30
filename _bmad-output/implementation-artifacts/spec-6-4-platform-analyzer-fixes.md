---
title: 'Story 6.4 upstream Platform analyzer fixes'
type: 'bugfix'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Repair only the four authorized Platform analyzer failures in AgentsAdmissionCurrentBoundaryTests, AgentsHostStartupBoundaryTests, PortableDevelopmentCompositionTests and AcceptedLaunchBoundaryTests. Preserve existing test behavior and analyzer gates. Build the Composition test project in Debug and run the narrowest relevant existing tests. Preserve existing work and historical evidence; do not stage, commit, push, publish, deploy, initialize nested submodules or change dependency versions. Keep story 6.4 in progress and provide exact verification evidence and the remaining Platform/Builds tuple qualification handoff.

</frozen-after-approval>

## Implementation Notes

- No intent gaps or irreversible actions. Four local test-file changes; no public API or production-code changes.
- Owning Platform tree initially clean at 494ecb32802d1454c15704066487fa206f5bcec0. Existing Projects story/tracking edits and evidence are preserved. Workspace baseline, inherited EditorConfig, Platform build configuration, README, candidate policy and architecture govern this work.
- Add ArgumentNullException.ThrowIfNull for the dereferenced fault/mutation theory parameters before fixture allocation; configure the private Fixture.Issue await with ConfigureAwait(false). Keep all InlineData and assertions unchanged.
- Use the existing Debug project build and direct xUnit class filters for all four affected classes. No running AppHost is needed for these metadata-only unit fixtures; live integration qualification remains separate.
- Evidence: evidence/6-4-advisory-resolution-20261021/platform-analyzer-fixes-20261029/. Preserve the original main story and sprint status bytes; this scoped repair does not complete story 6.4.

- Scoped analyzer repair complete: Debug build exit 0 (zero warnings/errors); four focused classes pass 76/76; whitespace exit 0. Full commands, XML, patch, preservation checks and remaining tuple handoff are in the evidence README.
- This done status applies only to the scoped repair, not story 6.4. Main story and sprint state remain in-progress and byte-preserved. No commit performed per user instruction.

## Review Triage Log

- Low, resolved: reviewer observed unfinished verification while build ran. Final Debug exit 0 and independently parsed 76 passing cases close this finding. Reviewer found no code defect; no deferred work.
