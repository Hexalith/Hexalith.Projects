# 6.1-P1R EventStore 3.106.0 Qualification Attempt 8

This append-only bundle corrects only attempt 7's non-contract isolation-proof
harness. The proof runs direct commands from a retained script; contract,
source, candidate, frozen intent, and attempts 1–7 are unchanged.

Outcome: **stopped / non-qualifying**. Preservation, coordinate, candidate,
catalog/audit, and API rows passed. Source restore passed, then the serialized
source build exited `1` after `/tmp` filled. The log retains `MSB3021`,
`MSB3027`, and `MSB3491` errors reporting `No space left on device` (93 build
errors). No test, package-source, remote, Builds runner, rollback, or acceptance
row ran. `disk-state.txt` and the clean post-failure checkout assertion are
retained. No prior attempt directory or cache was deleted and no rerun occurred.
