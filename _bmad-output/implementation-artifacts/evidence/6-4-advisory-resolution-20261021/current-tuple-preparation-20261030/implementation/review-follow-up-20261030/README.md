# Review follow-up — 2026-10-30

All fixes and new observations are confined to this follow-up packet and five
owner files. No original receipt, script, result, source snapshot or hash manifest
was replaced. The canonical story, sprint and preparation-spec accounting remain
with the parent. The original source/publication/owner-seed acceptance blockers
remain open.

## Targeted fixes

- The loader accepts exact supported 4.x NuGet versions only. SemVer prerelease
  identifiers permit repeated hyphens (`beta--x`) and reject numeric leading
  zeros (`01`, `beta.01`). Major 5 and malformed/unsupported versions refuse
  before the range can represent an empty interval. Minor/patch components stay
  within NuGet's signed 32-bit component bounds.
- `inventorySha256` and `inventoryUrl` are required, unavailable publication
  fields. The locator must be the exact official Builds release asset for the
  bound tag; malformed digests, foreign locators and wrong tags refuse. The
  positive historical control uses the actual captured 4.38.3 release asset
  digest/locator, not an invented future observation. Existing producer controls
  at `Tools/test-g4-tool-package-contracts.ps1:983` and `:1051` already generate
  the expected evaluated runtime catalog and compare installed embedded bytes.
  The owner document now requires retaining that verified actual producer
  evidence before filling the fields. No producer validator was duplicated.
- A successful read-only control runs the catalog verification against the
  existing Builds checkout, including committed LF versus actual CRLF bytes and
  the returned path. Dirty checkout and Git-operation failures preserve argv,
  cwd, exit and captured public diff/stderr in the raised observation. Recorded
  artifact orchestration tests prove pin/dirty/catalog failures precede output
  creation, signature verification, download and executable invocation. No Git
  fixture commits, staging, branches or index operations are used.
- The parent Gateway fixture now supplies 3.124.0; actual child MSBuild evaluation
  must still produce one Gateway 3.128.4 entry and inherited transitive policy.

## Targeted results

Exactly two test files and the fresh command-owned HTTP probe were run:

| Command | Result |
| --- | --- |
| `python3 -B eng/test_builds_tuple.py` | 15 tests pass |
| `python3 -B eng/test_host_builds_resolution.py` | 15 tests pass |
| Fresh copied `run-local-host.py` | 10 HTTP controls pass; owned host/Dapr/Redis all stop with exit 0 |

The original implementation binding suite ran 10 tests and its host selection
suite ran 15. An earlier progress message incorrectly said 16 host tests; the
retained output and this follow-up consistently report 15. New counts are
15 binding tests and 15 host selection tests. No restore, rebuild, broad source
suite, CI dispatch or producer qualification was repeated.

The fresh probe derives its expectations from the actual restored EventStore
3.128.4 owner source `717a06e16c3afd81b904eaf4de646b74b13262d2`, identified by its
NuGet repository metadata. [Expected owner source](expected-owner-source.json)
retains the relevant exact registration/controller/protocol source hashes and
bytes. It verifies one subscription exactly: pubsub `eventstore-pubsub`, topic
`*.*.projection-changed`, route `projections/changed`. Readiness has exactly eight
named checks; aggregate health adds healthy `self`. Only actor placement and
projection writer protocol are unhealthy, and the known disconnected/disabled
placement and inactive writer causes are checked. All other named checks must
be healthy; extra, missing or newly failing checks refuse. These results retain
readiness/health HTTP 503 without accepting an arbitrary 503.

[The sanitized receipt](local-host.json) includes the exact named statuses,
validated cause codes, subscription mapping and cleanup. Raw health descriptions,
exception data, secrets and private logs are not archived. Liveness remains 200,
authentication denials 401 and authenticated authorization denials 403. The probe
still proves no ready full service graph, successful owner query or runtime/
publication acceptance.

[Owner changes](owner-follow-up.patch), [probe changes](probe-follow-up.patch),
exact command receipts and new hashes are retained here. The [preservation
comparison](original-evidence-preservation.json) passes for every original
implementation receipt, including its original scripts and integrity manifest.
