# Closed receiver Q2 audit preparation

This directory contains a file-only audit script. It has not executed Q2 or claimed qualification.

`audit_closed_q2.py` requires the fixed Q2 summary to report all 62 processes passed before it inspects process absence or the pinned private directory. It requires the reviewed source receipt `83bb2d...`, runtime manifest `2e127e...`, producer receipt `d7d77a...`, and a separately supplied Root audit with `passed: true` and its exact SHA. It creates a fresh output only on complete success.

The script rechecks the full current input union, both copied runtime closures (only SDK DLL/PDB differ), all process receipts/facts/absence, 94 original/private native XML executions and their exact source-derived display-name multisets, 64 focused semantic pairs, exact active per-case admission/stage masks, last-disposal brackets, three first-business canaries, three eligible roles, one excluded entry, capped/failing-sink loss, every closed counter row and zero count/allocation column, and the original failure/correction lineage.

Future command, only after Q2 closes and Root reviews this script:

```text
python3 /tmp/story65-receiver-phase-qualified-prep-20261107/audit_closed_q2.py --execute --parent-audit <ROOT_CLOSED_Q2_AUDIT_JSON> --parent-audit-sha256 <ROOT_AUDIT_SHA256> --output /tmp/story65-receiver-phase-qualified-prep-20261107/qualification-receipt.json
```

The aggregate is diagnostic qualification only, with feature acceptance false and no addition to ordinary native1048. Whole includes post-timestamp helper admission/construction/bookkeeping. Stage5 is the last completed/unwound original DisposeAsync interval, not a total or callback count. Overlaps and residuals are not causal partitions. Capped/sink modes cannot prove complete per-case rows, and excluded artifact absence does not prove an invisible worker never existed. The inherited ten unknown crossmodule old/current provenance rows remain an explicit limit.

No builds, child processes, assembly loads, app lifecycle actions, counter-window writes or cleanup mutations are implemented here. The private directory is inspected read-only only after the closed successful summary. Root retains independent audit and operational release.
