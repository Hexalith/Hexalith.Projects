# 6.1-P1R EventStore 3.106.0 Qualification Attempt 11

This append-only bundle continues only reciprocal rollback phase 5. It adopts
the immutable passing phase 1–4 evidence sealed by attempt 10, preserves the
failed branch `fix/p1r-3106-rollback-attempt10` unchanged, and qualifies the
distinct clean rollback branch `fix/p1r-3106-rollback-candidate` from fresh
worktrees and caches. Contract, selected source/package coordinate, corrected
candidate, production source, frozen intent, Architecture, sprint status,
deferred work, downstream gates, and attempts 1–10 remain unchanged.

Outcome: **stopped / non-qualifying**. Attempt 10's 140-entry seal verified,
and all 27 immutable passing phase 1–4 rows were adopted. The selected
candidate's packaged reciprocal controls were also rechecked: `3.88.0` and
rollback `3.70.1` both retain exact `HXM016` rejection evidence.

The new rollback commits are
`38bac7906bd8aa3887bdec4dd7c31470a10dd5ed` and
`1f46bc373d82fb266c8396759cc3a57c62e6ae7c`. Their commit messages passed
repository-pinned commitlint. Commit 1's tree is byte-identical to attempt
10's `44bd5eb...` tree (`b404b162...`), and the new head tree is byte-identical
to `caf49c5...` (`d53a5361...`). The failed attempt-10 branch and commits remain
unchanged.

Rollback coordinate/tree proof, static alignment, restore, serialized Release
build, Module tests (117/117), Evidence tests (68/68), and the authoritative
catalog all passed. The next row, `rollback-bld-009-audit`, exited `1` because
the exact copied audit records `generatedFromRevision=44bd5eb...`, which is not
an ancestor of the new linear rollback head. The tree-equivalent new commit 1
is `38bac790...`; changing that recorded revision would necessarily change the
head tree and violate the explicit attempt-11 tree-equality requirement.

The first audit failure is retained with log SHA-256
`378b23a299031df38bc9a5b9a35ab26438a42668de5dd05aa28767c534f013ae`.
No rollback G-4, packaged command, `v3.70.1` EventStore source/package/API, or
owner-acceptance row ran. `audit-provenance-state.txt`, `disk-state.txt`, and
the passing post-failure state row prove the exact provenance mismatch and
clean state of the rollback and live repositories. Architecture, planning,
deferred work, downstream gates, and all four owner decisions remain unchanged
and pending.
