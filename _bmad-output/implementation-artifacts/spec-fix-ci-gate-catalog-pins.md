---
title: 'Align CI policy gate with catalog-owned Conversations/Folders versions'
type: 'bugfix'
created: '2026-10-06'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Every CI run since 72182df (2026-10-04) has failed at "Validate workflow policy". At `tests/tools/run-ci-workflow-gates.ps1:851-853`, the gate still requires root `Directory.Packages.props` to pin `Hexalith.Conversations.Client`, `Hexalith.Conversations.Contracts`, `Hexalith.Folders.Client`, and `Hexalith.Folders.Contracts` at `1.0.0`. That commit deliberately removed those pins so the shared Hexalith.Builds catalog owns the four versions as one set. Because the gate fails, every downstream CI job is skipped.

**Approach:** Replace the stale root-pin requirement with the current ownership invariant. Root `Directory.Packages.props` must import the shared Hexalith.Builds package catalog, and it must not declare, update, or remove a `PackageVersion` for any of the four packages. The gate does not assert a specific version, because the catalog behind the root gitlink owns that choice. Nothing else in the gate changes.

</frozen-after-approval>

## Implementation Notes

- Changed only the former pin block of `tests/tools/run-ci-workflow-gates.ps1`. The working tree is shared with concurrent sessions, and the live-apphost spec also plans gate edits, so the edit stayed narrow.
- The gate parses root `Directory.Packages.props` as XML rather than regex-matching text. Commented-out entries are ignored, and `Include`, `Update`, `Remove`, and `;`-separated lists all count. Matching uses PowerShell's case-insensitive `-contains`, which mirrors MSBuild item identity.
- Catalog ownership is checked as exactly one `$(Hexalith1BuildPackageProps)` import, with that property pointing at the root-declared `references/Hexalith.Builds/Props/Directory.Packages.props`. The `workflow-gates` job checks out with `submodules: false`, so the gate cannot read the catalog itself. A missing catalog entry would still fail restore in the `ci` job under central package management.
- No version is asserted. MSBuild evaluation (`CI=true`, Release, Server project) resolves all four packages to `1.0.0` through the catalog, the same effective version as the last green CI run on fbb5501.
- Verification: the gate passes on the current tree, and `git diff --check` is clean. Scratch-copy negative tests cover six cases. Five fail as intended: the restored fbb5501 pins (4 failures), a case-variant `Update`, a `;`-list `Include`, a removed catalog import, and a redirected catalog path. The sixth, a commented-out pin plus an unrelated pin, passes as intended.
- Review patch: the catalog check now uses `SelectNodes` with `InnerText` and a case-sensitive `-cne`, so an unrelated second `PropertyGroup` or a `Condition` on the property no longer fails it falsely. The `PackageVersion` scan uses `//PackageVersion`, so it also covers `Choose`/`When`. All 10 mutation cases behave as expected. The "4 failures" above came from a scratch copy that prints every failure; the real gate stops at its first `Write-Error` (deferred).
- The fix lets the skipped `ci` restore, build, and test jobs run for the first time since 72182df. Builds and seven other gitlinks have moved since the last green run on fbb5501. Their result is unverified locally and will be known only from the first CI run after push.

## Review Triage Log

- Blind Hunter 1. `PropertyGroup` enumeration adds null entries, and `Condition` turns the value into an `XmlElement`. **medium**: reproduced (count=2; `XmlElement`). Patched.
- Blind Hunter 2. Root can override the catalog's version-set properties. **low**: real and predates this change, which is scoped to `PackageVersion`. Deferred.
- Blind Hunter 3. Import check is case-insensitive and ignores `Condition`. **low**: `-ne` is case-insensitive (verified), patched with `-cne`. `Condition="false"` and the missing `.gitmodules` check are rejected: restore fails loudly anyway, and a guard would need more than a simple fix.
- Blind Hunter 4. Scan misses `Choose`/`When`, `$(P)` includes, `VersionOverride`, and other files. **low**: `Choose`/`When` patched through the XPath change. The rest is rejected: hypothetical, outside the spec's root-`Directory.Packages.props` scope, and needing new scans.
- Blind Hunter 5. Four IDs are hard-coded instead of a family prefix. **low**: Projects consumes only these four. Rejected: beyond the frozen intent, and hit only by a hypothetical future reference.
- Blind Hunter 6. No regression test for the gate. **medium**: the gap predates this change and covers the whole gate. Deferred.
- Blind Hunter 7. The "4 failures" result cannot be reproduced with the real gate. **low**: true, because the real gate stops at its first `Write-Error`. Clarified in the notes, and the reporting bug is deferred.
- Blind Hunter 8. Verification is too narrow for the newly unblocked `ci` jobs. **medium**: true. A local full build was not run because concurrent sessions share bin/obj in this worktree. Stated plainly in the notes.
- Blind Hunter 9. Spec lacks `baseline_commit` and a `context:` link, and the status is unclear. **false**: the one-shot template has no `baseline_commit`, and status is now `done`.
- Blind Hunter 10. `done` used for a superseded spec. **false**: the user chose to close it as done/superseded, the status vocabulary has no superseded value, and the change log records the outcome.
- Blind Hunter 11. Closing note does not name the new spec. **low**: patched with a relative link.
- Blind Hunter 12. Ask First blocker cleared. **low**: verified, since nuget.org lists `1.0.0` for all four. Recorded in the closing note.
- Blind Hunter 13. Stale Design Notes in the superseded spec. **low**: the closing note now marks them as describing the July design.
