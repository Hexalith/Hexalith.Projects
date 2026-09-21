# 6.1-P1R EventStore 3.106.0 Qualification Attempt 9

This append-only bundle restarts the unchanged qualification contract after
attempt 8's task-created worktrees and caches were safely removed. It uses a
new temporary root, preserves attempts 1–8, and retains the corrected direct-
command external-consumer CPM proof. Contract, production source, candidate,
frozen intent, Architecture, sprint status, deferred-work gates, and downstream
gates are unchanged.

Outcome: **stopped / non-qualifying**. Preservation, coordinate, candidate
static/catalog/audit, seven-API, complete EventStore source and package-source,
external-consumer CPM, official-only remote restore, 14-package inventory,
candidate restore/build, and candidate Module-test rows passed. The next row,
`bld-candidate-004-evidence-test`, exited `2`: 59 of 68 Evidence tests passed
and 9 failed. The failures consistently introduced `HXE152` binding
diagnostics, including the positive readiness fixture, or observed `HXE152`
where a fixture expected `HXE153`. The first result is retained and no later
catalog, audit, packaged-command, rollback, or acceptance row ran.

The external consumer was created outside all repositories and ancestor build
configuration. `effective-msbuild-properties.json` proves central package
management was not inherited; `remote-consumer/`,
`remote-consumer-files.sha256`, and `remote-package-inventory.tsv` retain and
verify its exact project/config/assets bytes and all 14 selected `3.106.0`
packages from the empty qualification cache. `disk-state.txt` and the clean
post-failure assertions cover the candidate plus both EventStore lanes and all
14 exact dependency worktrees. `proofs/api-compare.sh` is byte-identical to
the executed seven-API comparison script referenced by its ledger row.
