---
title: 'Story 6.4 Platform tuple owner CI follow-through'
type: 'bugfix'
created: '2026-10-30'
status: 'done'
route: 'oneshot'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
---

<frozen-after-approval reason="User authorized the concrete owner branch push, reviewed workflow registration and exact-candidate CI operation with 'I authorize'.">

## Intent

Push the recorded Platform tuple candidate, register the reviewed manual preflight through the configured owner process, execute exact-candidate CI and retain its actual outcome and blockers. Preserve original checkouts, dependency versions, analyzer fixes and all historical evidence. Correct a small CI wiring defect if the actual run exposes one, record a new candidate without rewriting the old SHA and qualify that revision freshly. Keep main story 6.4 in progress. Candidate integration, publication, deployment and consumer adoption remain separate gates.

</frozen-after-approval>

## Acceptance

- Given the frozen candidate, when pushed, its exact recorded SHA and selected Builds/tool source remain verifiable remotely.
- Given the reviewed manual workflow, when registered, the default-branch diff contains only that workflow and retains the exact owner exception/head/base/merge identities.
- Given a CI run, when it completes, its exact checkout, workflow/run/attempt/job, signed artifacts, actual commands and terminal outcome are retained without claiming runtime or consumer acceptance.
- Given an observed CI wiring failure, when corrected, its smallest source change preserves the Release/package guard and all dependency versions, and receives focused local inspection plus a fresh remote run.
- Given the original workspace and historical receipts, when work ends, their recorded files, HEADs and indexes remain unchanged and story 6.4 stays in progress.

## Implementation Notes

- Original candidate `9f43cefe11bd246ce0de1cd0740261b557c0c7a0` pushed unchanged. Workflow-only PR #8 registered via explicitly configured owner exception, final merge `357d8cfc074d79042dc4395cf7fc2ff51034c74b`; no fabricated GitHub review or policy acceptance. Draft integration PR #9 preserves the candidate for review.
- First run `41012249519`, attempt 1, job `123920527376`: verified clean exact candidate and signed published artifacts, evaluated/restored all 27 project graphs, passed 14 binding controls and pin checks, then failed the solution Release build with zero warnings/one error. No selected tests ran. The ArchiveProcessFixture was transitive but missing from the solution configuration map, so MSBuild removed its parent configuration and selected Debug; the CI guard correctly rejected it.
- Add only that existing fixture project to `Hexalith.Platform.slnx`, preserving every original byte. Local validation inspects the actual generated solution configuration before/after without building Release locally. A new fast-forward candidate and a fresh hosted run will qualify this correction. Original owner main remains at the old candidate.
- Current remote main advanced independently, with a newer Builds source/catalog and changes requiring their own integration decision. Do not roll it back or promote a frozen-branch preflight as merge-result evidence. Evidence is additive under `evidence/6-4-advisory-resolution-20261021/platform-tuple-owner-ci-20261030/`.

- Mapping correction committed as `0e524c17333ba181ff7a6a2384b680da9760489e` and pushed without force. Its hosted run `41013494873` passed the full Release build with zero warnings/errors, then 79/80 cases passed; the worker setup barrier timed out. All restored inputs and failed XML are retained.
- Worker setup now completes before arming the unchanged 200 ms timer; cleanup cancels and observes callers first. Review closed the cleanup and line-ending findings. The changed C# file follows required CRLF and retains the existing analyzer fix. Debug build and the 80-case class group pass with two processors. New exact candidate `6a186754102b33c3cfea823efc55e49264efbb84` is pushed, and fresh run `41014652010` is executing. No dependency versions or production guards changed.
- Remote main advanced independently and incorporated prior candidate work through other pushes. Its latest observed main `1e4e447c84109f2d8ba6e7e2357953e5cdbef9a4` records Builds `a13d7972395f989955cf00f1047f1ba50b1a6b73` with tool 4.38.3. This catalog differs from the published source, including Memories 3.0.10, newer Azure/HotChocolate and an explicit FrontComposer.Contracts.UI selection. Official GitHub latest release and NuGet feed still end at 4.38.3. No current-main acceptance is inferred from the frozen candidate.

## Review Triage Log

- Low, patched: solution entry now preserves required CRLF; ordinary whitespace checks remain.
- Medium, patched: test cleanup cancels and observes callers before releasing and deleting input on setup failure.
- Low, patched: changed C# file uses consistent required CRLF; prior analyzer correction retained.
- Both independent closure reviews found no remaining concrete regression. Main story status stays in progress by explicit user constraint.

- Scoped owner CI operation completed: run `41014652010`, attempt 1, job `123928122261` passed exact candidate `6a186754102b33c3cfea823efc55e49264efbb84` with all 82 required cases, 14 controls, 27 durable restored graphs and zero Release warnings/errors. All three raw artifacts match official digests. The main story remains in progress; no runtime or consumer acceptance is granted.
- Final package review found TenantRegistry.Host's separate central Gateway 3.124.0 pin and eight actual catalog-version differences. The one-line 3.128.4 proposal is unapplied and passes patch check. Applying it needs separate dependency authority and fresh closure/host qualification. Current remote main's later Builds identity also remains unqualified against its tool. Detailed exact next actions and observation cutoff are recorded in the evidence packet. No dependency versions were changed by this CI operation.
- Final preservation: 115458 original file checks, zero differences, thirteen original HEAD/index identities unchanged, no umbrella nested initialization, existing analyzer and historical evidence preserved. Draft PR #10 is the remaining correction review point; original #9 closed after other pushes. Scope is done for authorized transport/registration/preflight/recording only.
