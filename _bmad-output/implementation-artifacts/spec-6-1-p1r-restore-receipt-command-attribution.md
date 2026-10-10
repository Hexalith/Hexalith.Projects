---
title: '6.1-P1R Correct restore receipt command attribution'
type: 'bugfix'
created: '2026-10-10'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The 6.1-P1R published executor's restore receipt labels `create-database` with a later PostgreSQL readiness probe and `backup` with a later copy command, misleading owners who inspect recovery evidence.

**Approach:** Carry the command recorded for database creation or start and the command recorded for `pg_dump` through the helper returns into the corresponding recovery receipt rows. Add a focused regression check for intervening commands while preserving the receipt schema and historical evidence.

</frozen-after-approval>

## Implementation Notes

- `references/Hexalith.EventStore/tools/p1r_published_executor.py`: return the recorded Docker start command ID through `container` and `postgres_container`, and the `pg_dump` command ID through `postgres_backup`; resolve those IDs when building the recovery receipt. Existing callers that only need paths or ports discard the extra ID.
- `references/Hexalith.EventStore/tools/tests/test_p1r_published_executor.py`: exercise `restore_case` with synthetic command recording so intervening Docker inspect, readiness, and copy commands cannot become the `create-database` or `backup` witness.
- Historical P1R packets and receipt schema remain untouched. The focused executor suite passed: 47 tests.
- The backup receipt's `output_sha256` intentionally hashes the archive written by `pg_dump -f`; it is the artifact output, while the raw command journal separately hashes process stdout. The regression now checks the chosen commands' timing and exit fields too. The qualification suite passed: 49 tests.

## Review Triage Log

- `false` — The backup receipt's `output_sha256` is the dump artifact hash by the existing restore contract; `pg_dump -f` writes that artifact, while the raw command journal hashes stdout separately.
- `medium`, deferred — The receipt binds either the dump or the copy-out command as its backup witness, without a paired transfer link. This predates the attribution fix and needs a compatible receipt contract change; recorded in `deferred-work.md`.
- `false` — Command IDs are assigned as `len(self.commands) + 1` and the executor only appends to the command list, so an ID maps to its one-based list position throughout a run.
- `low`, patched — The new regression initially asserted only argv and backup hash. It now compares working directory, start and finish times, and exit code with each selected raw command. Full receipt shape validation is already covered by `test_p1r_published_qualification.py`; the synthetic command test does not grant operational qualification.
