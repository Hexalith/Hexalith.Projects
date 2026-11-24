---
title: 'Preserve the EventStore 3.97 qualification logs'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resolve DW-70 by tracking the missing logs named by the three retained
EventStore 3.97 qualification manifests. Preserve the original bytes and keep
the manifests and historical acceptance records intact.

</frozen-after-approval>

## Implementation Notes

The three existing manifests were the inventory. Each restored log matches its
original SHA-256, and the narrow Git attributes preserve its raw bytes.

## Review Triage Log

- Tightened the Git attributes to the three historical bundle directories and
  listed all expected manifest counts in the evidence index.
- The Git attributes preserve bytes through staging and checkout. To spot an
  editor change in a local copy, run `sha256sum -c artifact-manifest.sha256`
  in that bundle directory.
